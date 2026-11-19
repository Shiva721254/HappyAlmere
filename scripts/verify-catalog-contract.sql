-- Verification-only expected SQL-only contract, never an application migration.
-- Independently reviewed baseline: b4f092ffbbf385f16c4f42aaef117fafc8e56da7.
-- D01/D09 compatibility columns/indexes from InitialDiscovery + CatalogForward;
-- hierarchy correction from SupportedRegions; submitted for ARCH-03 re-review.
-- Execute only on the separate EF-generated reference database. Do not generate this
-- file from migrated/restored catalogs at verification time.
ALTER TABLE discovery.discovery_entries ADD COLUMN legacy_city_id integer NULL;
ALTER TABLE discovery.discovery_entries ADD COLUMN legacy_category_id integer NULL;
CREATE UNIQUE INDEX "Cities_uuid" ON catalog.cities(id);
CREATE UNIQUE INDEX "Categories_uuid" ON catalog.categories(id);
CREATE UNIQUE INDEX "DiscoveryEntries_uuid" ON discovery.discovery_entries(id);
CREATE INDEX "IX_DiscoveryEntries_CategoryId" ON discovery.discovery_entries(legacy_category_id);
CREATE UNIQUE INDEX "IX_DiscoveryEntries_CityId_Slug" ON discovery.discovery_entries(legacy_city_id,slug);
CREATE INDEX "IX_DiscoveryEntries_CityId_IsPublished_CategoryId_Id" ON discovery.discovery_entries(legacy_city_id,is_published,legacy_category_id,legacy_id);
CREATE INDEX entries_page_order ON discovery.discovery_entries(city_id, (legacy_id IS NULL),legacy_id,id) WHERE is_published;
CREATE FUNCTION catalog.lock_hierarchy() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF current_setting('transaction_isolation') <> 'read committed' THEN RAISE EXCEPTION 'Hierarchy writes require read committed'; END IF;
 PERFORM pg_advisory_xact_lock(72002); RETURN NULL; END $$;
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
CREATE TRIGGER categories_lock BEFORE INSERT OR UPDATE ON catalog.categories FOR EACH STATEMENT EXECUTE FUNCTION catalog.lock_hierarchy();
CREATE TRIGGER categories_cycle BEFORE INSERT OR UPDATE ON catalog.categories FOR EACH ROW EXECUTE FUNCTION catalog.check_hierarchy();
CREATE TRIGGER districts_lock BEFORE INSERT OR UPDATE ON catalog.districts FOR EACH STATEMENT EXECUTE FUNCTION catalog.lock_hierarchy();
CREATE TRIGGER districts_cycle BEFORE INSERT OR UPDATE ON catalog.districts FOR EACH ROW EXECUTE FUNCTION catalog.check_hierarchy();
CREATE FUNCTION catalog.check_timezone() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF NOT EXISTS(SELECT 1 FROM pg_timezone_names WHERE name=NEW.time_zone) THEN
  RAISE EXCEPTION 'Invalid timezone' USING ERRCODE='23514'; END IF; RETURN NEW; END $$;
CREATE TRIGGER cities_timezone BEFORE INSERT OR UPDATE ON catalog.cities FOR EACH ROW EXECUTE FUNCTION catalog.check_timezone();
