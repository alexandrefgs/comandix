using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class FuncionarioConfiguration : IEntityTypeConfiguration<Funcionario>
{
    public void Configure(EntityTypeBuilder<Funcionario> builder)
    {
        builder.ToTable("Funcionarios");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Nome).IsRequired().HasMaxLength(100);

        builder.Property(f => f.Cargo).HasConversion<string>().HasMaxLength(20);

        builder.Property(f => f.Usuario).HasMaxLength(50);
        builder.Property(f => f.SenhaHash).HasMaxLength(200);

        builder.Property(f => f.CodigoAcessoHash).HasMaxLength(64);
        builder.Property(f => f.PinHash).HasMaxLength(200);

        builder.HasIndex(f => f.Usuario).IsUnique().HasFilter("[Usuario] IS NOT NULL");

        builder
            .HasIndex(f => f.CodigoAcessoHash)
            .IsUnique()
            .HasFilter("[CodigoAcessoHash] IS NOT NULL");

        builder.Ignore(f => f.AcessaPorSenha);
        builder.Ignore(f => f.AcessaPorQrCode);
        builder.Ignore(f => f.PossuiPin);
    }
}