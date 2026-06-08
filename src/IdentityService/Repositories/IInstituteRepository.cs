using SharedKernel;

namespace IdentityService.Repositories;

public interface IInstituteRepository
{
    IEnumerable<Institute> List();
    Institute? GetBySubdomain(string subdomain);
    void Add(Institute institute);
    void AddBranch(Branch branch);
}
