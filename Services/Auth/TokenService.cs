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
            // [CISO] Pega a chave secreta do appsettings.json
            var keyString = _config["Jwt:Key"] ?? "CHAVE_ULTRA_SECRETA_DETECCAO_DE_VULNERABILIDADES_2026";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // [CISO] Payload - Identidade do usuário dentro do token
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };      

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "SVsharpAPI",
                audience: _config["Jwt:Audience"] ?? "SVsharpUsers",
                claims: claims,
                expires: DateTime.Now.AddHours(8), // [CTO] Token expira em 8h (Segurança)
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}   