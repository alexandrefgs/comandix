using Comandix.Domain.Entities;
using Comandix.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Infrastructure.Data;

public class ComandixDbContext : DbContext
{
    public ComandixDbContext(DbContextOptions<ComandixDbContext> options)
        : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Comanda> Comandas => Set<Comanda>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Caixa> Caixas => Set<Caixa>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<EventoComanda> EventosComanda => Set<EventoComanda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ComandixDbContext).Assembly);

        foreach (
            var entityType in modelBuilder
                .Model.GetEntityTypes()
                .Where(e => typeof(EntityBase).IsAssignableFrom(e.ClrType))
        )
        {
            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(EntityBase.Id))
                .ValueGeneratedNever();
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
