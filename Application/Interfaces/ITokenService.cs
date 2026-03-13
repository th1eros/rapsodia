using API_SVsharp.Models.Entity;

namespace API_SVsharp.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
