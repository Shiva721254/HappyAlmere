"""HA-002-A synthetic-only backup/restore gate. Never modifies the source schema/data.

Uses Docker/PostgreSQL already present in the project; Python standard library only.
Archives, globals and generated credentials stay in an access-restricted ignored folder.
Only the explicit sanitized evidence JSON is suitable for publication after review.
"""
import argparse
import copy
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import socket
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
TABLES = ['Categories', 'Cities', 'DiscoveryEntries', '__EFMigrationsHistory']
SOURCE_PROJECT = 'happyalmere-v02'


def run(*args, input=None, env=None, binary=False):
    result = subprocess.run(args, input=input, env=env, stdout=subprocess.PIPE,
                            stderr=subprocess.PIPE, text=not binary, encoding=None if binary else 'utf-8')
    if result.returncode:
        # Do not echo command/input/output: database errors could contain row or credential data.
        raise RuntimeError(f'{args[0]} operation failed (exit {result.returncode}); inspect protected local diagnostics if needed.')
    return result.stdout


def docker(*args, **kwargs):
    return run('docker', *args, **kwargs)


def sql(container, statement):
    return docker('exec', '-i', '-e', 'PGOPTIONS=-c default_transaction_read_only=on', container, 'psql', '-X', '-qAt', '-v', 'ON_ERROR_STOP=1',
                  '-U', 'happy_migrator', '-d', 'happyalmere', input=statement)


def query(container, statement):
    return json.loads(sql(container, statement))


def digest(value):
    return hashlib.sha256(json.dumps(value, ensure_ascii=False, sort_keys=True,
                                    separators=(',', ':')).encode('utf-8')).hexdigest()


def protect(directory):
    if os.name == 'nt':
        sid = run('whoami', '/user', '/fo', 'csv', '/nh')
        match = re.search(r'S-1-5-[0-9-]+', sid)
        if not match:
            raise RuntimeError('Cannot identify current Windows SID for protected backup ACL.')
        run('icacls', str(directory), '/inheritance:r', '/grant:r',
            f'*{match[0]}:(OI)(CI)F', '*S-1-5-18:(OI)(CI)F')
        # Newly created directory has no explicit grants beyond owner/SYSTEM.
        acl = run('icacls', str(directory))
        if '(I)' in acl:
            raise RuntimeError('Unexpected inherited backup permissions.')
        return 'NTFS ACL: current user and SYSTEM only; inheritance removed'
    directory.chmod(0o700)
    return 'POSIX directory mode 0700; files 0600'


def private_write(path, data):
    path.write_bytes(data if isinstance(data, bytes) else data.encode('utf-8'))
    if os.name != 'nt':
        path.chmod(0o600)


