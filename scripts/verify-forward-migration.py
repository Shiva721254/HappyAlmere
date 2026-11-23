"""Isolated HA-002-A forward rehearsal. Requires a successful preserved-source recovery gate.
No source mutation. Private archives/credentials are never evidence artifacts.
"""
import re
import copy
import argparse, datetime as dt, hashlib, importlib.util, json, os, secrets, subprocess, time
from pathlib import Path
spec=importlib.util.spec_from_file_location("recovery",Path(__file__).with_name("verify-database-recovery.py"))
r=importlib.util.module_from_spec(spec); spec.loader.exec_module(r)
ROOT=r.ROOT

def execute(container,sql,user="happy_migrator",password=None,options=None,expected_state=None,expected_reason=None):
    env=dict(os.environ)
    args=['docker','exec','-i']
    if password:
        env['PGPASSWORD']=password; args += ['-e','PGPASSWORD']
    if options: args += ['-e','PGOPTIONS='+options]
    args += [container,'psql','-X','-qAt','-v','ON_ERROR_STOP=1','-v','VERBOSITY=verbose','-U',user,'-d','happyalmere']
    if password: args += ['--dbname=host=localhost dbname=happyalmere sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt user='+user]
    result=subprocess.run(args,input=sql,text=True,encoding='utf-8',stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env)
    if expected_state:
        errors=re.findall(r'(?:ERROR|FATAL):\s+([0-9A-Z]{5}):\s+([^\r\n]+)',result.stderr)
        if result.returncode == 0 or len(errors) != 1 or errors[0][0] != expected_state:
            raise RuntimeError('Negative verification rejected unexpected outcome; expected SQLSTATE '+expected_state)
        if expected_reason is not None and errors[0][1] != expected_reason:
            raise RuntimeError('Expected controlled failure reason was not reached')
        return expected_state
    if result.returncode: raise RuntimeError('Isolated SQL failed (diagnostics withheld to prevent data/credential exposure).')
    return result.stdout.strip()

def new_target(directory,label,port,archive=None):
    project='ha002f-'+directory.name+'-'+label
    env=dict(os.environ,HA_FORWARD_NETWORK=os.environ['HA_FORWARD_NETWORK'],POSTGRES_PASSWORD=secrets.token_hex(32),HAPPY_APP_PASSWORD=secrets.token_hex(32),HA_RECOVERY_PORT=str(port))
    compose=['compose','-p',project,'-f',str(ROOT/'infrastructure/forward/compose.yaml')]
    r.docker(*compose,'up','-d','--wait','postgres',env=env)
    container=r.docker(*compose,'ps','-q','postgres',env=env).strip()
    info=json.loads(r.docker('inspect',container))[0]
    if info['Config']['Labels']['com.docker.compose.project']!=project: raise RuntimeError('Target isolation label mismatch')
    if any(b['HostIp']!='127.0.0.1' for b in info['HostConfig']['PortBindings']['5432/tcp']): raise RuntimeError('Non-loopback target')
    volumes={m['Name'] for m in info['Mounts'] if m['Type']=='volume'}
    existing_file=directory/'volumes-private.json'
    existing=set(json.loads(existing_file.read_text())) if existing_file.exists() else set()
    if volumes & existing:raise RuntimeError('Isolated instances share a volume')
    r.private_write(existing_file,json.dumps(sorted(existing|volumes)))
    if archive:
        r.docker('exec','-i',container,'pg_restore','-U','happy_migrator','-d','happyalmere','--no-owner','--no-acl','--single-transaction','--exit-on-error',input=archive,binary=True)
    return container,env,info

def roles(container,env):
    env['MIGRATION_PASSWORD']=secrets.token_hex(32);env['BACKUP_PASSWORD']=secrets.token_hex(32)
    execute(container,f"""
CREATE ROLE happy_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
CREATE ROLE happy_schema_migrator LOGIN PASSWORD '{env['MIGRATION_PASSWORD']}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
CREATE ROLE happy_backup LOGIN PASSWORD '{env['BACKUP_PASSWORD']}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
GRANT happy_owner TO happy_schema_migrator;
REVOKE ALL ON DATABASE happyalmere FROM PUBLIC;
GRANT CONNECT ON DATABASE happyalmere TO happy_owner,happy_schema_migrator,happy_app,happy_backup;
GRANT CREATE ON DATABASE happyalmere TO happy_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE,CREATE ON SCHEMA public TO happy_owner;
GRANT USAGE ON SCHEMA public TO happy_app,happy_backup;
DO $$ DECLARE t record; BEGIN FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='public' LOOP
 EXECUTE format('ALTER TABLE public.%I OWNER TO happy_owner',t.tablename); END LOOP; END $$;
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA public REVOKE ALL ON TABLES FROM happy_app,happy_backup;
""")

def grants(container):
    execute(container,"""GRANT USAGE ON SCHEMA catalog,discovery TO happy_app,happy_backup;
GRANT SELECT ON catalog.cities,catalog.categories,catalog.category_translations,catalog.city_domains,catalog.districts,catalog.locations,discovery.discovery_entries TO happy_app,happy_backup;
GRANT SELECT ON public."__EFMigrationsHistory" TO happy_backup;
REVOKE ALL ON public."__EFMigrationsHistory" FROM happy_app;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA catalog FROM PUBLIC;
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA catalog,discovery REVOKE ALL ON TABLES FROM happy_app,happy_backup;
""")

def canonical(container):
    return json.loads(execute(container,"""SELECT json_build_object(
'Cities',(SELECT json_agg(x ORDER BY "Id") FROM (SELECT legacy_id AS "Id",name AS "Name",slug AS "Slug" FROM catalog.cities) x),
'Categories',(SELECT json_agg(x ORDER BY "Id") FROM (SELECT legacy_id AS "Id",name AS "Name",slug AS "Slug" FROM catalog.categories) x),
'DiscoveryEntries',(SELECT json_agg(x ORDER BY "Id") FROM (SELECT e.legacy_id AS "Id",c.legacy_id AS "CityId",k.legacy_id AS "CategoryId",e.slug AS "Slug",title AS "Title",description AS "Description",district AS "District",icon AS "Icon",is_published AS "IsPublished" FROM discovery.discovery_entries e JOIN catalog.cities c ON c.id=e.city_id JOIN catalog.categories k ON k.id=e.category_id) x));"""))

