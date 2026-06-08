using SharedKernel;

namespace IdentityService.Services;

public static class SeedData
{
    public static void Ensure(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<AppDataStore>();
        if (store.Users.Values.Any(x => x.Role == Roles.SuperAdmin))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var superAdmin = new AppUser(Guid.NewGuid(), null, "superadmin@coachapp.local", hasher.Hash("Admin@123"), "System Super Admin", Roles.SuperAdmin, "Active");
        store.Users[superAdmin.Id] = superAdmin;
    }
}
