using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel;

public abstract class BaseRepository
{
    protected readonly AppDataStore store;
    protected readonly IClock clock;

    protected BaseRepository(IServiceProvider serviceProvider)
    {
        store = serviceProvider.GetRequiredService<AppDataStore>();
        clock = serviceProvider.GetRequiredService<IClock>();
    }
}
