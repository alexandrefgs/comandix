using Comandix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandix.Infrastructure.Data.Configurations;

public class PessoaComandaConfiguration : IEntityTypeConfiguration<PessoaComanda>
{
    public void Configure(EntityTypeBuilder<PessoaComanda> builder)
    {
        builder.ToTable("PessoasComanda");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).IsRequired().HasMaxLength(60);
    }
}
