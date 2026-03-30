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
            var keyStr = _config["Jwt:Key"] ?? "Chave_Mestra_Super_Secreta_aBitat_2026";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("Jwt__Issuer")
                        ?? _config["Jwt:Issuer"]
                        ?? "Rapsodia",
                audience: Environment.GetEnvironmentVariable("Jwt__Audience")
                          ?? _config["Jwt:Audience"]
                          ?? "Rapsodia_Clients",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}