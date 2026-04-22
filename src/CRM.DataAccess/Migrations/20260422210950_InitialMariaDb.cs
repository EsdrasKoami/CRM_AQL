using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMariaDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    NoClient = table.Column<string>(type: "char(7)", fixedLength: true, maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomEntreprise = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Adresse = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ville = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodePostal = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Courriel = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstActif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.NoClient);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    NoUtilisateur = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Login = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prenom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstActif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.NoUtilisateur);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Contrats",
                columns: table => new
                {
                    NoContrat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NoClient = table.Column<string>(type: "char(7)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MontantMax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstActif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contrats", x => x.NoContrat);
                    table.ForeignKey(
                        name: "FK_Contrats_Clients_NoClient",
                        column: x => x.NoClient,
                        principalTable: "Clients",
                        principalColumn: "NoClient",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemsContrat",
                columns: table => new
                {
                    NoItemContrat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NoContrat = table.Column<int>(type: "int", nullable: false),
                    NoProduit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescriptionProduit = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuotaMin = table.Column<int>(type: "int", nullable: false),
                    QuotaMax = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsContrat", x => x.NoItemContrat);
                    table.ForeignKey(
                        name: "FK_ItemsContrat_Contrats_NoContrat",
                        column: x => x.NoContrat,
                        principalTable: "Contrats",
                        principalColumn: "NoContrat",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    NoTransaction = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NoContrat = table.Column<int>(type: "int", nullable: false),
                    TypeTransaction = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoProduit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantite = table.Column<int>(type: "int", nullable: true),
                    DateTransaction = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    NoUtilisateur = table.Column<int>(type: "int", nullable: true),
                    Commentaire = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.NoTransaction);
                    table.ForeignKey(
                        name: "FK_Transactions_Contrats_NoContrat",
                        column: x => x.NoContrat,
                        principalTable: "Contrats",
                        principalColumn: "NoContrat",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Utilisateurs_NoUtilisateur",
                        column: x => x.NoUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "NoUtilisateur",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_NoClient",
                table: "Contrats",
                column: "NoClient");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsContrat_NoContrat_NoProduit",
                table: "ItemsContrat",
                columns: new[] { "NoContrat", "NoProduit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_NoContrat",
                table: "Transactions",
                column: "NoContrat");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_NoUtilisateur",
                table: "Transactions",
                column: "NoUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Login",
                table: "Utilisateurs",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemsContrat");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Contrats");

            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
