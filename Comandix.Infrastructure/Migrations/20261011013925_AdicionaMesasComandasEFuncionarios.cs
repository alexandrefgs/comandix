using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comandix.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaMesasComandasEFuncionarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Funcionarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcionarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mesas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Capacidade = table.Column<int>(type: "int", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comandas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MesaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GarcomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechadaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CobrarTaxaServico = table.Column<bool>(type: "bit", nullable: false),
                    PercentualTaxaServico = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ValorTaxaServico = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comandas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comandas_Funcionarios_GarcomId",
                        column: x => x.GarcomId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comandas_Mesas_MesaId",
                        column: x => x.MesaId,
                        principalTable: "Mesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensComanda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeProduto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensComanda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensComanda_Comandas_ComandaId",
                        column: x => x.ComandaId,
                        principalTable: "Comandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensComanda_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PessoasComanda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PessoasComanda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PessoasComanda_Comandas_ComandaId",
                        column: x => x.ComandaId,
                        principalTable: "Comandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensComandaPagantes",
                columns: table => new
                {
                    ItemComandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PessoaComandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensComandaPagantes", x => new { x.ItemComandaId, x.PessoaComandaId });
                    table.ForeignKey(
                        name: "FK_ItensComandaPagantes_ItensComanda_ItemComandaId",
                        column: x => x.ItemComandaId,
                        principalTable: "ItensComanda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensComandaPagantes_PessoasComanda_PessoaComandaId",
                        column: x => x.PessoaComandaId,
                        principalTable: "PessoasComanda",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comandas_GarcomId_FechadaEm",
                table: "Comandas",
                columns: new[] { "GarcomId", "FechadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Comandas_MesaId",
                table: "Comandas",
                column: "MesaId",
                unique: true,
                filter: "[Status] = 'Aberta'");

            migrationBuilder.CreateIndex(
                name: "IX_ItensComanda_ComandaId",
                table: "ItensComanda",
                column: "ComandaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensComanda_ProdutoId",
                table: "ItensComanda",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensComandaPagantes_PessoaComandaId",
                table: "ItensComandaPagantes",
                column: "PessoaComandaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_Numero",
                table: "Mesas",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PessoasComanda_ComandaId",
                table: "PessoasComanda",
                column: "ComandaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensComandaPagantes");

            migrationBuilder.DropTable(
                name: "ItensComanda");

            migrationBuilder.DropTable(
                name: "PessoasComanda");

            migrationBuilder.DropTable(
                name: "Comandas");

            migrationBuilder.DropTable(
                name: "Funcionarios");

            migrationBuilder.DropTable(
                name: "Mesas");
        }
    }
}
