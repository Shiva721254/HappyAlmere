-- HappyAlmere R1 Opt-In Draft Seed — 30 Public Almere Candidate Listings (R03-01..R03-30)
--
-- GOVERNANCE NOTICE:
--   * Strictly OPT-IN for local preview or staging review ONLY. Never invoke from EF migrations or CI.
--   * All 30 descriptions are original, neutral factual Dutch copy (zero scraped directory text,
--     zero personal PII, zero invented opening hours/prices/ratings, zero "geverifieerd" badges).
--   * Confirm a live correction/takedown email before enabling any public-facing staging/production display.

BEGIN;
SET LOCAL ROLE happy_owner;

-- Remove the 6 synthetic placeholder rows in preview/staging so only curated Almere entries appear.
DELETE FROM discovery.discovery_entries
WHERE city_id = '00000001-0000-8000-8000-000000000001'
  AND legacy_id IS NOT NULL;

INSERT INTO discovery.discovery_entries (id, legacy_id, city_id, category_id, slug, title, description, district, icon, is_published)
VALUES
  -- 1. activiteiten (00000002-0000-8000-8000-000000000001)
  ('a1000000-0000-4000-8000-000000000001', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'kunstlinie-almere', 'Kunstlinie',
   'Theater, podiumkunsten en exposities aan het Weerwater in Almere Centrum. Bron: kunstlinie.nl.',
   'Almere Stad', '☀', true),
  ('a1000000-0000-4000-8000-000000000002', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'stadslandgoed-de-kemphaan', 'Stadslandgoed De Kemphaan',
   'Groen landgoed met wandelroutes, natuuractiviteiten en gezinsuitjes in het bos. Bron: kemphaan.nl.',
   'Almere Hout', '♧', true),
  ('a1000000-0000-4000-8000-000000000003', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'corrosia-theater-expo-film', 'Corrosia Theater, Expo & Film',
   'Cultureel centrum in Almere Haven met theatervoorstellingen, film en tentoonstellingen. Bron: corrosia.nl.',
   'Almere Haven', '☀', true),
  ('a1000000-0000-4000-8000-000000000004', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'almere-jungle', 'Almere Jungle',
   'Dierenpark en botanische tuin op Stadslandgoed De Kemphaan voor gezinnen en kinderen. Bron: almerejungle.nl.',
   'Almere Hout', '♧', true),
  ('a1000000-0000-4000-8000-000000000005', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'natuurbelevingcentrum-de-oostvaarders', 'Natuurbelevingcentrum De Oostvaarders',
   'Bezoekerscentrum aan de rand van de Oostvaardersplassen met uitzicht en wandelroutes. Bron: stadennatuur.nl.',
   'Almere Buiten', '♧', true),
  ('a1000000-0000-4000-8000-000000000006', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'vrijetijdscentrum-de-vrijbuiter', 'Vrijetijdscentrum De Vrijbuiter',
   'Overdekt zwembad in Almere Buiten voor recreatief zwemmen, banenzwemmen en zwemles. Bron: almere.nl.',
   'Almere Buiten', '☀', true),

  -- 2. eten (00000002-0000-8000-8000-000000000002)
  ('a1000000-0000-4000-8000-000000000007', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'restaurant-bakboord', 'Restaurant Bakboord',
   'Restaurant en brasserie aan de Havenkom in Almere Haven. Bron: restaurantbakboord.nl.',
   'Almere Haven', '♨', true),
  ('a1000000-0000-4000-8000-000000000008', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'brasserie-julia', 'Brasserie Julia',
   'Brasserie aan de Sluiskade bij het water in Almere Haven. Bron: brasseriejulia.nl.',
   'Almere Haven', '♨', true),
  ('a1000000-0000-4000-8000-000000000009', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'de-harmonie-almere', 'De Harmonie Almere',
   'Horecagelegenheid aan de Havenkom voor lunch, diner en borrel. Bron: deharmoniealmere.nl.',
   'Almere Haven', '♨', true),
  ('a1000000-0000-4000-8000-000000000010', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'restaurant-dunes', 'Restaurant Dunes',
   'Restaurant in het kustgebied DUIN nabij het Almeerderstrand. Bron: restaurantdunes.nl.',
   'Almere Poort', '♨', true),
  ('a1000000-0000-4000-8000-000000000011', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'poortdok-duin', 'Poortdok',
   'Horecaplek aan het water in Almere Poort en DUIN. Bron: poortdok.nl.',
   'Almere Poort', '♨', true),
  ('a1000000-0000-4000-8000-000000000012', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'grand-cafe-de-waterburght', 'Grand Café De Waterburght',
   'Horecagelegenheid op Stadslandgoed De Kemphaan, te combineren met een boswandeling. Bron: kemphaan.nl.',
   'Almere Hout', '♨', true),

  -- 3. besparen (00000002-0000-8000-8000-000000000003)
  ('a1000000-0000-4000-8000-000000000013', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'rataplan-kringloop-almere-stad', 'RataPlan Kringloopwinkel Almere Stad',
   'Tweedehands winkel voor meubels, kleding, boeken en huishoudelijke spullen. Bron: rataplan.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000014', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'rataplan-kringloop-almere-haven', 'RataPlan Kringloopwinkel Almere Haven',
   'Kringloopwinkel in Almere Haven voor hergebruik en betaalbare tweedehands artikelen. Bron: rataplan.nl.',
   'Almere Haven', '€', true),
  ('a1000000-0000-4000-8000-000000000015', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'upcyclecentrum-almere', 'Upcyclecentrum Almere',
   'Gemeentelijk circulair centrum gericht op hergebruik en het terugdringen van afval. Bron: almere.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000016', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'de-nieuwe-bibliotheek-spreekuren', 'De Nieuwe Bibliotheek — Gratis Spreekuren',
   'Openbare bibliotheek met gratis inloopspreekuren (o.a. digitale overheid en geldzaken) en ruilacties. Bron: denieuwebibliotheek.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000017', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'voedselbank-almere-vla', 'Voedselbank Almere & Stichting VLA',
   'Lokale hulpverlening met voedselhulp en het voorzieningenloket (o.a. kledingloket) voor inwoners met een krap budget. Bron: voedselbankalmere.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000018', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'stichting-leergeld-almere', 'Stichting Leergeld Almere',
   'Ondersteuning zodat kinderen uit gezinnen met weinig financiële ruimte kunnen meedoen aan sport, cultuur en school. Bron: leergeld.nl.',
   'Almere (stadsbreed)', '€', true),

  -- 4. diensten (00000002-0000-8000-8000-000000000004)
  ('a1000000-0000-4000-8000-000000000019', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'de-schoor-welzijn', 'De Schoor — Welzijn & Buurtcentra',
   'Welzijnsorganisatie voor buurtcentra, wijkactiviteiten en laagdrempelige ondersteuning in alle stadsdelen. Bron: deschoor.nl.',
   'Almere (stadsbreed)', '⌂', true),
  ('a1000000-0000-4000-8000-000000000020', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'repair-cafe-de-ruimte', 'Repair Café Almere — De Ruimte',
   'Vrijwilligers helpen buurtbewoners samen kleine apparaten, kleding of fietsen te repareren. Bron: repaircafe-almere.nl.',
   'Almere Poort', '⌂', true),
  ('a1000000-0000-4000-8000-000000000021', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'repair-cafe-de-draaikolk', 'Repair Café Almere — De Draaikolk',
   'Lokaal reparatie-initiatief in buurtcentrum De Draaikolk voor duurzaam herstel van spullen. Bron: repaircafe-almere.nl.',
   'Almere Stad', '⌂', true),
  ('a1000000-0000-4000-8000-000000000022', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'vmca-almere', 'VMCA — Vrijwilligers- & Mantelzorg Centrale',
   'Steunpunt voor vrijwilligerswerk en mantelzorgondersteuning voor inwoners van Almere. Bron: vmca.nl.',
   'Almere Stad', '⌂', true),
  ('a1000000-0000-4000-8000-000000000023', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'gemeenteloket-almere', 'Gemeenteloket Almere (Stadhuisplein)',
   'Publieksloket van Gemeente Almere voor burgerzaken, meldingen openbare ruimte en lokale regelingen. Bron: almere.nl.',
   'Almere Stad', '⌂', true),
  ('a1000000-0000-4000-8000-000000000024', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'cultuurhuis-de-glasbak', 'Theater & Cultuurcentrum De Glasbak',
   'Buurtcentrum en theater bij station Muziekwijk voor cursussen, muzieklessen en wijkactiviteiten. Bron: deglasbak-almere.nl.',
   'Muziekwijk', '⌂', true),

  -- 5. winkelen (00000002-0000-8000-8000-000000000005)
  ('a1000000-0000-4000-8000-000000000025', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'almere-centrum-stadshart', 'Almere Centrum (Stadshart)',
   'Hoofdwinkelgebied van Almere met landelijke ketens, speciaalzaken en horecapleinen. Bron: almerecentrum.nl.',
   'Almere Stad', '◇', true),
  ('a1000000-0000-4000-8000-000000000026', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'boekhandel-stumpel-almere', 'Boekhandel Stumpel Almere',
   'Boek- en kantoorboekhandel met koffiehoek in het centrum van Almere. Bron: stumpel.nl.',
   'Almere Stad', '◇', true),
  ('a1000000-0000-4000-8000-000000000027', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'almere-buiten-centrum', 'Almere Buiten Centrum',
   'Winkelcentrum in Almere Buiten voor dagelijkse boodschappen, mode en lokale winkels. Bron: almerebuitencentrum.nl.',
   'Almere Buiten', '◇', true),
  ('a1000000-0000-4000-8000-000000000028', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'doemere-woonboulevard', 'DoeMere Woonboulevard',
   'Woon- en doe-het-zelfboulevard in Almere Buiten met winkels voor huis, tuin en inrichting. Bron: doemere.nl.',
   'Almere Buiten', '◇', true),
  ('a1000000-0000-4000-8000-000000000029', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'weekmarkt-almere-haven', 'Weekmarkt Almere Haven',
   'Wekelijkse warenmarkt rond de Brink en Markt in het centrum van Almere Haven. Bron: almere.nl.',
   'Almere Haven', '◇', true),
  ('a1000000-0000-4000-8000-000000000030', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'boerenmarkt-de-stadsboerderij', 'Biologische Boerenmarkt De Stadsboerderij',
   'Wekelijkse biologische boerenmarkt met lokale streekproducten op Stadslandgoed De Kemphaan. Bron: stadsboerderijalmere.nl.',
   'Almere Hout', '◇', true)
ON CONFLICT (city_id, slug) DO UPDATE SET
  category_id = EXCLUDED.category_id,
  title = EXCLUDED.title,
  description = EXCLUDED.description,
  district = EXCLUDED.district,
  icon = EXCLUDED.icon,
  is_published = EXCLUDED.is_published;

RESET ROLE;
COMMIT;

ANALYZE discovery.discovery_entries;
