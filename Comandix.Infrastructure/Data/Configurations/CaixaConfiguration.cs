using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class CaixaConfiguration : IEntityTypeConfiguration<Caixa>
{
    public void Configure(EntityTypeBuilder<Caixa> builder)
    {
        builder.ToTable("Caixas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(c => c.ValorAbertura).HasPrecision(10, 2);
        builder.Property(c => c.ValorEsperadoDinheiro).HasPrecision(10, 2);
        builder.Property(c => c.ValorContadoDinheiro).HasPrecision(10, 2);

        builder.Ignore(c => c.TotalSuprimentos);
        builder.Ignore(c => c.TotalSangrias);
        builder.Ignore(c => c.Diferenca);

        builder
            .HasMany(c => c.Movimentacoes)
            .WithOne()
            .HasForeignKey(m => m.CaixaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Movimentacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.Status).IsUnique().HasFilter("[Status] = 'Aberto'");
    }
}
