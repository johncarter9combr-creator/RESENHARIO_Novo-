using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resenhario.Api.Data;
using Resenhario.Api.Models;

namespace Resenhario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilmeController : ControllerBase
{
    private readonly AppDbContext _context;
    public FilmeController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Filme>>> GetFilmes() =>
        Ok(await _context.Filmes.OrderBy(f => f.Titulo).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Filme>> GetFilme(int id)
    {
        var filme = await _context.Filmes.FindAsync(id);
        return filme is null ? NotFound() : Ok(filme);
    }

    [HttpPost]
    public async Task<ActionResult<Filme>> CriarFilme(Filme filme)
    {
        if (string.IsNullOrWhiteSpace(filme.Titulo)) return BadRequest("O título é obrigatório.");
        var titulo = filme.Titulo.Trim();
        var existente = await _context.Filmes.FirstOrDefaultAsync(f =>
            (!string.IsNullOrEmpty(f.TmdbId) && f.TmdbId == filme.TmdbId) ||
            f.Titulo.ToLower() == titulo.ToLower());
        if (existente != null) return Ok(existente);
        filme.Titulo = titulo;
        _context.Filmes.Add(filme);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetFilme), new { id = filme.IdFilme }, filme);
    }
}
