using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_SVsharp.Data;
using API_SVsharp.Models.Entity;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.Application.DTOs;
using Microsoft.AspNetCore.RateLimiting;

namespace API_SVsharp.Controllers
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
            _logger.LogInformation("Tentativa de registro para o usuário: {Username}", request.Username);

            // Validação de Entropia (Criterio CISO/CTO: Lógica e Probabilidade)
            if (!IsPasswordStrong(request.Password))
            {
                _logger.LogWarning("Senha fraca fornecida para o usuário: {Username}", request.Username);
                return BadRequest(new { message = "A senha não atende aos critérios de entropia: deve conter letras maiúsculas, minúsculas, números e caracteres especiais." });
            }

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return Conflict(new { message = "Usuário já existe." });

            // Se for o primeiro usuário, define como Admin
            var isFirstUser = !await _context.Users.AnyAsync();

            var user = new User
            {
                Username     = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role         = isFirstUser ? "Admin" : "Analyst"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário {Username} criado com sucesso como {Role}.", request.Username, user.Role);
            return Ok(new { message = $"Usuário criado com sucesso como {user.Role}." });
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
                _logger.LogWarning("Falha de autenticação para o usuário: {Username}", request.Username);
                return Unauthorized(new { message = "Usuário ou senha inválidos." });
            }

            _logger.LogInformation("Usuário {Username} autenticado com sucesso.", request.Username);
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
