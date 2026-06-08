using IdentityService.Models;
using SharedKernel;

namespace IdentityService.Repositories;

public sealed class UserRepository : BaseRepository, IUserRepository
{
    public UserRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public IEnumerable<UserSummaryResponse> List(Guid? instituteId)
        => store.Users.Values
            .Where(x => !instituteId.HasValue || x.InstituteId == instituteId)
            .OrderBy(x => x.DisplayName)
            .Select(x => new UserSummaryResponse(x.Id, x.InstituteId, x.Email, x.DisplayName, x.Role, x.Status));

    public AppUser? GetByEmail(string email)
        => store.Users.Values.FirstOrDefault(x => x.Email == email.ToLowerInvariant());

    public AppUser? GetById(Guid userId)
        => store.Users.GetValueOrDefault(userId);

    public bool HasSuperAdmin()
        => store.Users.Values.Any(x => x.Role == Roles.SuperAdmin);

    public void Add(AppUser user)
        => store.Users[user.Id] = user;

    public void Update(AppUser user)
        => store.Users[user.Id] = user;
}
