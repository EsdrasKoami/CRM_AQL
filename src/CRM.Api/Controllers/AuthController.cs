using CRM.DataAccess.Context;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BC = BCrypt.Net.BCrypt;

namespace CRM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly CrmDbContext  _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(CrmDbContext db, IConfiguration config, ILogger<AuthController> logger)
    {
        _db     = db;
        _config = config;
        _logger = logger;
    }

    /// <summary>Authentifie un utilisateur et retourne un JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await _db.Utilisateurs
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == req.Login && u.EstActif, ct);

        if (user is null || !BC.Verify(req.Password, user.PasswordHash))
        {
            _logger.LogWarning("Auth — Échec de connexion pour '{Login}'.", req.Login);
            return Unauthorized(new { message = "Identifiants invalides." });
        }

        var token = GenerateJwt(user);
        _logger.LogInformation("Auth — '{Login}' connecté (rôle: {Role}).", user.Login, user.Role);

        return Ok(new LoginResponse(token, user.Role.ToString(), user.Nom, user.Prenom));
    }

    /// <summary>Hash un mot de passe en clair (utilitaire de configuration initiale).</summary>
    [HttpPost("hash-password")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult HashPassword([FromBody] HashRequest req)
        => Ok(new { hash = BC.HashPassword(req.Password) });

    // ── Helpers ──────────────────────────────────────────────────────────────
    private string GenerateJwt(Utilisateur user)
    {
        var jwtCfg = _config.GetSection("Jwt");
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtCfg["Key"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(int.Parse(jwtCfg["ExpiryMinutes"] ?? "480"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Login),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier,     user.NoUtilisateur.ToString()),
            new Claim(ClaimTypes.Name,               $"{user.Prenom} {user.Nom}"),
            new Claim(ClaimTypes.Role,               user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:   jwtCfg["Issuer"],
            audience: jwtCfg["Audience"],
            claims:   claims,
            expires:  expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ── DTO Records ──────────────────────────────────────────────────────────────
public record LoginRequest(string Login, string Password);
public record LoginResponse(string Token, string Role, string Nom, string Prenom);
public record HashRequest(string Password);
