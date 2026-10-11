using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Comandix.Api.Tests.Infra;

public class ComandixApiFactory : WebApplicationFactory<Program>
{
    private const string BancoDeTestes = "ComandixDb_Tests";

    private const string ConnectionString =
        $"Server=(localdb)\\mssqllocaldb;Database={BancoDeTestes};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ComandixDbContext>();

        if (!context.Database.GetConnectionString()!.Contains(BancoDeTestes))
            throw new InvalidOperationException(
                "Os testes não estão apontando para o banco de testes."
            );

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        return host;
    }
}
