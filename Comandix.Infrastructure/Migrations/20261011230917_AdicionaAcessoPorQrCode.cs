using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comandix.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAcessoPorQrCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcessoBloqueado",
                table: "Funcionarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CodigoAcessoHash",
                table: "Funcionarios",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                table: "Funcionarios",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TentativasPinFalhas",
                table: "Funcionarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_CodigoAcessoHash",
                table: "Funcionarios",
                column: "CodigoAcessoHash",
                unique: true,
                filter: "[CodigoAcessoHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Funcionarios_CodigoAcessoHash",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "AcessoBloqueado",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "CodigoAcessoHash",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "TentativasPinFalhas",
                table: "Funcionarios");
        }
    }
}
