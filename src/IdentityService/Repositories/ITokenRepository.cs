using SharedKernel;

namespace IdentityService.Repositories;

public interface ITokenRepository
{
    RefreshToken? GetValidRefreshToken(string token, DateTime nowUtc);
    void Add(RefreshToken token);
}
