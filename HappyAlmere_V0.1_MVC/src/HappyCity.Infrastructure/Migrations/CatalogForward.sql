-- Approved HA-DB-D01..D10; EF executes this as one transaction.
-- No destructive downgrade. All old identifiers/content are retained.
CREATE SCHEMA catalog;
CREATE SCHEMA discovery;
DO $$ BEGIN
 IF EXISTS(SELECT 1 FROM public."Cities" WHERE "Id" <> 1 OR "Slug" <> 'almere') THEN
  RAISE EXCEPTION 'Unknown city requires reviewed settings before migration'; END IF;
 IF EXISTS(SELECT 1 FROM public."Cities" WHERE "Id" < 0)
 OR EXISTS(SELECT 1 FROM public."Categories" WHERE "Id" < 0)
 OR EXISTS(SELECT 1 FROM public."DiscoveryEntries" WHERE "Id" < 0) THEN
  RAISE EXCEPTION 'Unsupported legacy identifier'; END IF;
END $$;
ALTER TABLE public."Cities" ADD COLUMN uuid_id uuid;
UPDATE public."Cities" SET uuid_id = ('00000001-0000-8000-8000-' || lpad(to_hex("Id"),12,'0'))::uuid;
ALTER TABLE public."Cities" ALTER COLUMN uuid_id SET NOT NULL;
CREATE UNIQUE INDEX "Cities_uuid" ON public."Cities"(uuid_id);
ALTER TABLE public."Categories" ADD COLUMN uuid_id uuid;
UPDATE public."Categories" SET uuid_id = ('00000002-0000-8000-8000-' || lpad(to_hex("Id"),12,'0'))::uuid;
ALTER TABLE public."Categories" ALTER COLUMN uuid_id SET NOT NULL;
CREATE UNIQUE INDEX "Categories_uuid" ON public."Categories"(uuid_id);
ALTER TABLE public."DiscoveryEntries" ADD COLUMN uuid_id uuid;
UPDATE public."DiscoveryEntries" SET uuid_id = ('00000003-0000-8000-8000-' || lpad(to_hex("Id"),12,'0'))::uuid;
ALTER TABLE public."DiscoveryEntries" ALTER COLUMN uuid_id SET NOT NULL;
CREATE UNIQUE INDEX "DiscoveryEntries_uuid" ON public."DiscoveryEntries"(uuid_id);
ALTER TABLE public."DiscoveryEntries" ADD COLUMN uuid_city_id uuid;
ALTER TABLE public."DiscoveryEntries" ADD COLUMN uuid_category_id uuid;
UPDATE public."DiscoveryEntries" e SET uuid_city_id=c.uuid_id FROM public."Cities" c WHERE e."CityId"=c."Id";
UPDATE public."DiscoveryEntries" e SET uuid_category_id=c.uuid_id FROM public."Categories" c WHERE e."CategoryId"=c."Id";
ALTER TABLE public."DiscoveryEntries" ALTER COLUMN uuid_city_id SET NOT NULL;
ALTER TABLE public."DiscoveryEntries" ALTER COLUMN uuid_category_id SET NOT NULL;
-- Deterministic test failpoint; setting is session-local, not an application feature.
DO $$ BEGIN IF current_setting('happycity.fail_after_backfill', true) = 'on' THEN
 RAISE EXCEPTION 'Controlled migration failure after backfill'; END IF; END $$;
