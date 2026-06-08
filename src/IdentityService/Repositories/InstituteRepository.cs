using SharedKernel;

namespace IdentityService.Repositories;

public sealed class InstituteRepository : BaseRepository, IInstituteRepository
{
    public InstituteRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public IEnumerable<Institute> List()
        => store.Institutes.Values.OrderBy(x => x.Name);

    public Institute? GetBySubdomain(string subdomain)
        => store.Institutes.Values.FirstOrDefault(x => x.Subdomain == subdomain);

    public void Add(Institute institute)
        => store.Institutes[institute.Id] = institute;

    public void AddBranch(Branch branch)
        => store.Branches[branch.Id] = branch;
}
