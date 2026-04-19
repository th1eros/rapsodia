using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Rapsodia.Models.Entity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Rapsodia.Application.Interfaces;

namespace Rapsodia.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        // [CTO] O nome DEVE ser exatamente GenerateToken para bater com a interface
        public string GenerateToken(User user)
        {
            var keyStr = Environment.GetEnvironmentVariable("JWT_KEY")
                         ?? _config["Jwt:Key"]
                         ?? throw new InvalidOperationException("JWT_KEY não configurada.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER")
                        ?? _config["Jwt:Issuer"],
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                          ?? _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}