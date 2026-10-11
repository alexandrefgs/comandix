using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Infrastructure.Data;

public class ComandixDbContext : DbContext
{
    public ComandixDbContext(DbContextOptions<ComandixDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Comanda> Comandas => Set<Comanda>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ComandixDbContext).Assembly);
    }
}