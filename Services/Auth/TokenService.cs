using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API_SVsharp.Models.Entity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using API_SVsharp.Application.Interfaces;

namespace API_SVsharp.Services.Auth
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
            var keyStr = _config["Jwt:Key"] ?? "Chave_Mestra_Super_Secreta_SVsharp_2026";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "SVsharpAPI",
                audience: _config["Jwt:Audience"] ?? "SVsharpFrontend",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}