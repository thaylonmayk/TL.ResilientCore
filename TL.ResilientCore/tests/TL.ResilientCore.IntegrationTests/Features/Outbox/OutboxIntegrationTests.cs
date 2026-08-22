using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.ResilientCore.Infrastructure.Persistence;
using TL.ResilientCore.IntegrationTests.Setup;
using Xunit;

namespace TL.ResilientCore.IntegrationTests.Features.Outbox;

public class OutboxIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public OutboxIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OutboxMessages_DeveEstarPresenteNoModel_E_PermitirConsulta()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var outboxType = dbContext.Model.FindEntityType("TL.ResilientCore.Infrastructure.Outbox.OutboxMessage");
        
        Assert.NotNull(outboxType);
    }
}