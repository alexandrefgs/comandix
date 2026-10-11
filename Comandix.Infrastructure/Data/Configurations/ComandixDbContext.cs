using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Infrastructure.Data;

public class ComandixDbContext : DbContext
{
    public ComandixDbContext(DbContextOptions<ComandixDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ComandixDbContext).Assembly);
    }
}