using API_SVsharp.Models.Entity;

namespace API_SVsharp.Application.Interfaces
{
    public interface ITokenService
    {
        // [CTO] Recebe o objeto User completo para extrair Username e Role com segurança
        string GenerateToken(User user);
    }
}