def inventory(container):
    # Separate read-only transactions; the source is expected to be quiescent synthetic data.
    tables = query(container, "SELECT coalesce(json_agg(tablename ORDER BY tablename),'[]') FROM pg_tables WHERE schemaname='public';")
    if tables != TABLES:
        raise RuntimeError('Source/restore table inventory differs from the known synthetic baseline; stop for review.')
    rows = {}
    for table in TABLES:
        rows[table] = query(container, f'SELECT coalesce(json_agg(t ORDER BY row_to_json(t)::text),\'[]\') FROM public."{table}" t;')
    return {
        'rows': rows,
        'schemas': query(container, "SELECT json_agg(t ORDER BY nspname) FROM (SELECT nspname,pg_get_userbyid(nspowner) AS owner FROM pg_namespace WHERE nspname NOT LIKE 'pg_%' AND nspname <> 'information_schema') t;"),
        'tablePrivileges': query(container, "SELECT json_agg(t ORDER BY table_name,grantee,privilege_type) FROM (SELECT table_name,grantee,privilege_type,is_grantable FROM information_schema.table_privileges WHERE table_schema='public') t;"),
        'columns': query(container, "SELECT json_agg(t ORDER BY table_name,ordinal_position) FROM (SELECT table_name,column_name,ordinal_position,data_type,is_nullable,column_default,character_maximum_length FROM information_schema.columns WHERE table_schema='public') t;"),
        'constraints': query(container, "SELECT json_agg(t ORDER BY table_name,name) FROM (SELECT c.relname AS table_name,con.conname AS name,con.contype AS type,pg_get_constraintdef(con.oid) AS definition FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public') t;"),
        'indexes': query(container, "SELECT json_agg(t ORDER BY tablename,indexname) FROM (SELECT tablename,indexname,indexdef FROM pg_indexes WHERE schemaname='public') t;"),
        'sequences': query(container, "SELECT json_agg(t ORDER BY sequencename) FROM (SELECT sequencename,start_value,min_value,max_value,increment_by,cycle,last_value FROM pg_sequences WHERE schemaname='public') t;"),
        'roles': query(container, "SELECT json_agg(t ORDER BY rolname) FROM (SELECT rolname,rolsuper,rolcreatedb,rolcreaterole,rolcanlogin,rolreplication,rolbypassrls FROM pg_roles WHERE rolname IN ('happy_app','happy_migrator')) t;"),
        'invariants': query(container, '''SELECT json_build_object(
            'entries',(SELECT count(*) FROM "DiscoveryEntries"),
            'published',(SELECT count(*) FROM "DiscoveryEntries" WHERE "IsPublished"),
            'unpublished',(SELECT count(*) FROM "DiscoveryEntries" WHERE NOT "IsPublished"),
            'orphanCities',(SELECT count(*) FROM "DiscoveryEntries" e LEFT JOIN "Cities" c ON e."CityId"=c."Id" WHERE c."Id" IS NULL),
            'orphanCategories',(SELECT count(*) FROM "DiscoveryEntries" e LEFT JOIN "Categories" c ON e."CategoryId"=c."Id" WHERE c."Id" IS NULL),
            'duplicateCitySlugs',(SELECT count(*) FROM (SELECT "CityId","Slug" FROM "DiscoveryEntries" GROUP BY "CityId","Slug" HAVING count(*)>1) x),
            'familySearch',(SELECT count(*) FROM "DiscoveryEntries" WHERE "IsPublished" AND "Title" ILIKE '%familie%'),
            'activities',(SELECT count(*) FROM "DiscoveryEntries" e JOIN "Categories" c ON c."Id"=e."CategoryId" WHERE e."IsPublished" AND c."Slug"='activiteiten'));
        ''')
    }


