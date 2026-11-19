using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyCity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportedRegions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Include ancestor parent references so a multi-row insert cannot hide
            // the not-yet-inserted final node of a cycle from the BEFORE trigger.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION catalog.check_hierarchy() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE cycle_found boolean;
                BEGIN
                 IF NEW.parent_id IS NULL THEN RETURN NEW; END IF;
                 IF TG_TABLE_NAME='categories' THEN
                  WITH RECURSIVE ancestors AS (SELECT id,parent_id FROM catalog.categories WHERE id=NEW.parent_id
                   UNION SELECT c.id,c.parent_id FROM catalog.categories c JOIN ancestors a ON c.id=a.parent_id)
                  SELECT EXISTS(SELECT 1 FROM ancestors WHERE id=NEW.id OR parent_id=NEW.id) INTO cycle_found;
                 ELSE
                  WITH RECURSIVE ancestors AS (SELECT id,parent_id FROM catalog.districts WHERE id=NEW.parent_id
                   UNION SELECT c.id,c.parent_id FROM catalog.districts c JOIN ancestors a ON c.id=a.parent_id)
                  SELECT EXISTS(SELECT 1 FROM ancestors WHERE id=NEW.id OR parent_id=NEW.id) INTO cycle_found;
                 END IF;
                 IF cycle_found THEN RAISE EXCEPTION 'Hierarchy cycle rejected' USING ERRCODE='23514'; END IF;
                 RETURN NEW;
                END $$;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "locations_supported_country",
                schema: "catalog",
                table: "locations",
                sql: "country_code = 'NL'");

            migrationBuilder.AddCheckConstraint(
                name: "cities_supported_region",
                schema: "catalog",
                table: "cities",
                sql: "country_code = 'NL' AND (default_language = 'nl' OR default_language = 'en')");

            migrationBuilder.AddCheckConstraint(
                name: "translations_supported_language",
                schema: "catalog",
                table: "category_translations",
                sql: "language_code = 'nl' OR language_code = 'en'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Use reviewed isolated restore; do not weaken applied catalog controls.");
    }
}
