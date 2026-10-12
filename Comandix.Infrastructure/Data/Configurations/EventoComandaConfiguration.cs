using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class EventoComandaConfiguration : IEntityTypeConfiguration<EventoComanda>
{
    public void Configure(EntityTypeBuilder<EventoComanda> builder)
    {
        builder.ToTable("EventosComanda");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);
        builder
            .Property(e => e.Descricao)
            .IsRequired()
            .HasMaxLength(EventoComanda.TamanhoMaximoDescricao);
        builder.Property(e => e.FuncionarioNome).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Valor).HasPrecision(10, 2);

        builder
            .HasOne<Comanda>()
            .WithMany()
            .HasForeignKey(e => e.ComandaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ComandaId, e.CriadoEm });
    }
}