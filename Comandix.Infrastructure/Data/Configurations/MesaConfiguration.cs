using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class MesaConfiguration : IEntityTypeConfiguration<Mesa>
{
    public void Configure(EntityTypeBuilder<Mesa> builder)
    {
        builder.ToTable("Mesas");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(m => m.Numero).IsUnique();
    }
}