"""Isolated HA-002-A forward rehearsal. Requires a successful preserved-source recovery gate.
No source mutation. Private archives/credentials are never evidence artifacts.
"""
import argparse, datetime as dt, hashlib, importlib.util, json, os, secrets, subprocess, time
from pathlib import Path
spec=importlib.util.spec_from_file_location("recovery",Path(__file__).with_name("verify-database-recovery.py"))
r=importlib.util.module_from_spec(spec); spec.loader.exec_module(r)
ROOT=r.ROOT

def execute(container,sql,user="happy_migrator",password=None,options=None,expect_failure=False):
    env=dict(os.environ)
    args=['docker','exec','-i']
    if password:
        env['PGPASSWORD']=password; args += ['-e','PGPASSWORD']
    if options: args += ['-e','PGOPTIONS='+options]
    args += [container,'psql','-X','-qAt','-v','ON_ERROR_STOP=1','-U',user,'-d','happyalmere']
    if password: args += ['--dbname=host=localhost dbname=happyalmere sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt user='+user]
    result=subprocess.run(args,input=sql,text=True,encoding='utf-8',stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env)
    if expect_failure:
        if result.returncode==0: raise RuntimeError('Expected negative SQL unexpectedly succeeded')
        return 'rejected'
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
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA public GRANT SELECT ON TABLES TO happy_app,happy_backup;
""")

def grants(container):
    execute(container,"""GRANT USAGE ON SCHEMA catalog,discovery TO happy_app,happy_backup;
