using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class ItemComandaConfiguration : IEntityTypeConfiguration<ItemComanda>
{
    public void Configure(EntityTypeBuilder<ItemComanda> builder)
    {
        builder.ToTable("ItensComanda");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.NomeProduto)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.PrecoUnitario).HasPrecision(10, 2);

        builder.Property(i => i.Observacao).HasMaxLength(250);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Ignore(i => i.Subtotal);

        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Pagantes)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "ItensComandaPagantes",
                r => r.HasOne<PessoaComanda>().WithMany().HasForeignKey("PessoaComandaId").OnDelete(DeleteBehavior.ClientCascade),
                l => l.HasOne<ItemComanda>().WithMany().HasForeignKey("ItemComandaId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasKey("ItemComandaId", "PessoaComandaId"));

        builder.Navigation(i => i.Pagantes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}