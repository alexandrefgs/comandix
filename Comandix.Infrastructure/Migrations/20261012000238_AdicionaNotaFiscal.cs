using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comandix.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaNotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Serie = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    ChaveAcesso = table.Column<string>(
                        type: "nchar(44)",
                        fixedLength: true,
                        maxLength: 44,
                        nullable: false
                    ),
                    Protocolo = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    ValorTotal = table.Column<decimal>(
                        type: "decimal(10,2)",
                        precision: 10,
                        scale: 2,
                        nullable: false
                    ),
                    Simulada = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Comandas_ComandaId",
                        column: x => x.ComandaId,
                        principalTable: "Comandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ChaveAcesso",
                table: "NotasFiscais",
                column: "ChaveAcesso",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ComandaId",
                table: "NotasFiscais",
                column: "ComandaId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_Serie_Numero",
                table: "NotasFiscais",
                columns: new[] { "Serie", "Numero" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NotasFiscais");
        }
    }
}
