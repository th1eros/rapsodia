using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_SVsharp.Data;
using API_SVsharp.Models.Entity;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.Application.DTOs;

namespace API_SVsharp.Controllers
{
    // Rotas públicas por design: register e login não exigem token.
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext  _context;
        private readonly ITokenService _tokenService;

        public AuthController(AppDbContext context, ITokenService tokenService)
        {
            _context      = context;
            _tokenService = tokenService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] LoginRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return Conflict(new { message = "Usuário já existe." });

            var user = new User
            {
                Username     = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role         = "Analyst"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuário criado com sucesso." });
        }

        // POST api/auth/login — retorna JWT para o frontend armazenar.
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            Console.WriteLine($"[AUDIT] Login attempt: {request.Username} at {DateTime.UtcNow:u}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                Console.WriteLine($"[SECURITY] Failed login: {request.Username}");
                return Unauthorized(new { message = "Usuário ou senha inválidos." });
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}
