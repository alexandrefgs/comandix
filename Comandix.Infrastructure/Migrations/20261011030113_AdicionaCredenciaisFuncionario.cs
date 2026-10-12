using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comandix.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCredenciaisFuncionario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "Funcionarios",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "Usuario",
                table: "Funcionarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_Usuario",
                table: "Funcionarios",
                column: "Usuario",
                unique: true,
                filter: "[Usuario] IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Funcionarios_Usuario", table: "Funcionarios");

            migrationBuilder.DropColumn(name: "SenhaHash", table: "Funcionarios");

            migrationBuilder.DropColumn(name: "Usuario", table: "Funcionarios");
        }
    }
}
