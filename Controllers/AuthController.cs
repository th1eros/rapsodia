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
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Tentativa de registro: {Username}", request.Username);

                if (!IsPasswordStrong(request.Password))
                {
                    return BadRequest(new { message = "Senha fraca: use maiúsculas, minúsculas, números e símbolos." });
                }

                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                    return Conflict(new { message = "Usuário já existe." });

                var isFirstUser = !await _context.Users.AnyAsync();

                var user = new User
                {
                    Username = request.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = isFirstUser ? "Admin" : "Analyst",
                    CreatedAt = DateTime.UtcNow 
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário {Username} criado com sucesso.", request.Username);
                return Ok(new { message = $"Usuário criado como {user.Role}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico no registro do usuário {Username}", request.Username);

                return StatusCode(500, new
                {
                    message = "Erro interno no servidor ao registrar.",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Usuário ou senha inválidos." });
                }

                var token = _tokenService.GenerateToken(user);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no login: {Username}", request.Username);
                return StatusCode(500, new { message = "Erro ao processar login." });
            }
        }

        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            return password.Any(char.IsUpper) && password.Any(char.IsLower)
                && password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
}