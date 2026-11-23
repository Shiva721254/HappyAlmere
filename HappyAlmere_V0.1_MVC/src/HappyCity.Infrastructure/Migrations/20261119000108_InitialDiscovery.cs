using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HappyCity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.CheckConstraint("CK_Categories_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.CheckConstraint("CK_Cities_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryEntries", x => x.Id);
                    table.CheckConstraint("CK_DiscoveryEntries_Slug", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("CK_DiscoveryEntries_Title", "length(btrim(\"Title\")) > 0");
                    table.ForeignKey(
                        name: "FK_DiscoveryEntries_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscoveryEntries_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Slug" },
                values: new object[,]
                {
                    { 1, "Activiteiten", "activiteiten" },
                    { 2, "Eten & drinken", "eten" },
                    { 3, "Besparen", "besparen" },
                    { 4, "Diensten", "diensten" },
                    { 5, "Winkelen", "winkelen" }
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "Name", "Slug" },
                values: new object[] { 1, "Almere", "almere" });

            migrationBuilder.InsertData(
                table: "DiscoveryEntries",
                columns: new[] { "Id", "CategoryId", "CityId", "Description", "District", "Icon", "IsPublished", "Slug", "Title" },
                values: new object[,]
                {
                    { 1, 1, 1, "Inspiratie voor een gratis buitenactiviteit.", "Almere Stad", "☀", true, "wandeling", "Een wandeling aan het water" },
                    { 2, 1, 1, "Ontdek ideeën voor een dag met kinderen.", "Almere Buiten", "♧", true, "familiedag", "Een familiedag plannen" },
                    { 3, 2, 1, "Voorbeeld van een categorie voor lunch en restaurants.", "Almere Haven", "♨", true, "lunch", "Lokaal lunchen" },
                    { 4, 3, 1, "Hier komen actuele, gecontroleerde acties.", "Almere Stad", "€", true, "aanbiedingen", "Winkelaanbiedingen" },
                    { 5, 4, 1, "Vind straks lokale vakmensen voor onderhoud.", "Almere Poort", "⌂", true, "huis-tuin", "Huis en tuin" },
                    { 6, 5, 1, "Inspiratie voor lokale winkels en voorzieningen.", "Muziekwijk", "◇", true, "buurt", "Ontdek je buurt" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Slug",
                table: "Cities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryEntries_CategoryId",
                table: "DiscoveryEntries",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryEntries_CityId_IsPublished_CategoryId_Id",
                table: "DiscoveryEntries",
                columns: new[] { "CityId", "IsPublished", "CategoryId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryEntries_CityId_Slug",
                table: "DiscoveryEntries",
                columns: new[] { "CityId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoveryEntries");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Cities");
        }
    }
}