def migrated_snapshot(container, executable_only=False):
    result={}
    for schema,table in ([] if executable_only else [('catalog','cities'),('catalog','categories'),('catalog','category_translations'),('catalog','city_domains'),('catalog','districts'),('catalog','locations'),('discovery','discovery_entries')]):
        result[schema+'.'+table]=json.loads(execute(container,f"SELECT coalesce(json_agg(t ORDER BY row_to_json(t)::text),'[]') FROM {schema}.{table} t"))
    if not executable_only:result['constraints']=json.loads(execute(container,"SELECT json_agg(t ORDER BY tab,name) FROM (SELECT c.relname tab,con.conname name,pg_get_constraintdef(con.oid) definition FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery')) t"))
    if not executable_only:result['indexes']=json.loads(execute(container,"SELECT json_agg(t ORDER BY schemaname,tablename,indexname) FROM (SELECT schemaname,tablename,indexname,indexdef FROM pg_indexes WHERE schemaname IN ('catalog','discovery')) t"))
    queries={
        'columns': "SELECT json_agg(t ORDER BY table_schema,table_name,ordinal_position) FROM (SELECT table_schema,table_name,column_name,ordinal_position,data_type,udt_name,is_nullable,column_default,character_maximum_length,numeric_precision,numeric_scale FROM information_schema.columns WHERE table_schema IN ('catalog','discovery','public')) t",
        'functions': "SELECT json_agg(t ORDER BY schema,name,args) FROM (SELECT n.nspname schema,p.proname name,pg_get_function_identity_arguments(p.oid) args,pg_get_functiondef(p.oid) definition,pg_get_userbyid(p.proowner) owner,p.prosecdef security_definer,p.provolatile volatility,p.proconfig config FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname IN ('catalog','discovery')) t",
        'triggers': "SELECT json_agg(t ORDER BY schema,tab,name) FROM (SELECT n.nspname schema,c.relname tab,t.tgname name,pg_get_triggerdef(t.oid) definition,t.tgenabled enabled FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery') AND NOT t.tgisinternal) t",
        'ownership': "SELECT json_agg(t ORDER BY schema,name) FROM (SELECT n.nspname schema,c.relname name,c.relkind kind,pg_get_userbyid(c.relowner) owner FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery','public') AND c.relkind IN ('r','i','S')) t",
        'schemaOwnership': "SELECT json_agg(t ORDER BY schema) FROM (SELECT nspname schema,pg_get_userbyid(nspowner) owner FROM pg_namespace WHERE nspname IN ('catalog','discovery','public')) t",
        'history': 'SELECT json_agg(t ORDER BY "MigrationId") FROM public."__EFMigrationsHistory" t'
    }
    for key,sql in queries.items():
        if not executable_only or key in ['functions','triggers']:result[key]=json.loads(execute(container,sql))
    return result


