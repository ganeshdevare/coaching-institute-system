using SharedKernel;
using IdentityService.Repositories;

namespace IdentityService.Services;

public static class SeedData
{
    public static void Ensure(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (userRepository.HasSuperAdmin())
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var superAdmin = new AppUser(Guid.NewGuid(), null, "superadmin@coachapp.local", hasher.Hash("Admin@123"), "System Super Admin", Roles.SuperAdmin, "Active");
        userRepository.Add(superAdmin);
    }
}
