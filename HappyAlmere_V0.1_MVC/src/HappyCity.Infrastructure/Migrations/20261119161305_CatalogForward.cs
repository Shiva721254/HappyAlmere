using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace HappyCity.Infrastructure.Migrations;

public partial class CatalogForward : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(CatalogForward).Assembly.GetManifestResourceStream("HappyCity.Infrastructure.Migrations.CatalogForward.sql")
            ?? throw new InvalidOperationException("Missing reviewed migration SQL.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Use the protected restore procedure; destructive downgrade is not supported.");
}
