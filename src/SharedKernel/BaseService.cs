using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel;

public abstract class BaseService
{
    private readonly IServiceProvider serviceProvider;

    protected BaseService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    protected T GetService<T>() where T : notnull
        => serviceProvider.GetRequiredService<T>();
}
