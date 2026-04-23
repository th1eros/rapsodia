using Rapsodia.Models.Entity;

namespace Rapsodia.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
