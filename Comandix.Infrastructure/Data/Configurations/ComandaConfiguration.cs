using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class ComandaConfiguration : IEntityTypeConfiguration<Comanda>
{
    public void Configure(EntityTypeBuilder<Comanda> builder)
    {
        builder.ToTable("Comandas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.PercentualTaxaServico).HasPrecision(5, 2);
        builder.Property(c => c.ValorTaxaServico).HasPrecision(10, 2);

        builder.Ignore(c => c.Consumo);

        builder.HasOne(c => c.Mesa)
            .WithMany()
            .HasForeignKey(c => c.MesaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Garcom)
            .WithMany()
            .HasForeignKey(c => c.GarcomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Itens)
            .WithOne()
            .HasForeignKey(i => i.ComandaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Pessoas)
            .WithOne()
            .HasForeignKey(p => p.ComandaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.Pessoas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.MesaId)
            .IsUnique()
            .HasFilter("[Status] = 'Aberta'");

        builder.HasIndex(c => new { c.GarcomId, c.FechadaEm });
    }
}