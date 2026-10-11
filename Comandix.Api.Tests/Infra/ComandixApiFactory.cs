using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Comandix.Api.Seed;

namespace Comandix.Api.Tests.Infra;

public class ComandixApiFactory : WebApplicationFactory<Program>
{
    private const string BancoDeTestes = "ComandixDb_Tests";

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("COMANDIX_TEST_CONNECTION")
        ?? $"Server=(localdb)\\mssqllocaldb;Database={BancoDeTestes};Trusted_Connection=True;TrustServerCertificate=True";

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
        GerenteInicialSeeder.ExecutarAsync(host.Services).GetAwaiter().GetResult();

        return host;
    }
}