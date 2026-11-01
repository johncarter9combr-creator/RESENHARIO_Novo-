using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resenhario.Api.Data;
using Resenhario.Api.Models;

namespace Resenhario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdaptacaoController : ControllerBase
{
    private readonly AppDbContext _context;
    public AdaptacaoController(AppDbContext context) => _context = context;

    public record AdaptacaoResposta(int IdAdaptacoes, int IdLivro, string TituloLivro,
        int IdFilme, string TituloFilme, string? PosterUrl, DateTime DataVinculo);
    public record CriarAdaptacaoRequest(int IdLivro, int IdFilme);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdaptacaoResposta>>> GetAdaptacoes()
    {
        var adaptacoes = await (
            from a in _context.Adaptacoes
            join l in _context.Livros on a.IdLivro equals l.IdLivros
            join f in _context.Filmes on a.IdFilme equals f.IdFilme
            orderby l.Titulo, f.Titulo
            select new AdaptacaoResposta(a.IdAdaptacoes, a.IdLivro, l.Titulo,
                a.IdFilme, f.Titulo, f.PosterUrl, a.DataVinculo)
        ).ToListAsync();
        return Ok(adaptacoes);
    }


    [HttpGet("ranking")]
    public async Task<IActionResult> Ranking()
    {
        var ranking = await _context.Adaptacoes.Select(a => new
        {
            a.IdAdaptacoes,
            a.IdLivro,
            a.IdFilme,
            TituloLivro = _context.Livros.Where(l => l.IdLivros == a.IdLivro).Select(l => l.Titulo).FirstOrDefault(),
            TituloFilme = _context.Filmes.Where(f => f.IdFilme == a.IdFilme).Select(f => f.Titulo).FirstOrDefault(),
            MediaFidelidade = _context.Reviews.Where(r => r.IdAdaptacao == a.IdAdaptacoes && r.NotaFidelidade != null)
                .Select(r => (double?)r.NotaFidelidade).Average(),
            TotalAvaliacoes = _context.Reviews.Count(r => r.IdAdaptacao == a.IdAdaptacoes && r.NotaFidelidade != null)
        })
        .Where(x => x.TotalAvaliacoes > 0)
        .OrderByDescending(x => x.MediaFidelidade)
        .ThenByDescending(x => x.TotalAvaliacoes)
        .Take(20)
        .ToListAsync();
        return Ok(ranking);
    }

    [HttpPost]
    public async Task<IActionResult> CriarAdaptacao([FromBody] CriarAdaptacaoRequest dados)
    {
        if (dados.IdLivro <= 0 || dados.IdFilme <= 0)
            return BadRequest("Selecione um livro e um filme válidos.");
        if (!await _context.Livros.AnyAsync(l => l.IdLivros == dados.IdLivro))
            return BadRequest("O livro selecionado não existe no catálogo.");
        if (!await _context.Filmes.AnyAsync(f => f.IdFilme == dados.IdFilme))
            return BadRequest("O filme selecionado não existe no catálogo.");

        var existente = await _context.Adaptacoes.FirstOrDefaultAsync(a =>
            a.IdLivro == dados.IdLivro && a.IdFilme == dados.IdFilme);
        if (existente is not null)
            return Conflict(new { mensagem = "Essa adaptação já está vinculada." });

        var adaptacao = new Adaptacao
        {
            IdLivro = dados.IdLivro,
            IdFilme = dados.IdFilme,
            DataVinculo = DateTime.UtcNow
        };
        _context.Adaptacoes.Add(adaptacao);
        await _context.SaveChangesAsync();
        return Ok(new { mensagem = "Adaptação vinculada com sucesso.", adaptacao.IdAdaptacoes });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletarAdaptacao(int id)
    {
        var adaptacao = await _context.Adaptacoes.FindAsync(id);
        if (adaptacao is null) return NotFound();
        if (await _context.Reviews.AnyAsync(r => r.IdAdaptacao == id))
            return Conflict("Não é possível remover esta adaptação enquanto houver resenhas vinculadas.");
        _context.Adaptacoes.Remove(adaptacao);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
