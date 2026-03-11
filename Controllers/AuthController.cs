using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_SVsharp.Data;
using API_SVsharp.Models.Entity;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.Application.DTOs;
using BCrypt.Net;

namespace API_SVsharp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // [CISO] Registro com carimbo de tempo UTC para o Postgres
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] LoginRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Usuário já cadastrado.");

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "Analyst",
                CreatedAt = DateTime.UtcNow // [CTO] Evita erro de 'null value' no banco
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuário criado com sucesso!" });
        }

        // [CTO] Login com logs de depuração para conferência no terminal
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login([FromBody] LoginRequest request)
        {
            // [CISO] Mantemos o log de QUEM tentou, mas NUNCA a senha
            Console.WriteLine($"[AUDIT] Tentativa de login para o usuário: {request.Username} às {DateTime.UtcNow}");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // [CISO] Logamos a falha para detectar ataques de força bruta
                Console.WriteLine($"[SECURITY] Falha de autenticação para: {request.Username}");
                return Unauthorized("Usuário ou senha inválidos.");
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}