ALTER TABLE public."DiscoveryEntries" DROP CONSTRAINT "FK_DiscoveryEntries_Categories_CategoryId";
ALTER TABLE public."DiscoveryEntries" DROP CONSTRAINT "FK_DiscoveryEntries_Cities_CityId";
ALTER TABLE public."Cities" DROP CONSTRAINT "PK_Cities";
ALTER TABLE public."Cities" ALTER COLUMN "Id" DROP IDENTITY;
ALTER TABLE public."Cities" ALTER COLUMN "Id" DROP NOT NULL;
ALTER TABLE public."Cities" RENAME COLUMN "Id" TO legacy_id;
ALTER TABLE public."Cities" RENAME COLUMN "Slug" TO slug;
ALTER TABLE public."Cities" RENAME COLUMN "Name" TO name;
ALTER TABLE public."Cities" RENAME COLUMN uuid_id TO id;
ALTER TABLE public."Cities" ADD PRIMARY KEY(id);
CREATE UNIQUE INDEX "Cities_legacy" ON public."Cities"(legacy_id);
ALTER TABLE public."Cities" SET SCHEMA catalog;
ALTER TABLE catalog."Cities" RENAME TO cities;
ALTER TABLE public."Categories" DROP CONSTRAINT "PK_Categories";
ALTER TABLE public."Categories" ALTER COLUMN "Id" DROP IDENTITY;
ALTER TABLE public."Categories" ALTER COLUMN "Id" DROP NOT NULL;
ALTER TABLE public."Categories" RENAME COLUMN "Id" TO legacy_id;
ALTER TABLE public."Categories" RENAME COLUMN "Slug" TO slug;
ALTER TABLE public."Categories" RENAME COLUMN "Name" TO name;
ALTER TABLE public."Categories" RENAME COLUMN uuid_id TO id;
ALTER TABLE public."Categories" ADD PRIMARY KEY(id);
CREATE UNIQUE INDEX "Categories_legacy" ON public."Categories"(legacy_id);
ALTER TABLE public."Categories" SET SCHEMA catalog;
ALTER TABLE catalog."Categories" RENAME TO categories;
ALTER TABLE public."DiscoveryEntries" DROP CONSTRAINT "PK_DiscoveryEntries";
ALTER TABLE public."DiscoveryEntries" ALTER COLUMN "Id" DROP IDENTITY;
ALTER TABLE public."DiscoveryEntries" ALTER COLUMN "Id" DROP NOT NULL;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "Id" TO legacy_id;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "CityId" TO legacy_city_id;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "CategoryId" TO legacy_category_id;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "Slug" TO slug;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "Title" TO title;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "Description" TO description;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "District" TO district;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "Icon" TO icon;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN "IsPublished" TO is_published;
ALTER TABLE public."DiscoveryEntries" RENAME COLUMN uuid_id TO id;
ALTER TABLE public."DiscoveryEntries" ADD PRIMARY KEY(id);
CREATE UNIQUE INDEX "DiscoveryEntries_legacy" ON public."DiscoveryEntries"(legacy_id);
ALTER TABLE public."DiscoveryEntries" SET SCHEMA discovery;
ALTER TABLE discovery."DiscoveryEntries" RENAME TO discovery_entries;
ALTER TABLE discovery.discovery_entries RENAME COLUMN uuid_city_id TO city_id;
ALTER TABLE discovery.discovery_entries RENAME COLUMN uuid_category_id TO category_id;
ALTER TABLE discovery.discovery_entries ALTER COLUMN legacy_city_id DROP NOT NULL;
ALTER TABLE discovery.discovery_entries ALTER COLUMN legacy_category_id DROP NOT NULL;
ALTER TABLE discovery.discovery_entries ADD FOREIGN KEY(city_id) REFERENCES catalog.cities(id) ON DELETE RESTRICT;
ALTER TABLE discovery.discovery_entries ADD FOREIGN KEY(category_id) REFERENCES catalog.categories(id) ON DELETE RESTRICT;
CREATE UNIQUE INDEX entries_city_slug ON discovery.discovery_entries(city_id,slug);
CREATE INDEX entries_filter_order ON discovery.discovery_entries(city_id,is_published,category_id,legacy_id,id);
CREATE INDEX entries_page_order ON discovery.discovery_entries(city_id,(legacy_id IS NULL),legacy_id,id) WHERE is_published;
ALTER TABLE catalog.cities ADD country_code varchar(2), ADD default_language varchar(10), ADD time_zone varchar(100), ADD is_active boolean NOT NULL DEFAULT false,
 ADD created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP, ADD updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP;
