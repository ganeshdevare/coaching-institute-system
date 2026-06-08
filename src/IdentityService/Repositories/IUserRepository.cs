using IdentityService.Models;
using SharedKernel;

namespace IdentityService.Repositories;

public interface IUserRepository
{
    IEnumerable<UserSummaryResponse> List(Guid? instituteId);
    AppUser? GetByEmail(string email);
    AppUser? GetById(Guid userId);
    bool HasSuperAdmin();
    void Add(AppUser user);
    void Update(AppUser user);
}
