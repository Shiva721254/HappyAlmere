using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace HappyCity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DutchFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "discovery",
                table: "discovery_entries",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "setweight(to_tsvector('dutch', coalesce(title, '')), 'A') || setweight(to_tsvector('dutch', coalesce(district, '')), 'B') || setweight(to_tsvector('dutch', coalesce(description, '')), 'C')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_discovery_entries_search_vector",
                schema: "discovery",
                table: "discovery_entries",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_discovery_entries_search_vector",
                schema: "discovery",
                table: "discovery_entries");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "discovery",
                table: "discovery_entries");
        }
    }
}