UPDATE catalog.cities SET country_code='NL',default_language='nl',time_zone='Europe/Amsterdam',is_active=true WHERE legacy_id=1 AND slug='almere';
ALTER TABLE catalog.cities ALTER country_code SET NOT NULL, ALTER default_language SET NOT NULL, ALTER time_zone SET NOT NULL;
ALTER TABLE catalog.cities ADD CHECK(country_code ~ '^[A-Z]{2}$'), ADD CHECK(default_language ~ '^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$');
ALTER TABLE catalog.categories ADD parent_id uuid REFERENCES catalog.categories(id) ON DELETE RESTRICT,
 ADD category_type varchar(30) NOT NULL DEFAULT 'discovery', ADD display_order integer NOT NULL DEFAULT 0,
 ADD is_active boolean NOT NULL DEFAULT false, ADD created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP, ADD updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
 ADD CHECK(parent_id IS NULL OR parent_id<>id);
UPDATE catalog.categories SET display_order=legacy_id,is_active=true;
ALTER TABLE catalog.categories ALTER category_type DROP DEFAULT;
CREATE INDEX categories_parent ON catalog.categories(parent_id);
CREATE TABLE catalog.category_translations (
 category_id uuid NOT NULL REFERENCES catalog.categories(id) ON DELETE RESTRICT,
 language_code varchar(10) NOT NULL CHECK(language_code ~ '^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$'),
 name varchar(100) NOT NULL, description varchar(2000), PRIMARY KEY(category_id,language_code));
