using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Seed;

public static class GerenteInicialSeeder
{
    public static async Task ExecutarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ComandixDbContext>();
        var configuracao = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Funcionario>>();

        if (await context.Funcionarios.AnyAsync(f => f.Cargo == Cargo.Gerente && f.Usuario != null))
            return;

        var usuario = configuracao["GerenteInicial:Usuario"];
        var senha = configuracao["GerenteInicial:Senha"];

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
            throw new InvalidOperationException(
                "Nenhum gerente com acesso encontrado. Configure GerenteInicial:Usuario e GerenteInicial:Senha."
            );

        var gerente = new Funcionario("Gerente", Cargo.Gerente);
        gerente.DefinirCredenciais(usuario, hasher.HashPassword(gerente, senha));

        context.Funcionarios.Add(gerente);
        await context.SaveChangesAsync();
    }
}