def security_snapshot(container, principals=None):
    # PostgreSQL 18 pinned by compose: all eight table privileges, including MAINTAIN.
    if int(execute(container,"SHOW server_version_num")) // 10000 != 18:
        raise RuntimeError('Re-review privilege inventory for this PostgreSQL major version')
    names=principals or ['happy_owner','happy_schema_migrator','happy_app','happy_backup','happy_migrator']
    if any(not re.fullmatch(r'happy_[a-z_]+',name) for name in names):raise RuntimeError('Invalid verification principal')
    roles=','.join("'"+name+"'" for name in names)
    result={}
    queries={
        'roles': f"SELECT json_agg(t ORDER BY rolname) FROM (SELECT rolname,rolsuper,rolinherit,rolcreaterole,rolcreatedb,rolcanlogin,rolreplication,rolbypassrls,rolconnlimit,rolvaliduntil FROM pg_roles WHERE rolname IN ({roles})) t",
        'membership': f"SELECT coalesce(json_agg(t ORDER BY member,role,grantor),'[]') FROM (SELECT a.rolname member,b.rolname role,pg_get_userbyid(m.grantor) grantor,m.admin_option,m.inherit_option,m.set_option FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.member JOIN pg_roles b ON b.oid=m.roleid WHERE a.rolname IN ({roles})) t",
        'effectiveTables': f"SELECT json_agg(t ORDER BY role,schema,tab,privilege) FROM (SELECT r.rolname role,n.nspname schema,c.relname tab,p privilege,has_table_privilege(r.oid,c.oid,p) allowed,has_table_privilege(r.oid,c.oid,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE','DELETE','TRUNCATE','REFERENCES','TRIGGER','MAINTAIN']) p WHERE r.rolname IN ({roles}) AND n.nspname IN ('catalog','discovery','public') AND c.relkind='r') t",
        'effectiveColumns': f"SELECT json_agg(t ORDER BY role,schema,tab,col,privilege) FROM (SELECT r.rolname role,n.nspname schema,c.relname tab,a.attname col,p privilege,has_column_privilege(r.oid,c.oid,a.attnum,p) allowed,has_column_privilege(r.oid,c.oid,a.attnum,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace JOIN pg_attribute a ON a.attrelid=c.oid CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE','REFERENCES']) p WHERE r.rolname IN ({roles}) AND n.nspname IN ('catalog','discovery','public') AND c.relkind='r' AND a.attnum>0 AND NOT a.attisdropped) t",
        'effectiveSchema': f"SELECT json_agg(t ORDER BY role,schema,privilege) FROM (SELECT r.rolname role,n.nspname schema,p privilege,has_schema_privilege(r.oid,n.oid,p) allowed,has_schema_privilege(r.oid,n.oid,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_namespace n CROSS JOIN unnest(ARRAY['USAGE','CREATE']) p WHERE r.rolname IN ({roles}) AND n.nspname IN ('catalog','discovery','public')) t",
        'effectiveFunctions': f"SELECT json_agg(t ORDER BY role,schema,name,args) FROM (SELECT r.rolname role,n.nspname schema,p.proname name,pg_get_function_identity_arguments(p.oid) args,has_function_privilege(r.oid,p.oid,'EXECUTE') allowed,has_function_privilege(r.oid,p.oid,'EXECUTE WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE r.rolname IN ({roles}) AND n.nspname IN ('catalog','discovery')) t",
        'effectiveDatabase': f"SELECT json_agg(t ORDER BY role,privilege) FROM (SELECT rolname role,p privilege,has_database_privilege(oid,'happyalmere',p) allowed,has_database_privilege(oid,'happyalmere',p||' WITH GRANT OPTION') grantable FROM pg_roles CROSS JOIN unnest(ARRAY['CONNECT','CREATE','TEMP']) p WHERE rolname IN ({roles})) t",
        'effectiveSequences': f"SELECT coalesce(json_agg(t ORDER BY role,schema,name,privilege),'[]') FROM (SELECT r.rolname role,n.nspname schema,c.relname name,p privilege,has_sequence_privilege(r.oid,c.oid,p) allowed,has_sequence_privilege(r.oid,c.oid,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN unnest(ARRAY['USAGE','SELECT','UPDATE']) p WHERE r.rolname IN ({roles}) AND n.nspname IN ('catalog','discovery','public') AND c.relkind='S') t",
        'defaultAcl': "SELECT coalesce(json_agg(t ORDER BY role,schema,type),'[]') FROM (SELECT pg_get_userbyid(d.defaclrole) role,n.nspname schema,d.defaclobjtype type,d.defaclacl::text grants FROM pg_default_acl d LEFT JOIN pg_namespace n ON n.oid=d.defaclnamespace WHERE pg_get_userbyid(d.defaclrole) IN ('happy_owner','happy_migrator')) t",
        'objectAcl': "SELECT coalesce(json_agg(t ORDER BY kind,schema,name,col,grantor,grantee,privilege_type,is_grantable),'[]') FROM (SELECT o.kind,o.schema,o.name,o.col,pg_get_userbyid(x.grantor) grantor,CASE WHEN x.grantee=0 THEN 'PUBLIC' ELSE pg_get_userbyid(x.grantee) END grantee,x.privilege_type,x.is_grantable FROM (SELECT 'relation' kind,n.nspname schema,c.relname name,'' col,c.relacl acl FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery','public') UNION ALL SELECT 'column',n.nspname,c.relname,a.attname,a.attacl FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery','public') AND a.attnum>0 AND NOT a.attisdropped UNION ALL SELECT 'function',n.nspname,p.proname||'('||pg_get_function_identity_arguments(p.oid)||')','',p.proacl FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname IN ('catalog','discovery') UNION ALL SELECT 'schema',nspname,nspname,'',nspacl FROM pg_namespace WHERE nspname IN ('catalog','discovery','public') UNION ALL SELECT 'database','','happyalmere','',datacl FROM pg_database WHERE datname='happyalmere') o CROSS JOIN LATERAL aclexplode(o.acl) x WHERE x.grantee=0 OR pg_get_userbyid(x.grantee)<>'happy_restore') t"
    }
    for key,sql in queries.items():result[key]=json.loads(execute(container,sql))
    return result


def complete_state(container):
    return {'dataMetadataHistory':migrated_snapshot(container),'security':security_snapshot(container)}


def executable_metadata(container):
    snapshot=migrated_snapshot(container,executable_only=True)
    result={key:snapshot[key] for key in ['functions','triggers']}
    # Canonicalize formatting only; preserve quoted SQL strings verbatim.
    for row in result['functions']:
        row['definition']=re.sub(r"'(?:''|[^'])*'|\s+",lambda m:m[0] if m[0].startswith("'") else ' ',row['definition']).strip()
    return result


def assert_sql_contract(reference, actual):
    if reference!=actual:raise RuntimeError('Independent SQL-only definition contract mismatch')


def verify_revoked_restore(container):
    execute(container,'ALTER ROLE happy_restore NOLOGIN; REVOKE happy_owner FROM happy_restore; REVOKE CONNECT ON DATABASE happyalmere FROM happy_restore;')
    state=security_snapshot(container,['happy_restore'])
    if any(state['roles'][0][flag] for flag in ['rolcanlogin','rolsuper','rolcreaterole','rolcreatedb','rolreplication','rolbypassrls']) or state['membership']:raise RuntimeError('Restore role authority remains')
    if any(row['grantable'] or (row['allowed'] and (row['schema'],row['privilege'])!=('public','USAGE')) for row in state['effectiveSchema']):raise RuntimeError('Restore schema authority remains')
    for kind in ['effectiveTables','effectiveColumns','effectiveFunctions','effectiveDatabase','effectiveSequences']:
        if any(row['allowed'] or row['grantable'] for row in state[kind]):raise RuntimeError('Restore resource authorization remains: '+kind)
    if execute(container,"SELECT count(*) FROM pg_stat_activity WHERE usename='happy_restore'")!='0':raise RuntimeError('Restore sessions remain')
    # Model a surviving identity without granting LOGIN or owner membership again.
    for statement in ['SET ROLE happy_owner','SELECT * FROM catalog.cities','SELECT * FROM public."__EFMigrationsHistory"']:
        execute(container,'SET SESSION AUTHORIZATION happy_restore; '+statement,expected_state='42501')
    return state


