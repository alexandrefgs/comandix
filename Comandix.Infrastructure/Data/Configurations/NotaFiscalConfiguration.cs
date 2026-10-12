using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class NotaFiscalConfiguration : IEntityTypeConfiguration<NotaFiscal>
{
    public void Configure(EntityTypeBuilder<NotaFiscal> builder)
    {
        builder.ToTable("NotasFiscais");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.ChaveAcesso).IsRequired().HasMaxLength(44).IsFixedLength();
        builder.Property(n => n.Protocolo).IsRequired().HasMaxLength(50);
        builder.Property(n => n.ValorTotal).HasPrecision(10, 2);

        builder
            .HasOne<Comanda>()
            .WithMany()
            .HasForeignKey(n => n.ComandaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.ComandaId).IsUnique();
        builder.HasIndex(n => new { n.Serie, n.Numero }).IsUnique();
        builder.HasIndex(n => n.ChaveAcesso).IsUnique();
    }
}
