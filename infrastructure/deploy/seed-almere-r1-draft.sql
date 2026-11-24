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
   'Theater, muziek, architectuur en wisselende exposities direct aan het Weerwater in Almere Centrum. Goed bereikbaar te voet vanaf station Almere Centrum. Bron: kunstlinie.nl.',
   'Almere Stad', '☀', true),
  ('a1000000-0000-4000-8000-000000000002', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'stadslandgoed-de-kemphaan', 'Stadslandgoed De Kemphaan',
   'Groen landgoed midden in het Almeerderhout met boswandelingen, speelplekken, natuuractiviteiten en gezinsuitjes. Ideaal voor een ontspannen middag buiten. Bron: kemphaan.nl.',
   'Almere Hout', '♧', true),
  ('a1000000-0000-4000-8000-000000000003', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'corrosia-theater-expo-film', 'Corrosia Theater, Expo & Film',
   'Cultureel hart van Almere Haven met intieme theatervoorstellingen, filmhuisprogramma en eigentijdse tentoonstellingen op de Markt. Bron: corrosia.nl.',
   'Almere Haven', '☀', true),
  ('a1000000-0000-4000-8000-000000000004', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'almere-jungle', 'Almere Jungle',
   'Overdekte botanische tuin en dierenpark op Stadslandgoed De Kemphaan met tropische planten en opvangdieren. Populair gezinsuitje voor jong en oud. Bron: almerejungle.nl.',
   'Almere Hout', '♧', true),
  ('a1000000-0000-4000-8000-000000000005', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'natuurbelevingcentrum-de-oostvaarders', 'Natuurbelevingcentrum De Oostvaarders',
   'Startpunt aan de rand van de Oostvaardersplassen met panoramisch uitzicht over het natuurgebied, verrekijkers en gemarkeerde wandelroutes. Bron: stadennatuur.nl.',
   'Almere Buiten', '♧', true),
  ('a1000000-0000-4000-8000-000000000006', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000001',
   'vrijetijdscentrum-de-vrijbuiter', 'Vrijetijdscentrum De Vrijbuiter',
   'Overdekt zwembad in Almere Buiten voor recreatief zwemmen met het gezin, banenzwemmen en zwemlessen. Bron: almere.nl.',
   'Almere Buiten', '☀', true),

  -- 2. eten (00000002-0000-8000-8000-000000000002)
  ('a1000000-0000-4000-8000-000000000007', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'restaurant-bakboord', 'Restaurant Bakboord',
   'Restaurant en brasserie aan de Havenkom in Almere Haven met uitzicht over de jachthaven en het Gooimeer. Bron: restaurantbakboord.nl.',
   'Almere Haven', '♨', true),
  ('a1000000-0000-4000-8000-000000000008', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'brasserie-julia', 'Brasserie Julia',
   'Gezellige brasserie aan de Sluiskade bij het water in Almere Haven voor koffie, lunch of een uitgebreid diner. Bron: brasseriejulia.nl.',
   'Almere Haven', '♨', true),
  ('a1000000-0000-4000-8000-000000000009', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'de-harmonie-almere', 'De Harmonie Almere',
   'Horecaplek aan de levendige Havenkom voor lunch, diner en borrel, op loopafstand van de boulevard en het strandje. Bron: deharmoniealmere.nl.',
   'Almere Haven', '♨', true),
  ('a1000000-0000-4000-8000-000000000010', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'restaurant-dunes', 'Restaurant Dunes',
   'Restaurant in het duinlandschap van Almere Poort nabij het Almeerderstrand, goed te combineren met een strandwandeling. Bron: restaurantdunes.nl.',
   'Almere Poort', '♨', true),
  ('a1000000-0000-4000-8000-000000000011', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'poortdok-duin', 'Poortdok',
   'Horecalocatie direct aan de jachthaven en het water in DUIN (Almere Poort) voor een kop koffie, lunch of borrel. Bron: poortdok.nl.',
   'Almere Poort', '♨', true),
  ('a1000000-0000-4000-8000-000000000012', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000002',
   'grand-cafe-de-waterburght', 'Grand Café De Waterburght',
   'Eet- en drinkgelegenheid op Stadslandgoed De Kemphaan, ideaal als tussenstop tijdens een boswandeling of gezinsbezoek. Bron: kemphaan.nl.',
   'Almere Hout', '♨', true),

  -- 3. besparen (00000002-0000-8000-8000-000000000003)
  ('a1000000-0000-4000-8000-000000000013', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'rataplan-kringloop-almere-stad', 'RataPlan Kringloopwinkel Almere Stad',
   'Ruime kringloopwinkel voor tweedehands meubels, kleding, boeken en huishoudelijke spullen. Een duurzame manier om betaalbaar te winkelen in Almere Stad. Bron: rataplan.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000014', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'rataplan-kringloop-almere-haven', 'RataPlan Kringloopwinkel Almere Haven',
   'Kringloopwinkel in Almere Haven gericht op hergebruik en scherp geprijsde tweedehands artikelen voor huis en garderobe. Bron: rataplan.nl.',
   'Almere Haven', '€', true),
  ('a1000000-0000-4000-8000-000000000015', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'upcyclecentrum-almere', 'Upcyclecentrum Almere',
   'Gemeentelijk circulair centrum waar materialen een tweede leven krijgen en inwoners terechtkunnen voor duurzaam hergebruik en werkplaatsen. Bron: almere.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000016', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'de-nieuwe-bibliotheek-spreekuren', 'De Nieuwe Bibliotheek — Gratis Spreekuren',
   'Openbare bibliotheek in het Stadshart met gratis inloopspreekuren (o.a. digitale overheid, taal en geldzaken), studieplekken en ruilacties. Bron: denieuwebibliotheek.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000017', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'voedselbank-almere-vla', 'Voedselbank Almere & Stichting VLA',
   'Lokale ondersteuning met voedselhulp en het voorzieningenloket (o.a. kledingloket) voor Almeerse huishoudens met een krap budget. Bron: voedselbankalmere.nl.',
   'Almere Stad', '€', true),
  ('a1000000-0000-4000-8000-000000000018', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000003',
   'stichting-leergeld-almere', 'Stichting Leergeld Almere',
   'Helpt gezinnen in heel Almere met een laag inkomen zodat kinderen toch kunnen meedoen aan sport, cultuur, muziek en schoolactiviteiten. Bron: leergeld.nl.',
   'Almere (stadsbreed)', '€', true),

  -- 4. diensten (00000002-0000-8000-8000-000000000004)
  ('a1000000-0000-4000-8000-000000000019', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'de-schoor-welzijn', 'De Schoor — Welzijn & Buurtcentra',
   'Welzijnsorganisatie voor buurtcentra, jongerenwerk, wijkactiviteiten en laagdrempelige ondersteuning in alle stadsdelen van Almere. Bron: deschoor.nl.',
   'Almere (stadsbreed)', '⌂', true),
  ('a1000000-0000-4000-8000-000000000020', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'repair-cafe-de-ruimte', 'Repair Café Almere — De Ruimte',
   'Buurtinitiatief in Almere Poort waar vrijwilligers samen met bewoners kleine huishoudelijke apparaten, kleding en fietsen repareren. Bron: repaircafe-almere.nl.',
   'Almere Poort', '⌂', true),
  ('a1000000-0000-4000-8000-000000000021', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'repair-cafe-de-draaikolk', 'Repair Café Almere — De Draaikolk',
   'Lokaal reparatie-initiatief in buurtcentrum De Draaikolk (Almere Stad) om kapotte spullen samen een tweede leven te geven. Bron: repaircafe-almere.nl.',
   'Almere Stad', '⌂', true),
  ('a1000000-0000-4000-8000-000000000022', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'vmca-almere', 'VMCA — Vrijwilligers- & Mantelzorg Centrale',
   'Centraal steunpunt in Almere voor wie vrijwilligerswerk zoekt, een buurtinitiatief wil starten of ondersteuning nodig heeft als mantelzorger. Bron: vmca.nl.',
   'Almere Stad', '⌂', true),
  ('a1000000-0000-4000-8000-000000000023', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'gemeenteloket-almere', 'Gemeenteloket Almere (Stadhuisplein)',
   'Publieksbalie van Gemeente Almere op het Stadhuisplein voor paspoorten, burgerzaken, meldingen openbare ruimte en lokale regelingen. Bron: almere.nl.',
   'Almere Stad', '⌂', true),
  ('a1000000-0000-4000-8000-000000000024', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000004',
   'cultuurhuis-de-glasbak', 'Theater & Cultuurcentrum De Glasbak',
   'Buurtcentrum en theater in Muziekwijk (direct bij station Muziekwijk) voor cursussen, muzieklessen, voorstellingen en wijkbijeenkomsten. Bron: deglasbak-almere.nl.',
   'Almere Stad', '⌂', true),

  -- 5. winkelen (00000002-0000-8000-8000-000000000005)
  ('a1000000-0000-4000-8000-000000000025', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'almere-centrum-stadshart', 'Almere Centrum (Stadshart)',
   'Hoofdwinkelgebied van Almere met moderne architectuur, speciaalzaken, warenhuizen en gezellige horecapleinen rondom de Grote Markt. Bron: almerecentrum.nl.',
   'Almere Stad', '◇', true),
  ('a1000000-0000-4000-8000-000000000026', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'boekhandel-stumpel-almere', 'Boekhandel Stumpel Almere',
   'Ruime boek- en kantoorboekhandel met eigen koffiehoek in het centrum van Almere voor literatuur, kinderboeken en cadeaus. Bron: stumpel.nl.',
   'Almere Stad', '◇', true),
  ('a1000000-0000-4000-8000-000000000027', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'almere-buiten-centrum', 'Almere Buiten Centrum',
   'Overzichtelijk winkelcentrum rondom station Almere Buiten voor dagelijkse boodschappen, mode, verswinkels en weekmarkt. Bron: almerebuitencentrum.nl.',
   'Almere Buiten', '◇', true),
  ('a1000000-0000-4000-8000-000000000028', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'doemere-woonboulevard', 'DoeMere Woonboulevard',
   'Grote woon- en doe-het-zelfboulevard in Almere Buiten met winkels voor meubels, klussen, tuin en woninginrichting bij elkaar. Bron: doemere.nl.',
   'Almere Buiten', '◇', true),
  ('a1000000-0000-4000-8000-000000000029', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'weekmarkt-almere-haven', 'Weekmarkt Almere Haven',
   'Wekelijkse warenmarkt rond de Brink en Markt in het dorpscentrum van Almere Haven met verse producten, bloemen en lokale kramen. Bron: almere.nl.',
   'Almere Haven', '◇', true),
  ('a1000000-0000-4000-8000-000000000030', NULL, '00000001-0000-8000-8000-000000000001', '00000002-0000-8000-8000-000000000005',
   'boerenmarkt-de-stadsboerderij', 'Biologische Boerenmarkt De Stadsboerderij',
   'Wekelijkse biologische boerenmarkt op Stadslandgoed De Kemphaan met verse streekproducten rechtstreeks van lokale telers en makers. Bron: stadsboerderijalmere.nl.',
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
