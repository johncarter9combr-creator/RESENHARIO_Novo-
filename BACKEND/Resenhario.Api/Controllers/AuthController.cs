using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resenhario.Api.Data;
using Resenhario.Api.Models;

namespace Resenhario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public record CadastroRequest(string Nome, string Email, string Senha);
    public record LoginRequest(string Email, string Senha);
    public record UsuarioResposta(int IdUsuario, string Nome, string Email, string Token);

    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastro([FromBody] CadastroRequest? dados)
    {
        if (dados is null || string.IsNullOrWhiteSpace(dados.Nome) ||
            string.IsNullOrWhiteSpace(dados.Email) || string.IsNullOrWhiteSpace(dados.Senha))
            return BadRequest("Informe nome, e-mail e senha.");

        var nome = dados.Nome.Trim();
        var email = dados.Email.Trim().ToLowerInvariant();
        if (nome.Length > 150 || email.Length > 150 || !email.Contains('@'))
            return BadRequest("Informe um nome e um e-mail válidos.");
        if (dados.Senha.Length < 8)
            return BadRequest("A senha deve ter pelo menos 8 caracteres.");

        if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict("Já existe uma conta com esse e-mail.");

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dados.Senha),
            DataCadastro = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return Ok(CriarResposta(usuario));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? dados)
    {
        if (dados is null || string.IsNullOrWhiteSpace(dados.Email) || string.IsNullOrEmpty(dados.Senha))
            return BadRequest("Informe e-mail e senha.");

        var email = dados.Email.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (usuario is null || string.IsNullOrWhiteSpace(usuario.SenhaHash))
            return Unauthorized("E-mail ou senha incorretos.");

        var senhaValida = false;
        try { senhaValida = BCrypt.Net.BCrypt.Verify(dados.Senha, usuario.SenhaHash); }
        catch (Exception) { /* Hash antigo, fictício ou inválido: tentar o verificador legado abaixo. */ }

        // Compatibilidade com contas criadas pela versão anterior do projeto:
        // quando o hash antigo for validado, ele é atualizado para BCrypt no mesmo login.
        if (!senhaValida)
        {
            try
            {
                var resultadoAntigo = new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, usuario.SenhaHash, dados.Senha);
                senhaValida = resultadoAntigo is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
                if (senhaValida)
                {
                    usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dados.Senha);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                senhaValida = false;
            }
        }

        if (!senhaValida) return Unauthorized("E-mail ou senha incorretos.");
        return Ok(CriarResposta(usuario));
    }

    private UsuarioResposta CriarResposta(Usuario usuario)
    {
        var chave = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(chave) || Encoding.UTF8.GetByteCount(chave) < 32)
            throw new InvalidOperationException("Configure Jwt:Key com pelo menos 32 bytes por variável de ambiente antes de iniciar a API.");

        var issuer = _configuration["Jwt:Issuer"] ?? "Resenhario.Api";
        var audience = _configuration["Jwt:Audience"] ?? "Resenhario.Frontend";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email)
        };
        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddHours(8), signingCredentials: credenciais);
        return new UsuarioResposta(usuario.IdUsuario, usuario.Nome, usuario.Email,
            new JwtSecurityTokenHandler().WriteToken(token));
    }
}