def relational_metadata(container):
    queries={
      'columns': "SELECT json_agg(t ORDER BY table_schema,table_name,column_name) FROM (SELECT table_schema,table_name,column_name,data_type,udt_name,is_nullable,column_default,character_maximum_length,numeric_precision,numeric_scale FROM information_schema.columns WHERE table_schema IN ('catalog','discovery')) t",
      'constraints': "SELECT json_agg(t ORDER BY schema,tab,name) FROM (SELECT n.nspname schema,c.relname tab,con.conname name,pg_get_constraintdef(con.oid) definition FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery') AND con.contype<>'n') t",
      'indexes': "SELECT json_agg(t ORDER BY schemaname,tablename,indexname) FROM (SELECT schemaname,tablename,indexname,indexdef FROM pg_indexes WHERE schemaname IN ('catalog','discovery')) t"
    }
    return {key:json.loads(execute(container,sql)) for key,sql in queries.items()}


def compare_model(reference, actual):
    # Explicit SQL-only compatibility/index inventory, not a blanket ignore.
    allowed_columns={('discovery','discovery_entries','legacy_city_id'),('discovery','discovery_entries','legacy_category_id')}
    allowed_indexes={'Cities_uuid','Categories_uuid','DiscoveryEntries_uuid','entries_page_order',
      'IX_DiscoveryEntries_CategoryId','IX_DiscoveryEntries_CityId_Slug','IX_DiscoveryEntries_CityId_IsPublished_CategoryId_Id'}
    extras={}
    keys={'columns':('table_schema','table_name','column_name'),'constraints':('schema','tab','name'),'indexes':('schemaname','tablename','indexname')}
    for kind,fields in keys.items():
        expected={tuple(row[k] for k in fields):row for row in reference[kind]}
        found={tuple(row[k] for k in fields):row for row in actual[kind]}
        for key,row in expected.items():
            if found.get(key)!=row:raise RuntimeError('EF physical definition mismatch: '+kind+' '+str(key))
        extra=set(found)-set(expected)
        if kind=='columns' and extra!=allowed_columns:raise RuntimeError('Unexpected SQL-only columns')
        if kind=='constraints' and extra:raise RuntimeError('Unexpected SQL-only constraint')
        if kind=='indexes' and {key[-1] for key in extra}!=allowed_indexes:raise RuntimeError('Unexpected SQL-only index inventory')
        extras[kind]=[found[key] for key in sorted(extra)]
    return extras


def verify_role_controls(container, env):
    for user,password in [('happy_app',env['HAPPY_APP_PASSWORD']),('happy_backup',env['BACKUP_PASSWORD'])]:
        if execute(container,'SELECT current_user',user,password)!=user:raise RuntimeError('Identity positive control failed')
        if execute(container,'SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()',user,password)!='t':raise RuntimeError('Restored TLS missing')
        execute(container,'SELECT count(*) FROM discovery.discovery_entries',user,password)
        for statement in ['DELETE FROM discovery.discovery_entries WHERE false','CREATE TABLE public.denied(id int)','SET ROLE happy_owner','SET ROLE happy_schema_migrator','SET ROLE happy_migrator','CREATE ROLE denied']:
            execute(container,statement,user,password,expected_state='42501')
    execute(container,'BEGIN; SET ROLE happy_owner; CREATE TABLE catalog.private_probe(id int); COMMIT;')
    try:
        for user,password in [('happy_app',env['HAPPY_APP_PASSWORD']),('happy_backup',env['BACKUP_PASSWORD'])]:
            execute(container,'SELECT * FROM catalog.private_probe',user,password,expected_state='42501')
    finally:execute(container,'DROP TABLE catalog.private_probe')
    if execute(container,'SELECT current_user','happy_schema_migrator',env['MIGRATION_PASSWORD'])!='happy_schema_migrator':raise RuntimeError('Migration identity positive control failed')
    # Syntax/connection failures must never masquerade as permission denials.
    for statement,password in [('SELEC broken',env['HAPPY_APP_PASSWORD']),('SELECT 1','incorrect-synthetic-password')]:
        try:execute(container,statement,'happy_app',password,expected_state='42501')
        except RuntimeError:pass
        else:raise RuntimeError('Negative verifier accepted an unrelated failure')
    try:execute('ha002r01-deliberately-nonexistent','SELECT 1',expected_state='42501')
    except RuntimeError:pass
    else:raise RuntimeError('Negative verifier accepted unavailable database')
    for sql in ["BEGIN; UPDATE catalog.categories SET parent_id=id WHERE legacy_id=1; ROLLBACK;",
                "BEGIN; UPDATE catalog.categories SET parent_id=(SELECT id FROM catalog.categories WHERE legacy_id=2) WHERE legacy_id=1; UPDATE catalog.categories SET parent_id=(SELECT id FROM catalog.categories WHERE legacy_id=1) WHERE legacy_id=2; ROLLBACK;"]:
        execute(container,sql,'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner',expected_state='23514',expected_reason='Hierarchy cycle rejected' if 'legacy_id=2; ROLLBACK' in sql else None)

    execute(container,"BEGIN; INSERT INTO catalog.districts(id,city_id,slug,name,district_type,is_active) VALUES('10000000-0000-4000-8000-000000000010','00000001-0000-8000-8000-000000000001','restore-a','Synthetic','test',false),('10000000-0000-4000-8000-000000000011','00000001-0000-8000-8000-000000000001','restore-b','Synthetic','test',false); UPDATE catalog.districts SET parent_id=CASE WHEN slug='restore-a' THEN '10000000-0000-4000-8000-000000000011'::uuid ELSE '10000000-0000-4000-8000-000000000010'::uuid END WHERE slug IN ('restore-a','restore-b'); ROLLBACK;",'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner',expected_state='23514',expected_reason='Hierarchy cycle rejected')
    execute(container,"BEGIN; UPDATE catalog.cities SET country_code='ZZ'; ROLLBACK;",'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner',expected_state='23514')


