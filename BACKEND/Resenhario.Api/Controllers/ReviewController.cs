using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resenhario.Api.Data;
using Resenhario.Api.Models;

namespace Resenhario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _context;
    public ReviewController(AppDbContext context) => _context = context;

    public record ReviewResposta(int IdReviews, int IdUsuario, string NomeUsuario,
        int? IdLivro, string? Titulo, int? NotaLivro, string? Comentario, DateTime DataCriacao,
        int? IdAdaptacao, string? TituloFilme, int? NotaFidelidade);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewResposta>>> GetReviews()
    {
        var resultado = await ConsultarReviews(1, 100);
        return Ok(resultado.Itens);
    }

    [HttpGet("feed")]
    public async Task<IActionResult> Feed([FromQuery] int pagina = 1, [FromQuery] int tamanho = 10)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 30);
        var resultado = await ConsultarReviews(pagina, tamanho);
        return Ok(new { pagina, tamanho, total = resultado.Total, totalPaginas = (int)Math.Ceiling(resultado.Total / (double)tamanho), itens = resultado.Itens });
    }

    private async Task<(List<ReviewResposta> Itens, int Total)> ConsultarReviews(int pagina, int tamanho)
    {
        var total = await _context.Reviews.CountAsync();
        var resultado = await _context.Reviews
            .OrderByDescending(r => r.DataCriacao)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(r => new ReviewResposta(
                r.IdReviews,
                r.IdUsuario,
                _context.Usuarios.Where(u => u.IdUsuario == r.IdUsuario).Select(u => u.Nome).FirstOrDefault() ?? "Usuário",
                r.IdLivro,
                _context.Livros.Where(l => l.IdLivros == r.IdLivro).Select(l => l.Titulo).FirstOrDefault() ?? "Obra",
                r.NotaLivro,
                r.Comentario,
                r.DataCriacao,
                r.IdAdaptacao,
                _context.Filmes.Where(f => _context.Adaptacoes.Any(a => a.IdAdaptacoes == r.IdAdaptacao && a.IdFilme == f.IdFilme))
                    .Select(f => f.Titulo).FirstOrDefault(),
                r.NotaFidelidade))
            .ToListAsync();
        return (resultado, total);
    }

    [HttpGet("ranking-adaptacoes")]
    public async Task<IActionResult> RankingAdaptacoes()
    {
        var ranking = await _context.Adaptacoes.Select(a => new
        {
            a.IdAdaptacoes,
            IdLivro = a.IdLivro,
            IdFilme = a.IdFilme,
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

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CriarReview([FromBody] Review review)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        if (review.IdLivro is null || !await _context.Livros.AnyAsync(l => l.IdLivros == review.IdLivro))
            return BadRequest("Selecione um livro válido.");
        if (review.NotaLivro is null || review.NotaLivro < 1 || review.NotaLivro > 5)
            return BadRequest("A nota da obra deve estar entre 1 e 5 estrelas.");
        if (string.IsNullOrWhiteSpace(review.Comentario) || review.Comentario.Trim().Length > 5000)
            return BadRequest("Escreva uma resenha de até 5.000 caracteres.");

        if (review.IdAdaptacao.HasValue)
        {
            var adaptacao = await _context.Adaptacoes.FirstOrDefaultAsync(a =>
                a.IdAdaptacoes == review.IdAdaptacao.Value && a.IdLivro == review.IdLivro.Value);
            if (adaptacao is null) return BadRequest("A adaptação selecionada não pertence ao livro escolhido.");
            if (review.NotaFidelidade is null || review.NotaFidelidade < 1 || review.NotaFidelidade > 5)
                return BadRequest("Informe a fidelidade da adaptação entre 1 e 5 estrelas.");
            review.AssistiuFilme = true;
        }
        else
        {
            review.NotaFidelidade = null;
            review.AssistiuFilme = false;
        }

        review.IdUsuario = usuarioId;
        review.Comentario = review.Comentario.Trim();
        review.Voto = null;
        review.DataCriacao = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return Ok(new { mensagem = "Resenha publicada.", review.IdReviews });
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletarReview(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.IdReviews == id && r.IdUsuario == usuarioId);
        if (review is null) return NotFound();
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