GRANT SELECT ON ALL TABLES IN SCHEMA catalog,discovery TO happy_app,happy_backup;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO happy_backup;
REVOKE ALL ON public."__EFMigrationsHistory" FROM happy_app;
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA catalog,discovery GRANT SELECT ON TABLES TO happy_app,happy_backup;
""")

def canonical(container):
    return json.loads(execute(container,"""SELECT json_build_object(
'Cities',(SELECT json_agg(x ORDER BY "Id") FROM (SELECT legacy_id AS "Id",name AS "Name",slug AS "Slug" FROM catalog.cities) x),
'Categories',(SELECT json_agg(x ORDER BY "Id") FROM (SELECT legacy_id AS "Id",name AS "Name",slug AS "Slug" FROM catalog.categories) x),
'DiscoveryEntries',(SELECT json_agg(x ORDER BY "Id") FROM (SELECT e.legacy_id AS "Id",c.legacy_id AS "CityId",k.legacy_id AS "CategoryId",e.slug AS "Slug",title AS "Title",description AS "Description",district AS "District",icon AS "Icon",is_published AS "IsPublished" FROM discovery.discovery_entries e JOIN catalog.cities c ON c.id=e.city_id JOIN catalog.categories k ON k.id=e.category_id) x));"""))

def migrated_snapshot(container):
    result={}
    for schema,table in [('catalog','cities'),('catalog','categories'),('catalog','category_translations'),('catalog','city_domains'),('catalog','districts'),('catalog','locations'),('discovery','discovery_entries')]:
        result[schema+'.'+table]=json.loads(execute(container,f"SELECT coalesce(json_agg(t ORDER BY row_to_json(t)::text),'[]') FROM {schema}.{table} t"))
    result['constraints']=json.loads(execute(container,"SELECT json_agg(t ORDER BY tab,name) FROM (SELECT c.relname tab,con.conname name,pg_get_constraintdef(con.oid) definition FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('catalog','discovery')) t"))
    result['indexes']=json.loads(execute(container,"SELECT json_agg(t ORDER BY schemaname,tablename,indexname) FROM (SELECT schemaname,tablename,indexname,indexdef FROM pg_indexes WHERE schemaname IN ('catalog','discovery')) t"))
    return result


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
    for name in ['20261119000108_InitialDiscovery.cs','20261119000108_InitialDiscovery.Designer.cs']:
        expected=r.run('git','show','3287994afb0bb3fe7e69f60a6bda689f77c40323:'+original+name)
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
            r.run('dotnet','ef','migrations','script',start,'--project',project,'--configuration','Release','--no-build','--output',str(directory/(label+'.sql')),env=process)
        forward=(directory/'forward.sql').read_text(encoding='utf-8-sig')
        before_failure=r.inventory(target)
        execute(target,forward,'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner -c happycity.fail_after_backfill=on',True)
        if r.inventory(target)!=before_failure: raise RuntimeError('Failed migration did not roll back all content/schema/history')
        evidence['checks']['afterBackfillRollback']='PASS'
        # Independent fresh-instance restore after the controlled failure.
        start=time.monotonic(); recovered,renv,rinfo=new_target(directory,'failure-recovery',args.port+2,enriched);targets.append(recovered)
        if r.inventory(recovered)!=before: raise RuntimeError('Post-failure recovery mismatch')
        evidence['recoverySeconds']=round(time.monotonic()-start,3);evidence['checks']['postFailureFreshRestore']='PASS'
        execute(target,forward,'happy_schema_migrator',env['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(target)
        after=canonical(target)
        for name in after:
            if sorted(after[name],key=lambda x:x['Id'])!=sorted(before['rows'][name],key=lambda x:x['Id']): raise RuntimeError('Full content reconciliation failed: '+name)
        evidence['checks']['completeRowsAssociationsPublication']='PASS'
        for table,tag in [('catalog.cities',1),('catalog.categories',2),('discovery.discovery_entries',3)]:
            mismatch=execute(target,f"SELECT count(*) FROM {table} WHERE legacy_id IS NOT NULL AND id <> ('{tag:08x}-0000-8000-8000-'||lpad(to_hex(legacy_id),12,'0'))::uuid")
            if mismatch!='0':raise RuntimeError('UUID mapping differs from approved injective encoding')
        evidence['checks']['allUuidMappingsAndBoundaryIds']='PASS'

        evidence['rowDigests']={k:r.digest(sorted(v,key=lambda x:x['Id'])) for k,v in after.items()}
        # Immutable first migration history plus exactly one appended forward entry.
        history=json.loads(execute(target,'SELECT json_agg(t ORDER BY "MigrationId") FROM public."__EFMigrationsHistory" t;'))
        if len(history)!=2 or history[0]!=before['rows']['__EFMigrationsHistory'][0]:raise RuntimeError('Migration history changed')
        evidence['migrationHistory']=history
        r.run('dotnet','ef','database','update','--project',project,'--configuration','Release','--no-build',env=process)
        if canonical(target)!=after: raise RuntimeError('Repeat migration changed content')
        evidence['checks']['secondRunNoOp']='PASS'
        clean,cenv,cinfo=new_target(directory,'clean',args.port+3);targets.append(clean);roles(clean,cenv)
        execute(clean,(directory/'clean.sql').read_text(encoding='utf-8-sig'),'happy_schema_migrator',cenv['MIGRATION_PASSWORD'],'-c role=happy_owner');grants(clean)
        if len(canonical(clean)['DiscoveryEntries'])!=6:raise RuntimeError('Clean path fixture mismatch')
        evidence['checks']['cleanCreation']='PASS'
        negatives=['CREATE TABLE public.denied(id int)','DELETE FROM discovery.discovery_entries WHERE false','SET ROLE happy_owner','SET ROLE happy_schema_migrator','SET ROLE happy_migrator','CREATE ROLE denied','GRANT happy_owner TO happy_app']
        for user,password in [('happy_app',env['HAPPY_APP_PASSWORD']),('happy_backup',env['BACKUP_PASSWORD'])]:
            if execute(target,'SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()',user,password)!='t':raise RuntimeError('TLS missing')
            if execute(target,'SELECT count(*) FROM discovery.discovery_entries',user,password)!='8':raise RuntimeError('Role read failed')
            for statement in negatives:execute(target,statement,user,password,expect_failure=True)
        for statement in ['CREATE ROLE denied','ALTER ROLE happy_schema_migrator SUPERUSER','SET ROLE happy_migrator']:
            execute(target,statement,'happy_schema_migrator',env['MIGRATION_PASSWORD'],expect_failure=True)
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
        for statement in ['CREATE ROLE denied','SET ROLE happy_migrator','ALTER ROLE happy_restore SUPERUSER']:
            execute(final_restore,statement,'happy_restore',restore_password,expect_failure=True)
        execute(final_restore,'ALTER ROLE happy_restore NOLOGIN; REVOKE happy_owner FROM happy_restore;')
        evidence['checks']['dedicatedRestoreIdentityAndRevocation']='PASS'
        evidence['checks']['migratedArchiveAllTablesConstraintsIndexes']='PASS'
        ownership=execute(target,"SELECT count(*) FROM pg_tables WHERE schemaname IN ('catalog','discovery') AND tableowner <> 'happy_owner'")
        if ownership!='0':raise RuntimeError('Object ownership separation failed')
        evidence['checks']['nonLoginObjectOwnership']='PASS'

        # Use the clean isolated instance for existing six-record regression tests.
        clean_dir=directory/'clean';clean_dir.mkdir();r.private_write(clean_dir/'postgres.crt',r.docker('exec',clean,'cat','/var/lib/postgresql-tls/server.crt'))
        r.private_write(directory/'rehearsal.env','ConnectionStrings__HappyCity='+connection(clean_dir,args.port+3,'happy_app',cenv['HAPPY_APP_PASSWORD'])+'\nHAPPYCITY_MIGRATION_CONNECTION='+connection(clean_dir,args.port+3,'happy_schema_migrator',cenv['MIGRATION_PASSWORD'])+';Options=-c role=happy_owner\n')
        evidence['targetContainers']=targets;evidence['regressionContainer']=clean;evidence['completedUtc']=dt.datetime.now(dt.timezone.utc).isoformat()
        evidence['status']='PASS';r.private_write(directory/'evidence.json',json.dumps(evidence,indent=2)+'\n')
        print(json.dumps({'status':'PASS','evidence':str(directory.relative_to(ROOT)/'evidence.json'),'environment':str(directory.relative_to(ROOT)/'rehearsal.env'),'checks':evidence['checks']},indent=2))
    except Exception:
        for target in targets:r.docker('stop',target)
        raise

if __name__=='__main__':main()
