using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("Pagamentos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Forma).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Valor).HasPrecision(10, 2);
        builder.Property(p => p.ValorRecebido).HasPrecision(10, 2);
        builder.Property(p => p.MotivoEstorno).HasMaxLength(200);

        builder.Ignore(p => p.Troco);

        builder
            .HasOne<Caixa>()
            .WithMany()
            .HasForeignKey(p => p.CaixaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}