def verify_synthetic(snapshot):
    source = run('git', 'show', '3287994afb0bb3fe7e69f60a6bda689f77c40323:HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/SyntheticDiscoverySeed.cs')
    entries = re.findall(r'Entry\((\d+), (\d+), "([^"]*)", "([^"]*)", "([^"]*)", "([^"]*)", "([^"]*)"\)', source)
    expected = [{'Id': int(i), 'CityId': 1, 'CategoryId': int(c), 'Slug': s, 'Title': t,
                 'Description': d, 'District': district, 'Icon': icon, 'IsPublished': True}
                for i,c,s,t,d,district,icon in entries]
    if len(expected) != 6 or sorted(expected, key=lambda x:x['Id']) != sorted(snapshot['rows']['DiscoveryEntries'], key=lambda x:x['Id']):
        raise RuntimeError('Source is not the exact six known synthetic discovery fixtures; review before backup.')
    if snapshot['rows']['Cities'] != [{'Id': 1, 'Slug': 'almere', 'Name': 'Almere'}]:
        raise RuntimeError('Source city inventory is not the known synthetic baseline.')
    categories = re.findall(r'new Category \{ Id = (\d+), Slug = \"([^\"]*)\", Name = \"([^\"]*)\" \}', source)
    expected_categories = [{'Id': int(i), 'Slug': slug, 'Name': name} for i,slug,name in categories]
    if len(expected_categories) != 5 or sorted(expected_categories,key=lambda x:x['Id']) != sorted(snapshot['rows']['Categories'],key=lambda x:x['Id']):
        raise RuntimeError('Unexpected category content; only known synthetic fixtures are authorized.')
    if snapshot['invariants']['orphanCities'] or snapshot['invariants']['orphanCategories']:
        raise RuntimeError('Baseline referential integrity failed.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-container', default='happyalmere-v02-postgres-1')
    parser.add_argument('--port', type=int, default=55433)
    parser.add_argument('--leave-target-running', action='store_true', help='Keep isolated target for follow-up regression tests; source lifecycle is always restored.')
    args = parser.parse_args()
    if not re.fullmatch(r'[a-zA-Z0-9_.-]+', args.source_container) or not 1024 <= args.port <= 65535:
        raise RuntimeError('Invalid local container/port input.')
    os.chdir(ROOT)
    endpoint = json.loads(docker('context', 'inspect'))[0]['Endpoints']['docker']['Host']
    if not endpoint.startswith(('npipe://','unix://')):
        raise RuntimeError('Only a local Docker context is authorized.')
    source = json.loads(docker('inspect', args.source_container))[0]
    if source['Config']['Labels'].get('com.docker.compose.project') != SOURCE_PROJECT:
        raise RuntimeError('Source is not the preserved local synthetic project.')
    bindings = source['HostConfig']['PortBindings'].get('5432/tcp')
    if not bindings or any(x['HostIp'] != '127.0.0.1' for x in bindings):
        raise RuntimeError('Source database binding is not loopback-only.')
    with socket.socket() as probe:
        probe.bind(('127.0.0.1',args.port))  # Fail rather than reusing another database's listener.
    stamp = dt.datetime.now(dt.timezone.utc).strftime('%Y%m%d%H%M%S')+'-'+secrets.token_hex(3)
    project = 'ha002a-'+stamp
    directory = ROOT/'artifacts'/'ha002a'/stamp
    directory.mkdir(parents=True,exist_ok=False)
    protection = protect(directory)
    env = dict(os.environ, POSTGRES_PASSWORD=secrets.token_hex(32), HAPPY_APP_PASSWORD=secrets.token_hex(32), HA_RECOVERY_PORT=str(args.port))
    compose = ['compose','-p',project,'-f',str(ROOT/'infrastructure/recovery/compose.yaml')]
    source_started = not source['State']['Running']
    target = None
    target_attempted = False
    succeeded = False
    evidence = {'task':'HA-002-A backup/restore gate','startedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),
                'implementationBaseline':run('git','rev-parse','HEAD').strip(),
                'developBaseline':run('git','rev-parse','refs/remotes/origin/develop').strip(),
                'project':project,'backupProtection':protection,'sourceImage':source['Config']['Image'],
                'limitations':['Only exact known synthetic fixtures accepted.','Local ACL-protected storage; encryption-at-rest status of host disk not asserted.','Single logical database restore, not point-in-time or production recovery.','Current baseline administrative role design is inventoried, not hardened by this gate.']}
    try:
        if source_started:
            docker('start',args.source_container)
        for _ in range(30):
            ready = subprocess.run(['docker','exec',args.source_container,'pg_isready','-U','happy_migrator','-d','happyalmere'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
            if ready.returncode == 0: break
            time.sleep(1)
        before = inventory(args.source_container)
        verify_synthetic(before)
        rejected_fixtures = []
        for table, field, value in [('DiscoveryEntries','Title','Unexpected non-fixture text'), ('DiscoveryEntries','IsPublished',False), ('Cities','Name','Unexpected city'), ('Categories','Name','Unexpected category')]:
            altered = copy.deepcopy(before)
            altered['rows'][table][0][field] = value
            try:
                verify_synthetic(altered)
            except RuntimeError:
                rejected_fixtures.append(f'{table}.{field}')
            else:
                raise RuntimeError('Synthetic-only input guard failed its negative verification.')
        evidence['negativeFixtureChecks'] = rejected_fixtures
        evidence['postgresVersion'] = sql(args.source_container,'SHOW server_version;').strip()
        evidence['dumpVersion'] = docker('exec',args.source_container,'pg_dump','--version').strip()
        evidence['baseline'] = {k:v for k,v in before.items() if k != 'rows'}
        evidence['tableCounts'] = {k:len(v) for k,v in before['rows'].items()}
        evidence['tableDigests'] = {k:digest(v) for k,v in before['rows'].items()}
        evidence['migrationHistory'] = before['rows']['__EFMigrationsHistory']
        private_write(directory/'baseline-private.json',json.dumps(before,ensure_ascii=False,indent=2))
        start = time.monotonic()
        # Binary stdout goes directly into a protected file; no archive or SQL is printed.
        archive = docker('exec',args.source_container,'pg_dump','-U','happy_migrator','-d','happyalmere','--format=custom',binary=True)
        private_write(directory/'baseline.dump',archive)
        globals_sql = docker('exec',args.source_container,'pg_dumpall','-U','happy_migrator','--globals-only','--no-role-passwords')
        private_write(directory/'globals-no-passwords.sql',globals_sql)
        evidence['backupSeconds'] = round(time.monotonic()-start,3)
        evidence['backupSha256'] = hashlib.sha256(archive).hexdigest()
        evidence['backupBytes'] = len(archive)
        evidence['globalsSha256'] = hashlib.sha256(globals_sql.encode()).hexdigest()
        if inventory(args.source_container) != before:
            raise RuntimeError('Source changed during backup; cannot claim a quiescent reconciled snapshot.')
        target_attempted = True
        docker(*compose,'up','-d','--wait','postgres',env=env)
        target = docker(*compose,'ps','-q','postgres',env=env).strip()
        target_info = json.loads(docker('inspect',target))[0]
        if target == source['Id'] or target_info['Config']['Labels']['com.docker.compose.project'] != project:
            raise RuntimeError('Isolation check failed.')
        source_volumes = {m['Name'] for m in source['Mounts'] if m['Type']=='volume'}
        target_volumes = {m['Name'] for m in target_info['Mounts'] if m['Type']=='volume'}
        if source_volumes & target_volumes:
            raise RuntimeError('Source/target volumes overlap.')
        empty = query(target,"SELECT coalesce(json_agg(tablename),'[]') FROM pg_tables WHERE schemaname='public';")
        if empty:
            raise RuntimeError('Refusing restore into a populated target database.')
        if hashlib.sha256((directory/'baseline.dump').read_bytes()).hexdigest() != evidence['backupSha256']:
            raise RuntimeError('Backup checksum mismatch.')
        # Negative rehearsal: a truncated archive must fail and leave the fresh database empty.
        invalid = subprocess.run(['docker','exec','-i',target,'pg_restore','-U','happy_migrator','-d','happyalmere','--no-owner','--no-acl','--exit-on-error','--single-transaction'], input=archive[:32],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
        if invalid.returncode == 0 or query(target,"SELECT coalesce(json_agg(tablename),'[]') FROM pg_tables WHERE schemaname='public';"):
            raise RuntimeError('Invalid-archive safety test did not fail cleanly.')
        evidence['failedRestoreThenRecovery'] = 'Truncated archive rejected; target remained empty; valid archive then restored.'
        start = time.monotonic()
        docker('exec','-i',target,'pg_restore','-U','happy_migrator','-d','happyalmere','--no-owner','--no-acl','--exit-on-error','--single-transaction',input=archive,binary=True)
        evidence['restoreSeconds'] = round(time.monotonic()-start,3)
        restored = inventory(target)
        if before != restored:
            raise RuntimeError('Restored inventory does not exactly match baseline.')
        if inventory(args.source_container) != before:
            raise RuntimeError('Source changed during restore rehearsal.')
        certificate = docker('exec',target,'cat','/var/lib/postgresql-tls/server.crt')
        private_write(directory/'postgres.crt',certificate)
        common = f'Host=localhost;Port={args.port};Database=happyalmere;SSL Mode=VerifyFull;Root Certificate={(directory/"postgres.crt").as_posix()}'
        # Environment file remains inside the protected ignored folder, never root tracked config.
        private_write(directory/'rehearsal.env',f'ConnectionStrings__HappyCity={common};Username=happy_app;Password={env["HAPPY_APP_PASSWORD"]}\nHAPPYCITY_MIGRATION_CONNECTION={common};Username=happy_migrator;Password={env["POSTGRES_PASSWORD"]}\n')
        evidence['targetContainer'] = target_info['Name'].lstrip('/')
        evidence['targetBindings'] = target_info['HostConfig']['PortBindings']
        evidence['isolatedVolumes'] = sorted(target_volumes)
        evidence['checks'] = {name:'PASS' for name in ['exactSyntheticBaseline','backupChecksum','freshEmptyTarget','independentVolumes','allRowContent','counts','constraints','indexes','sequences','migrationHistory','roleAttributes','publicationState','foreignKeys','representativeQueries','sourceUnchanged']}
        evidence['gate'] = 'PASS: clean isolated backup/restore verified before schema modification'
        evidence['completedUtc'] = dt.datetime.now(dt.timezone.utc).isoformat()
        evidence['targetLeftRunning'] = args.leave_target_running
        private_write(directory/'evidence.json',json.dumps(evidence,indent=2)+'\n')
        succeeded = True
        print(json.dumps({'gate':evidence['gate'],'evidence':str(directory.relative_to(ROOT)/'evidence.json'),'project':project,'tableCounts':evidence['tableCounts'],'backupSha256':evidence['backupSha256'],'targetLeftRunning':args.leave_target_running},indent=2))
    finally:
        if source_started:
            docker('stop',args.source_container)
        if target_attempted and (not succeeded or not args.leave_target_running):
            docker(*compose,'stop','postgres',env=env)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(f'FAIL: {error}',file=sys.stderr)
        sys.exit(1)
