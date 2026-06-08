using SharedKernel;

namespace IdentityService.Repositories;

public sealed class TokenRepository : BaseRepository, ITokenRepository
{
    public TokenRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public RefreshToken? GetValidRefreshToken(string token, DateTime nowUtc)
        => store.RefreshTokens.Values.FirstOrDefault(x => x.Token == token && !x.Revoked && x.ExpiresUtc > nowUtc);

    public void Add(RefreshToken token)
        => store.RefreshTokens[token.Id] = token;
}