INSERT INTO catalog.category_translations(category_id,language_code,name) SELECT id,'nl',name FROM catalog.categories;
CREATE TABLE catalog.city_domains (
 id uuid PRIMARY KEY, city_id uuid NOT NULL REFERENCES catalog.cities(id) ON DELETE RESTRICT,
 hostname varchar(253) NOT NULL UNIQUE CHECK(hostname=lower(hostname) AND hostname ~ '^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$'),
 is_primary boolean NOT NULL, is_active boolean NOT NULL, created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE UNIQUE INDEX domains_primary ON catalog.city_domains(city_id) WHERE is_primary AND is_active;
CREATE TABLE catalog.districts (
 id uuid PRIMARY KEY, city_id uuid NOT NULL REFERENCES catalog.cities(id) ON DELETE RESTRICT,
 parent_id uuid, name varchar(100) NOT NULL, slug varchar(100) NOT NULL CHECK(slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'),
 external_code varchar(100), district_type varchar(30) NOT NULL, is_active boolean NOT NULL,
 created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP, updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
 UNIQUE(city_id,id), UNIQUE(city_id,slug), CHECK(parent_id IS NULL OR parent_id<>id),
 FOREIGN KEY(city_id,parent_id) REFERENCES catalog.districts(city_id,id) ON DELETE RESTRICT);
CREATE INDEX districts_parent ON catalog.districts(city_id,parent_id);
CREATE TABLE catalog.locations (
 id uuid PRIMARY KEY, city_id uuid NOT NULL REFERENCES catalog.cities(id) ON DELETE RESTRICT, district_id uuid,
 street varchar(200) NOT NULL, house_number varchar(20) NOT NULL CHECK(house_number ~ '^[0-9]+$'), house_number_suffix varchar(20),
 postal_code varchar(20) NOT NULL, country_code varchar(2) NOT NULL CHECK(country_code ~ '^[A-Z]{2}$'), bag_identifier varchar(16),
 latitude numeric(9,6), longitude numeric(9,6), created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP, updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
 FOREIGN KEY(city_id,district_id) REFERENCES catalog.districts(city_id,id) ON DELETE RESTRICT,
 CHECK((latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)));
CREATE INDEX locations_district ON catalog.locations(city_id,district_id);
-- Serialize hierarchy statements, including concurrent opposite reparenting.
-- READ COMMITTED is required so the row trigger sees prior committed changes after lock.
CREATE FUNCTION catalog.lock_hierarchy() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF current_setting('transaction_isolation') <> 'read committed' THEN RAISE EXCEPTION 'Hierarchy writes require read committed'; END IF;
 PERFORM pg_advisory_xact_lock(72002); RETURN NULL; END $$;
CREATE FUNCTION catalog.check_hierarchy() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE cycle_found boolean;
BEGIN
 IF NEW.parent_id IS NULL THEN RETURN NEW; END IF;
 IF TG_TABLE_NAME='categories' THEN
  WITH RECURSIVE ancestors AS (SELECT id,parent_id FROM catalog.categories WHERE id=NEW.parent_id
   UNION SELECT c.id,c.parent_id FROM catalog.categories c JOIN ancestors a ON c.id=a.parent_id)
  SELECT EXISTS(SELECT 1 FROM ancestors WHERE id=NEW.id) INTO cycle_found;
 ELSE
  WITH RECURSIVE ancestors AS (SELECT id,parent_id FROM catalog.districts WHERE id=NEW.parent_id
   UNION SELECT c.id,c.parent_id FROM catalog.districts c JOIN ancestors a ON c.id=a.parent_id)
  SELECT EXISTS(SELECT 1 FROM ancestors WHERE id=NEW.id) INTO cycle_found;
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
REVOKE ALL ON SCHEMA catalog,discovery FROM PUBLIC;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA catalog FROM PUBLIC;

-- Align physical names with the EF model for future forward migrations.
ALTER TABLE catalog.cities RENAME CONSTRAINT "CK_Cities_Slug" TO "cities_slug";
ALTER TABLE catalog.cities RENAME CONSTRAINT "Cities_pkey" TO "PK_cities";
ALTER TABLE catalog.categories RENAME CONSTRAINT "CK_Categories_Slug" TO "categories_slug";
ALTER TABLE catalog.categories RENAME CONSTRAINT "Categories_pkey" TO "PK_categories";
ALTER TABLE discovery.discovery_entries RENAME CONSTRAINT "CK_DiscoveryEntries_Slug" TO "entries_slug";
ALTER TABLE discovery.discovery_entries RENAME CONSTRAINT "CK_DiscoveryEntries_Title" TO "entries_title";
ALTER TABLE discovery.discovery_entries RENAME CONSTRAINT "DiscoveryEntries_pkey" TO "PK_discovery_entries";
ALTER TABLE discovery.discovery_entries RENAME CONSTRAINT "discovery_entries_city_id_fkey" TO "FK_discovery_entries_cities_city_id";
ALTER TABLE discovery.discovery_entries RENAME CONSTRAINT "discovery_entries_category_id_fkey" TO "FK_discovery_entries_categories_category_id";
ALTER TABLE catalog.cities RENAME CONSTRAINT "cities_country_code_check" TO "cities_country";
ALTER TABLE catalog.cities RENAME CONSTRAINT "cities_default_language_check" TO "cities_language";
ALTER TABLE catalog.categories RENAME CONSTRAINT "categories_parent_id_fkey" TO "FK_categories_categories_parent_id";
ALTER TABLE catalog.categories RENAME CONSTRAINT "categories_check" TO "categories_parent";
ALTER TABLE catalog.category_translations RENAME CONSTRAINT "category_translations_language_code_check" TO "translations_language";
ALTER TABLE catalog.category_translations RENAME CONSTRAINT "category_translations_pkey" TO "PK_category_translations";
ALTER TABLE catalog.category_translations RENAME CONSTRAINT "category_translations_category_id_fkey" TO "FK_category_translations_categories_category_id";
ALTER TABLE catalog.city_domains RENAME CONSTRAINT "city_domains_hostname_check" TO "domains_normalized";
ALTER TABLE catalog.city_domains RENAME CONSTRAINT "city_domains_pkey" TO "PK_city_domains";
ALTER TABLE catalog.city_domains RENAME CONSTRAINT "city_domains_city_id_fkey" TO "FK_city_domains_cities_city_id";
ALTER TABLE catalog.districts RENAME CONSTRAINT "districts_slug_check" TO "districts_slug";
ALTER TABLE catalog.districts RENAME CONSTRAINT "districts_check" TO "districts_parent";
ALTER TABLE catalog.districts RENAME CONSTRAINT "districts_pkey" TO "PK_districts";
ALTER TABLE catalog.districts RENAME CONSTRAINT "districts_city_id_id_key" TO "AK_districts_city_id_id";
ALTER TABLE catalog.districts RENAME CONSTRAINT "districts_city_id_fkey" TO "FK_districts_cities_city_id";
ALTER TABLE catalog.districts RENAME CONSTRAINT "districts_city_id_parent_id_fkey" TO "FK_districts_districts_city_id_parent_id";
ALTER TABLE catalog.locations RENAME CONSTRAINT "locations_house_number_check" TO "locations_number";
ALTER TABLE catalog.locations RENAME CONSTRAINT "locations_country_code_check" TO "locations_country";
ALTER TABLE catalog.locations RENAME CONSTRAINT "locations_check" TO "locations_coordinates";
ALTER TABLE catalog.locations RENAME CONSTRAINT "locations_pkey" TO "PK_locations";
ALTER TABLE catalog.locations RENAME CONSTRAINT "locations_city_id_fkey" TO "FK_locations_cities_city_id";
ALTER TABLE catalog.locations RENAME CONSTRAINT "locations_city_id_district_id_fkey" TO "FK_locations_districts_city_id_district_id";
ALTER TABLE catalog.city_domains DROP CONSTRAINT city_domains_hostname_key;
ALTER TABLE catalog.districts DROP CONSTRAINT districts_city_id_slug_key;
ALTER INDEX catalog."IX_Cities_Slug" RENAME TO "IX_cities_slug";
ALTER INDEX catalog."IX_Categories_Slug" RENAME TO "IX_categories_slug";
ALTER INDEX catalog."Cities_legacy" RENAME TO "IX_cities_legacy_id";
ALTER INDEX catalog."Categories_legacy" RENAME TO "IX_categories_legacy_id";
ALTER INDEX catalog."categories_parent" RENAME TO "IX_categories_parent_id";
ALTER INDEX catalog."domains_primary" RENAME TO "IX_city_domains_city_id";
ALTER INDEX catalog."districts_parent" RENAME TO "IX_districts_city_id_parent_id";
ALTER INDEX catalog."locations_district" RENAME TO "IX_locations_city_id_district_id";
ALTER INDEX discovery."DiscoveryEntries_legacy" RENAME TO "IX_discovery_entries_legacy_id";
ALTER INDEX discovery."entries_city_slug" RENAME TO "IX_discovery_entries_city_id_slug";
ALTER INDEX discovery."entries_filter_order" RENAME TO "IX_discovery_entries_city_id_is_published_category_id_legacy_i~";
CREATE UNIQUE INDEX "IX_city_domains_hostname" ON catalog.city_domains(hostname);
CREATE UNIQUE INDEX "IX_districts_city_id_slug" ON catalog.districts(city_id,slug);
CREATE INDEX "IX_discovery_entries_category_id" ON discovery.discovery_entries(category_id);
ALTER TABLE catalog.cities ALTER is_active DROP DEFAULT;
ALTER TABLE catalog.categories ALTER is_active DROP DEFAULT, ALTER display_order DROP DEFAULT;
-- Retained legacy relationship columns are archival compatibility metadata, not write-side keys.
