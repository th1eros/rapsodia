using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.Models.Entity;
using Microsoft.IdentityModel.Tokens;

namespace API_SVsharp.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            // Chave lida do config (env var no Render, appsettings em dev).
            var keyString = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key não configurada.");

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Tempo de expiração configurável via appsettings / env var.
            var expiresInMinutes = int.TryParse(_config["Jwt:ExpiresInMinutes"], out var mins) ? mins : 60;

            var claims = new[]
            {
                new Claim(ClaimTypes.Name,              user.Username),
                new Claim(ClaimTypes.Role,              user.Role),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:            _config["Jwt:Issuer"],
                audience:          _config["Jwt:Audience"],
                claims:            claims,
                expires:           DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
