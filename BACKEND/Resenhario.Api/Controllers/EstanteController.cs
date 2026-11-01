using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resenhario.Api.Data;
using Resenhario.Api.Models;

namespace Resenhario.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EstanteController : ControllerBase
{
    private readonly AppDbContext _context;
    public EstanteController(AppDbContext context) => _context = context;

    public record EstanteItem(int IdEstante, int IdUsuario, int? IdLivros, int? IdFilme,
        int? IdSerie, string Status, string? Titulo, string? Autor, string Tipo, DateTime DataAtualizacao);

    [HttpGet]
    public async Task<IActionResult> GetEstantes()
    {
        if (!TentarObterUsuarioId(out var usuarioId)) return Unauthorized();
        var query =
            from e in _context.Estantes
            join l0 in _context.Livros on e.IdLivros equals (int?)l0.IdLivros into livros
            from l in livros.DefaultIfEmpty()
            join f0 in _context.Filmes on e.IdFilme equals (int?)f0.IdFilme into filmes
            from f in filmes.DefaultIfEmpty()
            join s0 in _context.Series on e.IdSerie equals (int?)s0.IdSerie into series
            from s in series.DefaultIfEmpty()
            where e.IdUsuario == usuarioId
            select new EstanteItem(e.IdEstante, e.IdUsuario, e.IdLivros, e.IdFilme, e.IdSerie,
                e.Status, l != null ? l.Titulo : f != null ? f.Titulo : s != null ? s.Titulo : "Item",
                l != null ? l.Autor : f != null ? f.Diretor : null,
                l != null ? "Livro" : f != null ? "Filme" : "Série", e.DataAtualizacao);

        return Ok(await query.OrderByDescending(e => e.DataAtualizacao).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CriarEstante([FromBody] Estante estante)
    {
        if (!TentarObterUsuarioId(out var usuarioId)) return Unauthorized();
        int tipos = (estante.IdLivros.HasValue ? 1 : 0) +
                    (estante.IdFilme.HasValue ? 1 : 0) +
                    (estante.IdSerie.HasValue ? 1 : 0);
        if (tipos != 1) return BadRequest("Informe exatamente um livro, filme ou série.");

        bool obraExiste = estante.IdLivros.HasValue
            ? await _context.Livros.AnyAsync(x => x.IdLivros == estante.IdLivros.Value)
            : estante.IdFilme.HasValue
                ? await _context.Filmes.AnyAsync(x => x.IdFilme == estante.IdFilme.Value)
                : await _context.Series.AnyAsync(x => x.IdSerie == estante.IdSerie!.Value);
        if (!obraExiste) return BadRequest("A obra selecionada não existe no catálogo.");

        bool duplicado = await _context.Estantes.AnyAsync(e =>
            e.IdUsuario == usuarioId && e.IdLivros == estante.IdLivros &&
            e.IdFilme == estante.IdFilme && e.IdSerie == estante.IdSerie);
        if (duplicado) return Conflict("Esse item já está na sua estante.");

        var statusPermitidos = estante.IdLivros.HasValue
            ? new[] { "Quero ler", "Lendo", "Lido", "Abandonado" }
            : new[] { "Quero ver", "Assistindo", "Assistido" };
        if (string.IsNullOrWhiteSpace(estante.Status)) estante.Status = statusPermitidos[0];
        if (!statusPermitidos.Contains(estante.Status)) return BadRequest("Status inválido para o tipo de item.");

        estante.IdUsuario = usuarioId; // Nunca confiar no ID enviado pelo navegador.
        estante.DataAtualizacao = DateTime.UtcNow;
        _context.Estantes.Add(estante);
        await _context.SaveChangesAsync();
        return Ok(new { mensagem = "Item adicionado à estante.", estante.IdEstante });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> AtualizarEstante(int id, [FromBody] AtualizarStatusRequest dados)
    {
        if (!TentarObterUsuarioId(out var usuarioId)) return Unauthorized();
        var estante = await _context.Estantes.FirstOrDefaultAsync(e => e.IdEstante == id && e.IdUsuario == usuarioId);
        if (estante is null) return NotFound();

        var statusValidos = estante.IdLivros.HasValue
            ? new[] { "Quero ler", "Lendo", "Lido", "Abandonado" }
            : new[] { "Quero ver", "Assistindo", "Assistido" };
        if (dados is null || !statusValidos.Contains(dados.Status)) return BadRequest("Status inválido.");
        estante.Status = dados.Status;
        estante.DataAtualizacao = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletarEstante(int id)
    {
        if (!TentarObterUsuarioId(out var usuarioId)) return Unauthorized();
        var item = await _context.Estantes.FirstOrDefaultAsync(e => e.IdEstante == id && e.IdUsuario == usuarioId);
        if (item is null) return NotFound();
        _context.Estantes.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool TentarObterUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);

    public record AtualizarStatusRequest(string Status);
}
