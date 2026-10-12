using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class MovimentacaoCaixaConfiguration : IEntityTypeConfiguration<MovimentacaoCaixa>
{
    public void Configure(EntityTypeBuilder<MovimentacaoCaixa> builder)
    {
        builder.ToTable("MovimentacoesCaixa");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Valor).HasPrecision(10, 2);
        builder.Property(m => m.Motivo).IsRequired().HasMaxLength(200);
    }
}
