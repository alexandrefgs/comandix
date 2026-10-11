using System.Text;
using System.Text.Json.Serialization;
using Comandix.Api.Auth;
using Comandix.Api.ExceptionHandlers;
using Comandix.Api.Seed;
using Comandix.Domain.Entities;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ComandixDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder
    .Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Secao))
    .Validate(
        o => Encoding.UTF8.GetByteCount(o.Chave) >= 32,
        "A chave JWT (Jwt:Chave) deve ter pelo menos 32 caracteres."
    )
    .ValidateOnStart();

builder.Services.AddSingleton<IPasswordHasher<Funcionario>, PasswordHasher<Funcionario>>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

builder
    .Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>(
        (options, jwt) =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwt.Value.Issuer,
                ValidAudience = jwt.Value.Audience,
                IssuerSigningKey = jwt.Value.ObterChaveAssinatura(),
                NameClaimType = "name",
                RoleClaimType = "role",
                ClockSkew = TimeSpan.FromMinutes(1),
            };
        }
    );

builder.Services.AddAuthorization();

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.AllowInputFormatterExceptionMessages = false;
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Comandix API v1");
        options.DocumentTitle = "Comandix API";
    });

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
    await GerenteInicialSeeder.ExecutarAsync(app.Services);

app.Run();

public partial class Program { }