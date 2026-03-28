using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rapsodia.Data;
using Rapsodia.Models.Entity;
using Rapsodia.Application.Interfaces;
using Rapsodia.Application.DTOs;
using Microsoft.AspNetCore.RateLimiting;

namespace Rapsodia.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("auth-limit")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext  _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _context      = context;
            _tokenService = tokenService;
            _logger       = logger;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Tentativa de registro para o usuÃ¡rio: {Username}", request.Username);

            // ValidaÃ§Ã£o de inTropic (Criterio CISO/CTO: LÃ³gica e Probabilidade)
            if (!IsPasswordStrong(request.Password))
            {
                _logger.LogWarning("Senha fraca fornecida para o usuÃ¡rio: {Username}", request.Username);
                return BadRequest(new { message = "A senha nÃ£o atende aos critÃ©rios de inTropic: deve conter letras maiÃºsculas, minÃºsculas, nÃºmeros e caracteres especiais." });
            }

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return Conflict(new { message = "UsuÃ¡rio jÃ¡ existe." });

            // Se for o primeiro usuÃ¡rio, define como Admin
            var isFirstUser = !await _context.Users.AnyAsync();

            var user = new User
            {
                Username     = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role         = isFirstUser ? "Admin" : "Analyst"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("UsuÃ¡rio {Username} criado com sucesso como {Role}.", request.Username, user.Role);
            return Ok(new { message = $"UsuÃ¡rio criado com sucesso como {user.Role}." });
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Tentativa de login: {Username}", request.Username);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Falha de autenticaÃ§Ã£o para o usuÃ¡rio: {Username}", request.Username);
                return Unauthorized(new { message = "UsuÃ¡rio ou senha invÃ¡lidos." });
            }

            _logger.LogInformation("UsuÃ¡rio {Username} autenticado com sucesso.", request.Username);
            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }

        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }
    }
}
