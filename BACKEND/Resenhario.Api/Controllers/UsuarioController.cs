using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resenhario.Api.Data;

namespace Resenhario.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsuarioController(AppDbContext context) => _context = context;

    public record UsuarioPublico(int IdUsuario, string Nome, string Email, DateTime DataCadastro);

    [HttpGet("me")]
    public async Task<ActionResult<UsuarioPublico>> GetMeuUsuario()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        var usuario = await _context.Usuarios.Where(u => u.IdUsuario == usuarioId)
            .Select(u => new UsuarioPublico(u.IdUsuario, u.Nome, u.Email, u.DataCadastro))
            .FirstOrDefaultAsync();
        return usuario is null ? NotFound() : Ok(usuario);
    }
}