def connection(directory,port,user,password):
    return f'Host=localhost;Port={port};Database=happyalmere;SSL Mode=VerifyFull;Root Certificate={(directory/"postgres.crt").as_posix()};Username={user};Password={password}'

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--gate',required=True);parser.add_argument('--port',type=int,default=55435);args=parser.parse_args()
    os.chdir(ROOT)
    endpoint=json.loads(r.docker('context','inspect'))[0]['Endpoints']['docker']['Host']
    if not endpoint.startswith(('npipe://','unix://')):raise RuntimeError('Local Docker only')
    networks=[json.loads(line) for line in r.docker('network','ls','--format','json').splitlines()]
    selected=None
    for network in networks:
        name=network['Name']
        if name.startswith('ha002f-'):
            details=json.loads(r.docker('network','inspect',name))[0]
            labels=details.get('Labels') or {}
            if labels.get('com.docker.compose.project','').startswith('ha002f-') or labels.get('com.happycity.task')=='ha002a-forward':
                selected=name;break
    if selected is None:
        selected='ha002f-shared-'+secrets.token_hex(3)
        r.docker('network','create','--label','com.happycity.task=ha002a-forward',selected)
    os.environ['HA_FORWARD_NETWORK']=selected

    original='HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/Migrations/'
    for name in ['20261119000108_InitialDiscovery.cs','20261119000108_InitialDiscovery.Designer.cs','20261119161305_CatalogForward.cs','20261119161305_CatalogForward.Designer.cs','CatalogForward.sql','20261119175842_SupportedRegions.cs','20261119175842_SupportedRegions.Designer.cs']:
        expected=r.run('git','show','b4f092ffbbf385f16c4f42aaef117fafc8e56da7:'+original+name)
        if (ROOT/original/name).read_text(encoding='utf-8-sig').replace('\r\n','\n') != expected.lstrip('\ufeff').replace('\r\n','\n'):raise RuntimeError('Original migration changed')
    gate_path=(ROOT/args.gate).resolve(); gate=json.loads(gate_path.read_text())
    if not gate['gate'].startswith('PASS:') or gate_path.parent.parent != (ROOT/'artifacts/ha002a').resolve(): raise RuntimeError('Validated local recovery gate required')
    archive=(gate_path.parent/'baseline.dump').read_bytes()
    if hashlib.sha256(archive).hexdigest()!=gate['backupSha256']: raise RuntimeError('Archive integrity failed')
    directory=ROOT/'artifacts/forward'/ (dt.datetime.now(dt.timezone.utc).strftime('%Y%m%d%H%M%S')+'-'+secrets.token_hex(3))
    directory.mkdir(parents=True);r.protect(directory)
    evidence={'baselineCommit':r.run('git','rev-parse','HEAD').strip(),'startedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'sourceGate':str(gate_path.relative_to(ROOT)),'checks':{}}
    targets=[]
    try:
        target,env,info=new_target(directory,'upgrade',args.port,archive);targets.append(target)
        # Only isolated clone receives synthetic unpublished and integer boundary fixtures.
        execute(target,"""INSERT INTO public."DiscoveryEntries"("Id","CityId","CategoryId","Slug","Title","Description","District","Icon","IsPublished") OVERRIDING SYSTEM VALUE
VALUES (0,1,1,'zero-fixture','Synthetic zero','Migration boundary','Exact legacy text','*',false),
(2147483647,1,1,'max-fixture','Synthetic maximum','Migration boundary',' Exact legacy text ','*',false);""")
        before=r.inventory(target)
        enriched=r.docker('exec',target,'pg_dump','-U','happy_migrator','-d','happyalmere','-Fc',binary=True)
        r.private_write(directory/'enriched.dump',enriched)
        r.private_write(directory/'baseline-private.json',json.dumps(before,ensure_ascii=False))
        restore,restore_env,restore_info=new_target(directory,'restore',args.port+1,enriched);targets.append(restore)
        if r.inventory(restore)!=before: raise RuntimeError('Exact enriched restore mismatch')
        evidence['checks']['enrichedBackupRestore']='PASS';evidence['enrichedCounts']={k:len(v) for k,v in before['rows'].items()}
        evidence['enrichedSha256']=hashlib.sha256(enriched).hexdigest()
        roles(target,env)
        r.private_write(directory/'postgres.crt',r.docker('exec',target,'cat','/var/lib/postgresql-tls/server.crt'))
        migration=connection(directory,args.port,'happy_schema_migrator',env['MIGRATION_PASSWORD'])+';Options=-c role=happy_owner'
        app=connection(directory,args.port,'happy_app',env['HAPPY_APP_PASSWORD'])
        process=dict(os.environ,HAPPYCITY_MIGRATION_CONNECTION=migration)
        project='HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure'
        for label,start in [('forward','20261119000108_InitialDiscovery'),('clean','0')]:
            r.run('dotnet','ef','migrations','script',start,'--project',project,'--context','HappyCityDbContext','--configuration','Release','--no-build','--output',str(directory/(label+'.sql')),env=process)
        forward=(directory/'forward.sql').read_text(encoding='utf-8-sig')
        before_failure=r.inventory(target)
        execute(target,forward,'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner -c happycity.fail_after_backfill=on',expected_state='P0001',expected_reason='Controlled migration failure after backfill')
        if r.inventory(target)!=before_failure: raise RuntimeError('Failed migration did not roll back all content/schema/history')
        evidence['checks']['afterBackfillRollback']='PASS: P0001 Controlled migration failure after backfill; full rollback verified'
        # Independent fresh-instance restore after the controlled failure.
        start=time.monotonic(); recovered,renv,rinfo=new_target(directory,'failure-recovery',args.port+2,enriched);targets.append(recovered)
        if r.inventory(recovered)!=before: raise RuntimeError('Post-failure recovery mismatch')
        evidence['recoverySeconds']=round(time.monotonic()-start,3);evidence['checks']['postFailureFreshRestore']='PASS'
        # Rehearse R01 against the already-applied CatalogForward checkpoint,
        # with a protected exact migrated backup/restore gate before correction.
        r.run('dotnet','ef','migrations','script','20261119000108_InitialDiscovery','20261119161305_CatalogForward','--project',project,'--context','HappyCityDbContext','--configuration','Release','--no-build','--output',str(directory/'prior-forward.sql'),env=process)
        r.run('dotnet','ef','migrations','script','20261119161305_CatalogForward','20261119175842_SupportedRegions','--project',project,'--context','HappyCityDbContext','--configuration','Release','--no-build','--output',str(directory/'correction.sql'),env=process)
        r.run('dotnet','ef','migrations','script','20261119175842_SupportedRegions','--project',project,'--context','HappyCityDbContext','--configuration','Release','--no-build','--output',str(directory/'fts.sql'),env=process)
        execute(target,(directory/'prior-forward.sql').read_text(encoding='utf-8-sig'),'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(target)
        prior_snapshot=migrated_snapshot(target)
        prior_archive=r.docker('exec',target,'pg_dump','-U','happy_migrator','-d','happyalmere','-Fc',binary=True)
        r.private_write(directory/'pre-r01.dump',prior_archive)
        prior_restore,prior_env,prior_info=new_target(directory,'pre-r01-restore',args.port+6);targets.append(prior_restore);roles(prior_restore,prior_env)
        r.docker('exec','-i',prior_restore,'pg_restore','-U','happy_migrator','--role=happy_owner','-d','happyalmere','--no-owner','--no-acl','--single-transaction','--exit-on-error',input=prior_archive,binary=True)
        if migrated_snapshot(prior_restore)!=prior_snapshot:raise RuntimeError('Pre-correction migrated backup/restore mismatch')
        evidence['checks']['preCorrectionMigratedBackupRestore']='PASS'
        evidence['preCorrectionBackupSha256']=hashlib.sha256(prior_archive).hexdigest()
        grants(prior_restore)
        pre_correction=complete_state(target)
        if complete_state(prior_restore)!=pre_correction:raise RuntimeError('Pre-correction restored complete state mismatch')
        correction=(directory/'correction.sql').read_text(encoding='utf-8-sig')
        if correction.count('COMMIT;')!=1 or not correction.lstrip().startswith('START TRANSACTION;'):raise RuntimeError('Unexpected correction transaction envelope')
        fault="DO $$ BEGIN IF NOT EXISTS(SELECT 1 FROM pg_constraint WHERE conname='cities_supported_region') OR NOT EXISTS(SELECT 1 FROM public.\"__EFMigrationsHistory\" WHERE \"MigrationId\"='20261119175842_SupportedRegions') THEN RAISE EXCEPTION 'Correction probe prerequisite missing'; END IF; RAISE EXCEPTION 'Controlled correction failure after constraints and history' USING ERRCODE='P0001'; END $$;"
        execute(target,correction.replace('COMMIT;',fault+'\nCOMMIT;'),'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner',expected_state='P0001',expected_reason='Controlled correction failure after constraints and history')
        if complete_state(target)!=pre_correction:raise RuntimeError('Correction rollback changed complete state')
        # Fresh target created AFTER the correction-specific failure from protected pre-correction archive.
        correction_recovery,crenv,crinfo=new_target(directory,'correction-recovery',args.port+7);targets.append(correction_recovery);roles(correction_recovery,crenv)
        r.docker('exec','-i',correction_recovery,'pg_restore','-U','happy_migrator','--role=happy_owner','-d','happyalmere','--no-owner','--no-acl','--single-transaction','--exit-on-error',input=prior_archive,binary=True)
        grants(correction_recovery)
        if complete_state(correction_recovery)!=pre_correction:raise RuntimeError('Correction recovery complete state mismatch')
        execute(target,correction,'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(target)
        execute(correction_recovery,correction,'happy_schema_migrator',crenv['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(correction_recovery)
        if complete_state(correction_recovery)!=complete_state(target):raise RuntimeError('Correction recovery reapplication mismatch')
        evidence['checks']['correctionSpecificFailureRollbackRecovery']='PASS: P0001 after constraints/history; complete state rollback, fresh restore and reapplication'
        evidence['correctionPreStateDigest']=r.digest(pre_correction)
        pre_fts=complete_state(target)
        fts=(directory/'fts.sql').read_text(encoding='utf-8-sig')
        if fts.count('COMMIT;')!=1 or not fts.lstrip().startswith('START TRANSACTION;'):raise RuntimeError('Unexpected FTS transaction envelope')
        fts_fault="DO $$ BEGIN IF NOT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='discovery' AND table_name='discovery_entries' AND column_name='search_vector' AND udt_name='tsvector') OR NOT EXISTS(SELECT 1 FROM pg_indexes WHERE schemaname='discovery' AND tablename='discovery_entries' AND indexname='ix_discovery_entries_search_vector') OR NOT EXISTS(SELECT 1 FROM public.\"__EFMigrationsHistory\" WHERE \"MigrationId\"='20261123113902_DutchFullTextSearch') THEN RAISE EXCEPTION 'FTS probe prerequisite missing'; END IF; RAISE EXCEPTION 'Controlled FTS failure after search_vector, GIN index and history' USING ERRCODE='P0001'; END $$;"
        execute(target,fts.replace('COMMIT;',fts_fault+'\nCOMMIT;'),'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner',expected_state='P0001',expected_reason='Controlled FTS failure after search_vector, GIN index and history')
        if complete_state(target)!=pre_fts:raise RuntimeError('FTS rollback changed complete state')
        execute(target,fts,'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(target)
        execute(correction_recovery,fts,'happy_schema_migrator',crenv['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(correction_recovery)
        if complete_state(correction_recovery)!=complete_state(target):raise RuntimeError('FTS recovery reapplication mismatch')
        evidence['checks']['dutchFullTextSearchFailureRollbackReapplication']='PASS: P0001 after search_vector/GIN/history; complete state rollback and reapplication'


        after=canonical(target)
        for name in after:
            if sorted(after[name],key=lambda x:x['Id'])!=sorted(before['rows'][name],key=lambda x:x['Id']): raise RuntimeError('Full content reconciliation failed: '+name)
        evidence['checks']['completeRowsAssociationsPublication']='PASS'
        for table,tag in [('catalog.cities',1),('catalog.categories',2),('discovery.discovery_entries',3)]:
            mismatch=execute(target,f"SELECT count(*) FROM {table} WHERE legacy_id IS NOT NULL AND id <> ('{tag:08x}-0000-8000-8000-'||lpad(to_hex(legacy_id),12,'0'))::uuid")
            if mismatch!='0':raise RuntimeError('UUID mapping differs from approved injective encoding')
        evidence['checks']['allUuidMappingsAndBoundaryIds']='PASS'

        evidence['rowDigests']={k:r.digest(sorted(v,key=lambda x:x['Id'])) for k,v in after.items()}
        # Immutable first migration history plus reviewed forward migrations.
        history=json.loads(execute(target,'SELECT json_agg(t ORDER BY "MigrationId") FROM public."__EFMigrationsHistory" t;'))
        if len(history)!=4 or history[0]!=before['rows']['__EFMigrationsHistory'][0] or history[-1]['MigrationId']!='20261123113902_DutchFullTextSearch':raise RuntimeError('Migration history changed')
        evidence['migrationHistory']=history
        no_op_before=complete_state(target)
        r.run('dotnet','ef','database','update','--project',project,'--context','HappyCityDbContext','--configuration','Release','--no-build',env=process)
        if complete_state(target)!=no_op_before: raise RuntimeError('Repeat migration changed complete state')
        evidence['noOpStateDigest']=r.digest(no_op_before)
        evidence['checks']['secondRunNoOp']='PASS'
        clean,cenv,cinfo=new_target(directory,'clean',args.port+3);targets.append(clean);roles(clean,cenv)
        execute(clean,(directory/'clean.sql').read_text(encoding='utf-8-sig'),'happy_schema_migrator',cenv['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(clean)
        if len(canonical(clean)['DiscoveryEntries'])!=6:raise RuntimeError('Clean path fixture mismatch')
        evidence['checks']['cleanCreation']='PASS'
        reference,ref_env,ref_info=new_target(directory,'ef-model',args.port+5);targets.append(reference);roles(reference,ref_env)
        r.run('dotnet','ef','dbcontext','script','--project',project,'--context','HappyCityDbContext','--configuration','Release','--no-build','--output',str(directory/'ef-model.sql'),env=process)
        execute(reference,(directory/'ef-model.sql').read_text(encoding='utf-8-sig'),'happy_schema_migrator',ref_env['MIGRATION_PASSWORD'],'-c role=happy_owner')
        expected_model=relational_metadata(reference)
        extras=compare_model(expected_model,relational_metadata(target));compare_model(expected_model,relational_metadata(clean))
        contract=(ROOT/'scripts/verify-catalog-contract.sql').read_text()
        execute(reference,contract,'happy_schema_migrator',ref_env['MIGRATION_PASSWORD'],'-c role=happy_owner')
        expected_contract={'relational':relational_metadata(reference),'executable':executable_metadata(reference)}
        for instance in [target,clean,correction_recovery]:
            assert_sql_contract(expected_contract,{'relational':relational_metadata(instance),'executable':executable_metadata(instance)})
        # Negative controls: correct names must not hide wrong definitions or identity.
        for kind,field in [('columns','is_nullable'),('indexes','indexdef'),('indexes','schemaname'),('constraints','definition')]:
            changed=copy.deepcopy(expected_contract);changed['relational'][kind][0][field]='deliberate mismatch'
            try:assert_sql_contract(expected_contract,changed)
            except RuntimeError:pass
            else:raise RuntimeError('SQL contract negative control failed')
        for kind,field in [('functions','definition'),('triggers','definition')]:
            changed=copy.deepcopy(expected_contract);changed['executable'][kind][0][field]='deliberate mismatch'
            try:assert_sql_contract(expected_contract,changed)
            except RuntimeError:pass
            else:raise RuntimeError('Executable contract negative control failed')
        evidence['sqlOnlyContractSha256']=hashlib.sha256(contract.encode()).hexdigest()
        evidence['checks']['independentSqlOnlyDefinitionsAndMutationGuards']='PASS'
        evidence['sqlOnlyInventory']=extras
        evidence['checks']['efPhysicalDefinitionsCleanAndUpgrade']='PASS'
        verify_role_controls(target,env)
        evidence['checks']['exactSqlstateAndUnrelatedErrorRejection']='PASS'
        evidence['checks']['futurePrivateTableDenied']='PASS'

        negatives=['CREATE TABLE public.denied(id int)','DELETE FROM discovery.discovery_entries WHERE false','SET ROLE happy_owner','SET ROLE happy_schema_migrator','SET ROLE happy_migrator','CREATE ROLE denied','GRANT happy_owner TO happy_app']
        for user,password in [('happy_app',env['HAPPY_APP_PASSWORD']),('happy_backup',env['BACKUP_PASSWORD'])]:
            if execute(target,'SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()',user,password)!='t':raise RuntimeError('TLS missing')
            if execute(target,'SELECT count(*) FROM discovery.discovery_entries',user,password)!='8':raise RuntimeError('Role read failed')
            for statement in negatives:execute(target,statement,user,password,expected_state='42501')
        for statement in ['CREATE ROLE denied','ALTER ROLE happy_schema_migrator SUPERUSER','SET ROLE happy_migrator']:
            execute(target,statement,'happy_schema_migrator',env['MIGRATION_PASSWORD'],expected_state='42501')
        evidence['checks']['tlsRoleReadsAndEscalationNegatives']='PASS'
        # Real restricted logical backup over verified TLS, with no role escalation.
        backup_env=dict(os.environ,PGPASSWORD=env['BACKUP_PASSWORD'])
        restricted=r.run('docker','exec','-e','PGPASSWORD',target,'pg_dump','--dbname=host=localhost dbname=happyalmere user=happy_backup sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt','-Fc',env=backup_env,binary=True)
        r.private_write(directory/'restricted-backup.dump',restricted);evidence['checks']['restrictedLogicalBackup']='PASS'
        final_restore,fenv,finfo=new_target(directory,'restricted-restore',args.port+4);targets.append(final_restore);roles(final_restore,fenv)
        restore_password=secrets.token_hex(32)
        execute(final_restore,f"CREATE ROLE happy_restore LOGIN PASSWORD '{restore_password}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS; GRANT happy_owner TO happy_restore; GRANT CONNECT ON DATABASE happyalmere TO happy_restore;")
        restore_process=dict(os.environ,PGPASSWORD=restore_password)
        r.run('docker','exec','-i','-e','PGPASSWORD','-e','PGOPTIONS=-c role=happy_owner',final_restore,'pg_restore','--dbname=host=localhost dbname=happyalmere user=happy_restore sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt','--no-owner','--no-acl','--single-transaction','--exit-on-error',input=restricted,env=restore_process,binary=True)
        if migrated_snapshot(final_restore)!=migrated_snapshot(target):raise RuntimeError('Restricted restore rows/schema/index mismatch')
        grants(final_restore)
        verify_role_controls(final_restore,fenv)
        compare_model(expected_model,relational_metadata(final_restore))
        assert_sql_contract(expected_contract,{'relational':relational_metadata(final_restore),'executable':executable_metadata(final_restore)})
        if security_snapshot(final_restore)!=security_snapshot(target):raise RuntimeError('Restored effective privileges/memberships mismatch')
        if execute(final_restore,'SELECT current_user','happy_restore',restore_password)!='happy_restore':raise RuntimeError('Restore identity positive control failed')
        for statement in ['CREATE ROLE denied','SET ROLE happy_migrator','ALTER ROLE happy_restore SUPERUSER']:
            execute(final_restore,statement,'happy_restore',restore_password,expected_state='42501')
        revoked=verify_revoked_restore(final_restore)
        evidence['revokedRestoreAuthorization']=revoked
        if security_snapshot(final_restore)!=security_snapshot(target):raise RuntimeError('Final post-revocation common privilege mismatch')
        evidence['checks']['completePrivilegesGrantOptionsAndRevocation']='PASS (authentication denial also required by PostgresTests)'
        evidence['checks']['dedicatedRestoreIdentityAndRevocation']='PASS'
        evidence['checks']['migratedArchiveAllTablesConstraintsIndexes']='PASS'
        evidence['checks']['restoredColumnsFunctionsTriggersHistoryOwnershipEffectivePrivileges']='PASS'
        evidence['checks']['restoredFunctionalNegatives']='PASS'
        ownership=execute(target,"SELECT count(*) FROM pg_tables WHERE schemaname IN ('catalog','discovery') AND tableowner <> 'happy_owner'")
        if ownership!='0':raise RuntimeError('Object ownership separation failed')
        evidence['checks']['nonLoginObjectOwnership']='PASS'
        scope=json.loads(execute(target,"SELECT json_build_object('sequences',(SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery','public') AND c.relkind='S'),'largeObjects',(SELECT count(*) FROM pg_largeobject_metadata),'foreignServers',(SELECT count(*) FROM pg_foreign_server),'foreignTables',(SELECT count(*) FROM pg_foreign_table),'rowPolicies',(SELECT count(*) FROM pg_policy),'customDomainsEnums',(SELECT count(*) FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE n.nspname IN ('catalog','discovery','public') AND t.typtype IN ('d','e')),'roleSettings',(SELECT count(*) FROM pg_db_role_setting))"))
        if any(scope.values()):raise RuntimeError('Re-review declared privilege/object exclusions')
        evidence['inapplicableObjectCounts']=scope


        # Use the clean isolated instance for existing six-record regression tests.
        clean_dir=directory/'clean';clean_dir.mkdir();r.private_write(clean_dir/'postgres.crt',r.docker('exec',clean,'cat','/var/lib/postgresql-tls/server.crt'))
        restored_dir=directory/'restored';restored_dir.mkdir();r.private_write(restored_dir/'postgres.crt',r.docker('exec',final_restore,'cat','/var/lib/postgresql-tls/server.crt'))
        r.private_write(directory/'rehearsal.env','ConnectionStrings__HappyCity='+connection(clean_dir,args.port+3,'happy_app',cenv['HAPPY_APP_PASSWORD'])+'\nHAPPYCITY_MIGRATION_CONNECTION='+connection(clean_dir,args.port+3,'happy_schema_migrator',cenv['MIGRATION_PASSWORD'])+';Options=-c role=happy_owner\nHAPPYCITY_REVOKED_RESTORE_CONNECTION='+connection(restored_dir,args.port+4,'happy_restore',restore_password)+'\nHAPPYCITY_RESTORE_CONTROL_CONNECTION='+connection(restored_dir,args.port+4,'happy_app',fenv['HAPPY_APP_PASSWORD'])+'\n')
        evidence['targetContainers']=targets;evidence['regressionContainer']=clean;evidence['completedUtc']=dt.datetime.now(dt.timezone.utc).isoformat()
        evidence['status']='PASS';r.private_write(directory/'evidence.json',json.dumps(evidence,indent=2)+'\n')
        print(json.dumps({'status':'PASS','evidence':str(directory.relative_to(ROOT)/'evidence.json'),'environment':str(directory.relative_to(ROOT)/'rehearsal.env'),'checks':evidence['checks']},indent=2))
    except Exception:
        for target in targets:r.docker('stop',target)
        raise

if __name__=='__main__':main()
