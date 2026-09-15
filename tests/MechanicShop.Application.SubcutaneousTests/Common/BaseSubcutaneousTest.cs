using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Common;

// Collection fixture only — IClassFixture would create a second SQL container.
public abstract class BaseSubcutaneousTest : IAsyncLifetime
{
    protected readonly WebAppFactory Factory;

    protected BaseSubcutaneousTest(WebAppFactory factory)
    {
        Factory = factory;
    }

    public async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}