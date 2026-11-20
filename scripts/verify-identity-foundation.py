"""HA-003-A isolated synthetic rehearsal. Never connects to a preserved source database.

Only sanitized metadata/check results are publishable. Archives, full snapshots, generated
SQL and environment files stay in the ACL-protected task directory and are not uploaded.
"""
import argparse
import copy
import datetime as dt
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import zipfile

spec = importlib.util.spec_from_file_location('forward', Path(__file__).with_name('verify-forward-migration.py'))
f = importlib.util.module_from_spec(spec)
spec.loader.exec_module(f)
r, ROOT = f.r, f.ROOT
BASELINE = '216df25c5656a979fcf67dfe3a16d07d7d66f154'
PROJECT = 'HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure'
TABLES = ['roles', 'users', 'role_claims', 'user_claims', 'user_logins', 'user_roles', 'user_tokens']
PRINCIPALS = ['happy_identity_owner', 'happy_identity_migrator', 'happy_identity_app', 'happy_identity_backup']
HISTORY = 'CREATE TABLE IF NOT EXISTS identity."__EFMigrationsHistory" ("MigrationId" varchar(150) NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,"ProductVersion" varchar(32) NOT NULL);'


def query(container, sql):
    value=f.execute(container, sql)
    return json.loads(value) if value else []


def original_state(container):
    value = f.complete_state(container)
    # New principals get CONNECT only. Compare every existing grant unchanged;
    # new-principal database grants are independently checked by identity_security.
    value['security']['objectAcl'] = [row for row in value['security']['objectAcl']
                                      if not row['grantee'].startswith('happy_identity_')]
    return value


def identity_metadata(container):
    queries = {
        'columns': "SELECT coalesce(json_agg(t ORDER BY table_name,column_name),'[]') FROM (SELECT table_name,column_name,data_type,udt_name,is_nullable,character_maximum_length,column_default,is_identity,identity_generation FROM information_schema.columns WHERE table_schema='identity') t",
        'constraints': "SELECT coalesce(json_agg(t ORDER BY tab,name),'[]') FROM (SELECT c.relname tab,con.conname name,con.contype type,pg_get_constraintdef(con.oid) definition FROM pg_constraint con JOIN pg_class c ON c.oid=con.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='identity' AND con.contype<>'n') t",
        'indexes': "SELECT coalesce(json_agg(t ORDER BY tablename,indexname),'[]') FROM (SELECT tablename,indexname,indexdef FROM pg_indexes WHERE schemaname='identity') t",
        'ownership': "SELECT coalesce(json_agg(t ORDER BY name),'[]') FROM (SELECT c.relname name,c.relkind kind,pg_get_userbyid(c.relowner) owner FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='identity' AND c.relkind IN ('r','i','S')) t",
        'sequences': "SELECT coalesce(json_agg(t ORDER BY sequence),'[]') FROM (SELECT c.relname sequence,format_type(s.seqtypid,NULL) type,s.seqstart,s.seqincrement,s.seqmin,s.seqmax,s.seqcache,s.seqcycle,t.relname tab,a.attname col,d.deptype FROM pg_sequence s JOIN pg_class c ON c.oid=s.seqrelid JOIN pg_namespace n ON n.oid=c.relnamespace JOIN pg_depend d ON d.objid=c.oid AND d.deptype='i' JOIN pg_class t ON t.oid=d.refobjid JOIN pg_attribute a ON a.attrelid=t.oid AND a.attnum=d.refobjsubid WHERE n.nspname='identity') t"
    }
    return {key: query(container, sql) for key, sql in queries.items()}


def identity_security(container):
    names = ','.join("'" + name + "'" for name in PRINCIPALS + ['happy_app', 'happy_backup', 'happy_schema_migrator'])
    queries = {
        'roles': f"SELECT json_agg(t ORDER BY rolname) FROM (SELECT rolname,rolsuper,rolinherit,rolcreaterole,rolcreatedb,rolcanlogin,rolreplication,rolbypassrls FROM pg_roles WHERE rolname IN ({names})) t",
        'memberships': f"SELECT coalesce(json_agg(t ORDER BY member,role),'[]') FROM (SELECT a.rolname member,b.rolname role,m.admin_option,m.inherit_option,m.set_option,pg_get_userbyid(m.grantor) grantor FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.member JOIN pg_roles b ON b.oid=m.roleid WHERE a.rolname IN ({names})) t",
        'tables': f"SELECT json_agg(t ORDER BY role,tab,privilege) FROM (SELECT r.rolname role,c.relname tab,p privilege,has_table_privilege(r.oid,c.oid,p) allowed,has_table_privilege(r.oid,c.oid,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE','DELETE','TRUNCATE','REFERENCES','TRIGGER','MAINTAIN']) p WHERE r.rolname IN ({names}) AND n.nspname='identity' AND c.relkind='r') t",
        'columns': f"SELECT json_agg(t ORDER BY role,tab,col,privilege) FROM (SELECT r.rolname role,c.relname tab,a.attname col,p privilege,has_column_privilege(r.oid,c.oid,a.attnum,p) allowed,has_column_privilege(r.oid,c.oid,a.attnum,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace JOIN pg_attribute a ON a.attrelid=c.oid CROSS JOIN unnest(ARRAY['SELECT','INSERT','UPDATE','REFERENCES']) p WHERE r.rolname IN ({names}) AND n.nspname='identity' AND c.relkind='r' AND a.attnum>0 AND NOT a.attisdropped) t",
        'sequences': f"SELECT json_agg(t ORDER BY role,name,privilege) FROM (SELECT r.rolname role,c.relname name,p privilege,has_sequence_privilege(r.oid,c.oid,p) allowed,has_sequence_privilege(r.oid,c.oid,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace CROSS JOIN unnest(ARRAY['USAGE','SELECT','UPDATE']) p WHERE r.rolname IN ({names}) AND n.nspname='identity' AND c.relkind='S') t",
        'schemas': f"SELECT json_agg(t ORDER BY role,schema,privilege) FROM (SELECT r.rolname role,n.nspname schema,p privilege,has_schema_privilege(r.oid,n.oid,p) allowed,has_schema_privilege(r.oid,n.oid,p||' WITH GRANT OPTION') grantable FROM pg_roles r CROSS JOIN pg_namespace n CROSS JOIN unnest(ARRAY['USAGE','CREATE']) p WHERE r.rolname IN ({names}) AND n.nspname IN ('identity','catalog','discovery','public')) t",
        'database': f"SELECT json_agg(t ORDER BY role,privilege) FROM (SELECT rolname role,p privilege,has_database_privilege(oid,'happyalmere',p) allowed,has_database_privilege(oid,'happyalmere',p||' WITH GRANT OPTION') grantable FROM pg_roles CROSS JOIN unnest(ARRAY['CONNECT','CREATE','TEMP']) p WHERE rolname IN ({names})) t",
        'defaultAcl': "SELECT coalesce(json_agg(t),'[]') FROM (SELECT pg_get_userbyid(defaclrole) role,defaclobjtype type,defaclacl::text acl FROM pg_default_acl WHERE pg_get_userbyid(defaclrole) LIKE 'happy_identity_%') t",
        'rawAcl': "SELECT coalesce(json_agg(t ORDER BY kind,name,col,grantor,grantee,privilege_type),'[]') FROM (SELECT o.kind,o.name,o.col,pg_get_userbyid(x.grantor) grantor,CASE WHEN x.grantee=0 THEN 'PUBLIC' ELSE pg_get_userbyid(x.grantee) END grantee,x.privilege_type,x.is_grantable FROM (SELECT 'relation' kind,c.relname name,'' col,c.relacl acl FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='identity' UNION ALL SELECT 'column',c.relname,a.attname,a.attacl FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='identity' AND a.attnum>0 AND NOT a.attisdropped UNION ALL SELECT 'schema',nspname,'',nspacl FROM pg_namespace WHERE nspname='identity') o CROSS JOIN LATERAL aclexplode(o.acl) x) t",
        'functions': "SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='identity'"
    }
    return {key: query(container, sql) for key, sql in queries.items()}


def assert_contract(actual, expected):
    for kind in ['columns','constraints','indexes','ownership','sequences']:
        if actual[kind] != expected[kind]:
            raise RuntimeError('Independent Identity contract mismatch: '+kind)


def state(container):
    rows = {}
    for table in TABLES + ['__EFMigrationsHistory']:
        exists = f.execute(container, f"SELECT to_regclass('identity.\"{table}\"') IS NOT NULL") == 't'
        if exists:
            rows[table] = query(container, f'SELECT coalesce(json_agg(t ORDER BY row_to_json(t)::text),\'[]\') FROM identity."{table}" t')
    sequences = {}
    for name in ['user_claims_id_seq', 'role_claims_id_seq']:
        if f.execute(container, f"SELECT to_regclass('identity.{name}') IS NOT NULL") == 't':
            sequences[name] = query(container, f'SELECT row_to_json(t) FROM (SELECT last_value,is_called FROM identity.{name}) t')
    return {'original': original_state(container), 'identity': identity_metadata(container),
            'security': identity_security(container), 'rows': rows, 'sequenceState': sequences}


def provision(container, env):
    for name in PRINCIPALS:
        if name.endswith('_owner'):
            suffix = 'NOLOGIN'
        else:
            env[name] = secrets.token_hex(32)
            suffix = "LOGIN PASSWORD '" + env[name] + "'"
        f.execute(container, f'CREATE ROLE {name} {suffix} NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;')
    f.execute(container, """
        GRANT happy_identity_owner TO happy_identity_migrator;
        GRANT CONNECT ON DATABASE happyalmere TO happy_identity_migrator,happy_identity_app,happy_identity_backup;
        CREATE SCHEMA identity AUTHORIZATION happy_identity_owner;
        REVOKE ALL ON SCHEMA identity FROM PUBLIC;
        GRANT USAGE ON SCHEMA identity TO happy_identity_app,happy_identity_backup;
        """)


def grant_identity(container):
    tables = ','.join('identity.' + name for name in TABLES)
    f.execute(container, f"""SET ROLE happy_identity_owner;
        GRANT SELECT,INSERT,UPDATE,DELETE ON {tables} TO happy_identity_app;
        GRANT SELECT ON {tables},identity."__EFMigrationsHistory" TO happy_identity_backup;
        GRANT USAGE ON SEQUENCE identity.user_claims_id_seq,identity.role_claims_id_seq TO happy_identity_app;
        GRANT SELECT ON SEQUENCE identity.user_claims_id_seq,identity.role_claims_id_seq TO happy_identity_backup;
        """)


def verify_permissions(container, env):
    security = identity_security(container)
    owners = ['happy_identity_owner', 'happy_identity_migrator']
    for role in security['roles']:
        if role['rolname'] in PRINCIPALS:
            if any(role[k] for k in ['rolsuper','rolcreaterole','rolcreatedb','rolreplication','rolbypassrls']):
                raise RuntimeError('Elevated identity role')
            assert role['rolcanlogin'] == (role['rolname'] != 'happy_identity_owner')
    memberships = [x for x in security['memberships'] if x['member'] in PRINCIPALS]
    assert len(memberships) == 1 and memberships[0]['member'] == 'happy_identity_migrator' and memberships[0]['role'] == 'happy_identity_owner'
    assert not memberships[0]['admin_option'] and memberships[0]['set_option'] and memberships[0]['inherit_option']
    for kind in ['tables', 'columns']:
        for row in security[kind]:
            expected = row['role'] in owners or (row['role'] == 'happy_identity_app' and row['tab'] in TABLES and row['privilege'] in ['SELECT','INSERT','UPDATE','DELETE']) or (row['role'] == 'happy_identity_backup' and row['privilege'] == 'SELECT')
            assert row['allowed'] == expected and row['grantable'] == (row['role'] in owners), (kind, row['role'], row['tab'], row['privilege'])
    for row in security['sequences']:
        expected = row['role'] in owners or (row['role'] == 'happy_identity_app' and row['privilege'] == 'USAGE') or (row['role'] == 'happy_identity_backup' and row['privilege'] == 'SELECT')
        assert row['allowed'] == expected and row['grantable'] == (row['role'] in owners)
    for row in security['schemas']:
        if row['role'] not in PRINCIPALS: continue
        expected = (row['schema'] == 'identity' and (row['role'] in owners or row['privilege']=='USAGE')) or (row['schema']=='public' and row['privilege']=='USAGE')
        assert row['allowed'] == expected
        assert row['grantable'] == (row['schema']=='identity' and row['role'] in owners)
    for row in security['database']:
        if row['role'] in PRINCIPALS:
            assert row['allowed'] == (row['privilege']=='CONNECT' and row['role']!='happy_identity_owner')
            assert not row['grantable']
    assert security['defaultAcl'] == [] and security['functions'] == 0
    assert not any(x['grantee']=='PUBLIC' for x in security['rawAcl'])
    # Actual authenticated VerifyFull connections before denials; no false pass for outages.
    for name in ['happy_identity_app', 'happy_identity_backup']:
        assert f.execute(container, 'SELECT current_user', name, env[name]) == name
        assert f.execute(container, 'SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()', name, env[name]) == 't'
        assert f.execute(container, 'SELECT count(*) >= 0 FROM identity.users', name, env[name]) == 't'
        for sql in ['SELECT * FROM catalog.cities', 'SET ROLE happy_owner', 'SET ROLE happy_migrator', 'CREATE ROLE forbidden']:
            f.execute(container, sql, name, env[name], expected_state='42501')
    for name,pwd in [('happy_app',env['HAPPY_APP_PASSWORD']),('happy_backup',env['BACKUP_PASSWORD']),('happy_schema_migrator',env['MIGRATION_PASSWORD'])]:
        assert f.execute(container,'SELECT current_user',name,pwd)==name
        f.execute(container,'SELECT * FROM identity.users',name,pwd,expected_state='42501')
        f.execute(container,'SET ROLE happy_identity_owner',name,pwd,expected_state='42501')
    for sql in ['CREATE TABLE identity.denied(id integer)', 'SELECT * FROM identity."__EFMigrationsHistory"', 'SET ROLE happy_identity_owner', "SELECT setval('identity.user_claims_id_seq',1)"]:
        f.execute(container,sql,'happy_identity_app',env['happy_identity_app'],expected_state='42501')
    f.execute(container,'SET ROLE happy_identity_owner; CREATE TABLE identity.private_probe(id integer);')
    try:
        for name in ['happy_identity_app','happy_identity_backup']:
            f.execute(container,'SELECT * FROM identity.private_probe',name,env[name],expected_state='42501')
    finally:
        f.execute(container,'SET ROLE happy_identity_owner; DROP TABLE identity.private_probe;')
    return security


def dump(container, user, password, schemas):
    process=dict(os.environ,PGPASSWORD=password)
    args=['docker','exec','-e','PGPASSWORD',container,'pg_dump',
          '--dbname=host=localhost dbname=happyalmere user='+user+' sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt','-Fc']
    for schema in schemas:args+=['--schema',schema]
    return r.run(*args,env=process,binary=True)


def restore_catalog(container, env, archive):
    listing=r.run('docker','exec','-i',container,'pg_restore','--list',input=archive,binary=True).decode()
    filtered='\n'.join(x for x in listing.splitlines() if not re.search(r' (?:SCHEMA - public|COMMENT - SCHEMA public) ',x))+'\n'
    r.docker('exec','-i',container,'sh','-c','cat > /tmp/catalog-restore.list',input=filtered)
    result=subprocess.run(['docker','exec','-i','-e','PGPASSWORD','-e','PGOPTIONS=-c role=happy_owner',container,'pg_restore',
          '--dbname=host=localhost dbname=happyalmere user=happy_schema_migrator sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt',
          '--no-owner','--no-acl','--single-transaction','--exit-on-error','--use-list=/tmp/catalog-restore.list'],input=archive,
          env=dict(os.environ,PGPASSWORD=env['MIGRATION_PASSWORD']),capture_output=True)
    if result.returncode:
        r.private_write(env['certificateDirectory']/'restore-error-private.txt',result.stderr)
        raise RuntimeError('Catalog restoration failed; protected local diagnostics retained')
    f.grants(container)


def authority_inventory(container):
    """ACL-01..20. No data rows, credentials or cluster-internal object inventory.

    acldefault is essential: a NULL routine ACL includes PUBLIC EXECUTE. Column
    ACLs have no implicit grants; effective column access also includes table ACLs.
    Keep names/signatures, not cluster-specific OIDs, for cross-instance equality.
    """
    sql="""
    WITH ns AS (SELECT * FROM pg_namespace WHERE nspname IN ('identity','catalog','discovery','public')),
    roles AS (SELECT * FROM pg_roles WHERE rolname LIKE 'happy_%' AND rolname NOT IN ('happy_restore','happy_identity_restore')),
    objects AS (
      SELECT 'table' kind,n.nspname schema,c.relname name,'' col,c.oid,0 subid,c.relowner owner,c.relacl raw,
        coalesce(c.relacl,acldefault('r',c.relowner)) acl,
        ARRAY['SELECT','INSERT','UPDATE','DELETE','TRUNCATE','REFERENCES','TRIGGER','MAINTAIN'] privileges
        FROM pg_class c JOIN ns n ON n.oid=c.relnamespace WHERE c.relkind IN ('r','p','v','m','f')
      UNION ALL SELECT 'sequence',n.nspname,c.relname,'',c.oid,0,c.relowner,c.relacl,
        coalesce(c.relacl,acldefault('s',c.relowner)),ARRAY['USAGE','SELECT','UPDATE']
        FROM pg_class c JOIN ns n ON n.oid=c.relnamespace WHERE c.relkind='S'
      UNION ALL SELECT 'column',n.nspname,c.relname,a.attname,c.oid,a.attnum,c.relowner,a.attacl,
        coalesce(a.attacl,'{}'::aclitem[]),ARRAY['SELECT','INSERT','UPDATE','REFERENCES']
        FROM pg_class c JOIN ns n ON n.oid=c.relnamespace JOIN pg_attribute a ON a.attrelid=c.oid
        WHERE c.relkind IN ('r','p','v','m','f') AND a.attnum>0 AND NOT a.attisdropped
      UNION ALL SELECT 'routine',n.nspname,p.proname||'('||pg_get_function_identity_arguments(p.oid)||')','',p.oid,0,p.proowner,p.proacl,
        coalesce(p.proacl,acldefault('f',p.proowner)),ARRAY['EXECUTE'] FROM pg_proc p JOIN ns n ON n.oid=p.pronamespace
      UNION ALL SELECT 'schema',nspname,nspname,'',oid,0,nspowner,nspacl,coalesce(nspacl,acldefault('n',nspowner)),ARRAY['USAGE','CREATE'] FROM ns
      UNION ALL SELECT 'database','','happyalmere','',oid,0,datdba,datacl,coalesce(datacl,acldefault('d',datdba)),ARRAY['CONNECT','CREATE','TEMP']
        FROM pg_database WHERE datname='happyalmere'
    ), effective AS (
      SELECT o.kind,o.schema,o.name,o.col,r.rolname role,p privilege,g grant_option,
      CASE o.kind
        WHEN 'table' THEN has_table_privilege(r.oid,o.oid,p||g)
        WHEN 'column' THEN has_column_privilege(r.oid,o.oid,o.subid::smallint,p||g)
        WHEN 'sequence' THEN has_sequence_privilege(r.oid,o.oid,p||g)
        WHEN 'routine' THEN has_function_privilege(r.oid,o.oid,p||g)
        WHEN 'schema' THEN has_schema_privilege(r.oid,o.oid,p||g)
        WHEN 'database' THEN has_database_privilege(r.oid,o.oid,p||g)
      END allowed FROM objects o CROSS JOIN roles r CROSS JOIN LATERAL unnest(o.privileges) p
        CROSS JOIN unnest(ARRAY['',' WITH GRANT OPTION']) g
    ) SELECT json_build_object(
      'roles',(SELECT json_agg(t ORDER BY rolname) FROM (SELECT rolname,rolsuper,rolinherit,rolcreaterole,rolcreatedb,rolcanlogin,rolreplication,rolbypassrls,rolconnlimit,rolvaliduntil FROM roles) t),
      'memberships',(SELECT coalesce(json_agg(t ORDER BY member,role,grantor),'[]') FROM (SELECT a.rolname member,b.rolname role,pg_get_userbyid(m.grantor) grantor,m.admin_option,m.inherit_option,m.set_option FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.member JOIN pg_roles b ON b.oid=m.roleid WHERE a.rolname IN (SELECT rolname FROM roles) OR b.rolname IN (SELECT rolname FROM roles)) t),
      'paths',(SELECT json_agg(t ORDER BY member,role,privilege) FROM (SELECT a.rolname member,b.rolname role,p privilege,pg_has_role(a.oid,b.oid,p) allowed FROM roles a CROSS JOIN pg_roles b CROSS JOIN unnest(ARRAY['MEMBER','USAGE','SET','MEMBER WITH ADMIN OPTION']) p WHERE a.oid<>b.oid AND b.rolname NOT IN ('happy_restore','happy_identity_restore')) t),
      'objects',(SELECT json_agg(t ORDER BY kind,schema,name,col) FROM (SELECT kind,schema,name,col,pg_get_userbyid(owner) owner,raw IS NULL implicit_acl FROM objects) t),
      'acl',(SELECT coalesce(json_agg(t ORDER BY kind,schema,name,col,grantor,grantee,privilege_type),'[]') FROM (SELECT o.kind,o.schema,o.name,o.col,pg_get_userbyid(x.grantor) grantor,CASE WHEN x.grantee=0 THEN 'PUBLIC' ELSE pg_get_userbyid(x.grantee) END grantee,x.privilege_type,x.is_grantable FROM objects o CROSS JOIN LATERAL aclexplode(o.acl) x) t),
      'effective',(SELECT json_agg(e ORDER BY kind,schema,name,col,role,privilege,grant_option) FROM effective e),
      'ownership',(SELECT json_agg(t ORDER BY schema,name,kind) FROM (SELECT n.nspname schema,c.relname name,c.relkind kind,pg_get_userbyid(c.relowner) owner FROM pg_class c JOIN ns n ON n.oid=c.relnamespace) t),
      'routines',(SELECT json_agg(t ORDER BY schema,name,args) FROM (SELECT n.nspname schema,p.proname name,pg_get_function_identity_arguments(p.oid) args,p.prokind kind,pg_get_userbyid(p.proowner) owner,p.prosecdef security_definer,p.proconfig config,pg_get_functiondef(p.oid) definition FROM pg_proc p JOIN ns n ON n.oid=p.pronamespace) t),
      'defaultDefinitions',(SELECT coalesce(json_agg(t ORDER BY owner,schema,kind),'[]') FROM (SELECT pg_get_userbyid(d.defaclrole) owner,coalesce(n.nspname,'*') schema,d.defaclobjtype kind,cardinality(d.defaclacl)=0 empty_acl FROM pg_default_acl d LEFT JOIN pg_namespace n ON n.oid=d.defaclnamespace WHERE d.defaclnamespace=0 OR n.nspname IN ('identity','catalog','discovery','public') OR d.defaclrole IN (SELECT oid FROM roles)) t),
      'defaults',(SELECT coalesce(json_agg(t ORDER BY owner,schema,kind,grantor,grantee,privilege_type),'[]') FROM (SELECT pg_get_userbyid(d.defaclrole) owner,coalesce(n.nspname,'*') schema,d.defaclobjtype kind,pg_get_userbyid(x.grantor) grantor,CASE WHEN x.grantee=0 THEN 'PUBLIC' ELSE pg_get_userbyid(x.grantee) END grantee,x.privilege_type,x.is_grantable FROM pg_default_acl d LEFT JOIN pg_namespace n ON n.oid=d.defaclnamespace CROSS JOIN LATERAL aclexplode(d.defaclacl) x WHERE d.defaclnamespace=0 OR n.nspname IN ('identity','catalog','discovery','public') OR d.defaclrole IN (SELECT oid FROM roles)) t)
    )
    """
    # Only this read-only metadata query gets structured diagnostics. Never emit
    # server DETAIL/CONTEXT, SQL text, row values or connection information.
    result=subprocess.run(['docker','exec','-i',container,'psql','-X','-qAt','-v','ON_ERROR_STOP=1',
                           '-v','VERBOSITY=verbose','-U','happy_migrator','-d','happyalmere'],
                          input=sql,text=True,encoding='utf-8',capture_output=True)
    if result.returncode:
        code=re.search(r'ERROR:\s+([0-9A-Z]{5}):',result.stderr)
        location=re.search(r'LOCATION:\s+([A-Za-z0-9_]+),\s+([A-Za-z0-9_.]+):([0-9]+)',result.stderr)
        line=re.search(r'LINE ([0-9]+):',result.stderr)
        raise RuntimeError('Authority inventory SQL failed: SQLSTATE '+(code[1] if code else 'unavailable')+
                           '; statement line '+(line[1] if line else 'unavailable')+
                           '; server routine '+(location[1] if location else 'unavailable'))
    return json.loads(result.stdout)


def authority_mismatch(actual, expected):
    # Expected comes only from the independently provisioned reference: accepted
    # immutable historical migration + literal Identity SQL + explicit policy.
    # Never learn an expected permission from an upgraded/recovered target.
    return [key for key in expected if actual[key] != expected[key]]


def authority_digest(value):
    return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()


def verify_authority(container, expected):
    actual=authority_inventory(container)
    mismatch=authority_mismatch(actual,expected)
    if mismatch: raise RuntimeError('Independent authority mismatch: '+','.join(mismatch))
    return {'status':'PASS','sha256':authority_digest(actual),
            'dimensions':{key:len(value or []) for key,value in actual.items()}}


def mutation_cases():
    """Each case changes one independent policy dimension; never repairs a target.

    Mutations are applied only to the isolated synthetic upgrade instance by its
    provisioning actor. Explicit cleanup restores grant provenance as well as rights.
    """
    cases=[]
    def add(identifier, variant, change, cleanup, dimension):
        cases.append((identifier,variant,change,cleanup,dimension))
    for table in TABLES:
        for privilege in ['SELECT','INSERT','UPDATE','DELETE']:
            add('T06' if table=='users' else 'T07',table+'-'+privilege,
                f'REVOKE {privilege} ON identity.{table} FROM happy_identity_app',
                f'SET ROLE happy_identity_owner; GRANT {privilege} ON identity.{table} TO happy_identity_app','effective')
    for privilege in ['TRUNCATE','REFERENCES','TRIGGER','MAINTAIN']:
        add('T08',privilege,f'GRANT {privilege} ON identity.users TO happy_identity_app',
            f'REVOKE {privilege} ON identity.users FROM happy_identity_app','effective')
    for privilege in ['SELECT','INSERT','UPDATE','DELETE']:
        add('T09' if privilege=='SELECT' else 'T10',privilege,
            f'GRANT {privilege} ON identity."__EFMigrationsHistory" TO happy_identity_app',
            f'REVOKE {privilege} ON identity."__EFMigrationsHistory" FROM happy_identity_app','effective')
    for sequence in ['user_claims_id_seq','role_claims_id_seq']:
        add('T11',sequence,f'REVOKE USAGE ON SEQUENCE identity.{sequence} FROM happy_identity_app',
            f'SET ROLE happy_identity_owner; GRANT USAGE ON SEQUENCE identity.{sequence} TO happy_identity_app','effective')
        for privilege in ['SELECT','UPDATE']:
            add('T12',sequence+'-'+privilege,f'GRANT {privilege} ON SEQUENCE identity.{sequence} TO happy_identity_app',
                f'REVOKE {privilege} ON SEQUENCE identity.{sequence} FROM happy_identity_app','effective')
    add('T13','schema-create','GRANT CREATE ON SCHEMA identity TO happy_identity_app','REVOKE CREATE ON SCHEMA identity FROM happy_identity_app','effective')
    for privilege in ['CREATE','TEMP']:
        add('T14',privilege,f'GRANT {privilege} ON DATABASE happyalmere TO happy_identity_app',f'REVOKE {privilege} ON DATABASE happyalmere FROM happy_identity_app','effective')
    add('T15','table-grant-option','GRANT SELECT ON identity.users TO happy_identity_app WITH GRANT OPTION','REVOKE GRANT OPTION FOR SELECT ON identity.users FROM happy_identity_app','effective')
    for variant,target,privilege in [('column','identity.users','SELECT(id)'),('sequence','SEQUENCE identity.user_claims_id_seq','USAGE'),('schema','SCHEMA identity','USAGE'),('database','DATABASE happyalmere','CONNECT')]:
        revoke='' if variant=='column' else 'GRANT OPTION FOR '
        add('T15',variant+'-grant-option',f'GRANT {privilege} ON {target} TO happy_identity_app WITH GRANT OPTION',f'REVOKE {revoke}{privilege} ON {target} FROM happy_identity_app','effective')
    add('T16','direct-owner','GRANT happy_identity_owner TO happy_identity_app','REVOKE happy_identity_owner FROM happy_identity_app','memberships')
    for attribute in ['SUPERUSER','CREATEDB','CREATEROLE','REPLICATION','BYPASSRLS']:
        add('T17',attribute,f'ALTER ROLE happy_identity_app {attribute}',f'ALTER ROLE happy_identity_app NO{attribute}','roles')
    add('T18','table-owner','ALTER TABLE identity.users OWNER TO happy_identity_app','ALTER TABLE identity.users OWNER TO happy_identity_owner; SET ROLE happy_identity_owner; GRANT SELECT,INSERT,UPDATE,DELETE ON identity.users TO happy_identity_app','ownership')
    for schema,table,identifier in [('catalog','cities','T19'),('discovery','discovery_entries','T20'),('public','"__EFMigrationsHistory"','T21')]:
        for principal in ['happy_identity_app','happy_identity_backup']:
            add(identifier,principal+'-latent',f'GRANT SELECT ON {schema}.{table} TO {principal}',f'REVOKE SELECT ON {schema}.{table} FROM {principal}','effective')
        if schema!='public':
            add(identifier,'usable-app-access',f'GRANT SELECT ON {schema}.{table} TO happy_identity_app; GRANT USAGE ON SCHEMA {schema} TO happy_identity_app',f'REVOKE SELECT ON {schema}.{table} FROM happy_identity_app; REVOKE USAGE ON SCHEMA {schema} FROM happy_identity_app','effective')
    for schema,table,col in [('identity','"__EFMigrationsHistory"','"MigrationId"'),('catalog','cities','name')]:
        add('T22',schema+'-column-only',f'GRANT SELECT({col}) ON {schema}.{table} TO happy_identity_app',f'REVOKE SELECT({col}) ON {schema}.{table} FROM happy_identity_app','effective')
    add('T23','PUBLIC','GRANT SELECT ON identity.users TO PUBLIC','REVOKE SELECT ON identity.users FROM PUBLIC','effective')
    for owner in ['happy_identity_owner','happy_owner','happy_migrator']:
        for kind,privilege,identifier in [('TABLES','SELECT','T24'),('SEQUENCES','USAGE','T25')]:
            add(identifier,owner+'-'+kind,f'ALTER DEFAULT PRIVILEGES FOR ROLE {owner} IN SCHEMA identity GRANT {privilege} ON {kind} TO happy_identity_app',f'ALTER DEFAULT PRIVILEGES FOR ROLE {owner} IN SCHEMA identity REVOKE {privilege} ON {kind} FROM happy_identity_app','defaults')
    add('T26','latent-routine','GRANT EXECUTE ON FUNCTION catalog.check_hierarchy() TO happy_identity_app','REVOKE EXECUTE ON FUNCTION catalog.check_hierarchy() FROM happy_identity_app','effective')
    add('T26','implicit-PUBLIC-routine','SET ROLE happy_identity_owner; CREATE FUNCTION identity.r01_probe() RETURNS integer LANGUAGE sql AS $$ SELECT 1 $$','DROP FUNCTION identity.r01_probe()','acl')
    add('T27','security-definer','ALTER FUNCTION catalog.check_hierarchy() SECURITY DEFINER','ALTER FUNCTION catalog.check_hierarchy() SECURITY INVOKER','routines')
    add('T27','search-path','ALTER FUNCTION catalog.check_hierarchy() SET search_path=public','ALTER FUNCTION catalog.check_hierarchy() RESET search_path','routines')
    add('T28','transitive','GRANT happy_schema_migrator TO happy_identity_migrator','REVOKE happy_schema_migrator FROM happy_identity_migrator','paths')
    for option,alter,restore in [('ADMIN','TRUE','FALSE'),('INHERIT','FALSE','TRUE'),('SET','FALSE','TRUE')]:
        add('T29',option,f'GRANT happy_identity_owner TO happy_identity_migrator WITH {option} {alter}',f'GRANT happy_identity_owner TO happy_identity_migrator WITH {option} {restore}','memberships')
    add('T30','delegated-owner','GRANT happy_identity_owner TO happy_backup WITH ADMIN TRUE','REVOKE happy_identity_owner FROM happy_backup','memberships')
    add('T31','future-private','SET ROLE happy_identity_owner; CREATE TABLE identity.r01_probe(id integer)','DROP TABLE identity.r01_probe','objects')
    for principal in ['happy_app','happy_backup','happy_schema_migrator']:
        add('T32',principal,f'GRANT SELECT ON identity.users TO {principal}',f'REVOKE SELECT ON identity.users FROM {principal}','effective')
    return cases


def verify_mutations(container, env, expected):
    results=[]
    runtime_source=(ROOT/'HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/Identity/IdentityConfiguration.cs').read_text(encoding='utf-8-sig')
    runtime_sql=re.search(r'private const string Sql = """(.*?)""";',runtime_source,re.S).group(1)
    runtime_reject={'T06','T07','T08','T09','T10','T11','T12','T13','T14','T15','T16','T17','T18','T22'}
    for identifier,variant,change,cleanup,dimension in mutation_cases():
        verify_authority(container,expected)
        assert f.execute(container,'SELECT current_user = \'happy_identity_app\' AND (SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid())','happy_identity_app',env['happy_identity_app'])=='t'
        # Positive authenticated operation precedes each deviation.
        f.execute(container,'SELECT count(*) FROM identity.users','happy_identity_app',env['happy_identity_app'])
        try:
            f.execute(container,change)
            mismatch=authority_mismatch(authority_inventory(container),expected)
            if dimension not in mismatch: raise RuntimeError('Mutation not detected: '+identifier+'/'+variant)
            reject=(identifier in runtime_reject and not (identifier=='T22' and variant.startswith('catalog'))) or variant=='usable-app-access'
            runtime=f.execute(container,runtime_sql,'happy_identity_app',env['happy_identity_app'])
            if runtime != ('f' if reject else 't'):raise RuntimeError('Runtime mutation boundary mismatch: '+identifier+'/'+variant)
            if identifier=='T16':
                assert f.execute(container,'SET ROLE happy_identity_owner; '+runtime_sql,'happy_identity_app',env['happy_identity_app'])=='f'
            if identifier in ['T19','T20','T21']:
                schema,table={'T19':('catalog','cities'),'T20':('discovery','discovery_entries'),'T21':('public','"__EFMigrationsHistory"')}[identifier]
                principal='happy_identity_backup' if variant.startswith('happy_identity_backup') else 'happy_identity_app'
                statement=f'SELECT count(*) FROM {schema}.{table}'
                if variant=='usable-app-access' or schema=='public':f.execute(container,statement,principal,env[principal])
                else:f.execute(container,statement,principal,env[principal],expected_state='42501')
        finally:
            f.execute(container,cleanup)
        verify_authority(container,expected)
        f.execute(container,'SELECT count(*) FROM identity.users','happy_identity_app',env['happy_identity_app'])
        assert f.execute(container,runtime_sql,'happy_identity_app',env['happy_identity_app'])=='t'
        results.append({'scenario':identifier,'variant':variant,'status':'PASS','expectedMismatch':dimension,'runtimeAcceptedDuringMutation':not reject,
                        'actualMismatch':mismatch,'cleanupAuthoritySha256':authority_digest(expected)})
    return results


def inspect_archive(archive, evidence_file):
    """Post-download CL05; original ZIP stays immutable, evidence is a working copy.

    Reject unknown formats/paths and inspect every JSON byte recursively. PNGs are
    decoded/visually reviewed by the browser reviewer separately; text chunks are
    rejected here. This is a bounded exclusion check, not universal secret proof.
    """
    import struct
    import zlib
    if Path(archive).resolve()==Path(evidence_file).resolve() or Path(evidence_file).name!='evidence.json':
        raise RuntimeError('Use a separate existing evidence.json working copy')
    findings=[];entries=[]
    sensitive_keys={'password','pwd','passwordhash','securitystamp','concurrencystamp','accesstoken','refreshtoken',
                    'clientsecret','privatekey','connectionstring','pgpassword'}
    def inspect_json(value):
        if isinstance(value,dict):
            for key,child in value.items():
                if re.sub(r'[^a-z]','',key.lower()) in sensitive_keys:return False
                if not inspect_json(child):return False
        elif isinstance(value,list):
            return all(inspect_json(child) for child in value)
        return True
    patterns=[rb'-----BEGIN [A-Z ]*PRIVATE KEY-----',rb'gh[pousr]_[A-Za-z0-9]{30,}',rb'github_pat_[A-Za-z0-9_]{40,}',
              rb'(?i)(?:password|pwd|clientsecret|access_token)\s*[=:]\s*[^\s;",}]{6,}',
              rb'(?i)host=[^;\s]+;.*(?:username|user id)=',rb'AQAAAAI[A-Za-z0-9+/=]{50,}']
    with zipfile.ZipFile(archive) as zipped:
        if zipped.testzip() is not None: raise RuntimeError('Artifact CRC failure')
        for member in zipped.infolist():
            name=member.filename
            if member.is_dir():continue
            if '..' in Path(name).parts or name.startswith(('/','\\')) or member.file_size>25_000_000:
                raise RuntimeError('Unsafe artifact member')
            data=zipped.read(member)
            if name.endswith('.json'):
                if not inspect_json(json.loads(data)):findings.append(name)
                if any(re.search(pattern,data) for pattern in patterns):findings.append(name)
                if re.search(rb'"(?:password_hash|passwordHash|security_stamp|concurrency_stamp|connectionString|PGPASSWORD)"\s*:',data):findings.append(name)
            elif name.endswith('.png'):
                if not data.startswith(b'\x89PNG\r\n\x1a\n'):raise RuntimeError('Invalid PNG artifact')
                offset=8;image_data=b'';ended=False
                while offset<len(data):
                    length=struct.unpack('>I',data[offset:offset+4])[0];kind=data[offset+4:offset+8]
                    payload=data[offset+8:offset+8+length];crc=data[offset+8+length:offset+12+length]
                    if len(crc)!=4 or zlib.crc32(kind+payload)!=struct.unpack('>I',crc)[0]:raise RuntimeError('PNG CRC failure')
                    if kind in [b'tEXt',b'zTXt',b'iTXt']:raise RuntimeError('Unapproved PNG text payload')
                    if kind==b'IDAT':image_data+=payload
                    offset+=length+12
                    if kind==b'IEND':ended=True;break
                if not ended or offset!=len(data) or not zlib.decompress(image_data):raise RuntimeError('Incomplete PNG artifact')
            else:raise RuntimeError('Unapproved artifact type: '+Path(name).suffix)
            entries.append({'path':name,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
    if findings:raise RuntimeError('Sensitive-content indicators detected; do not publish this archive')
    evidence=json.loads(Path(evidence_file).read_text(encoding='utf-8-sig'))
    record={'status':'PASS','phase':'post-download; not part of original CI archive','archiveSha256':hashlib.sha256(Path(archive).read_bytes()).hexdigest(),
            'entries':entries,'limitations':['Pattern/structure checks are not exhaustive secret certification. PNG rendered content requires separate visual review.']}
    evidence['postDownloadInspection']=record
    r.private_write(Path(evidence_file),json.dumps(evidence,indent=2)+'\n')
    print(json.dumps({'status':'PASS','archiveSha256':record['archiveSha256'],'entries':len(entries)}))


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=55510)
    parser.add_argument('--inspect-archive');parser.add_argument('--inspection-evidence');args=parser.parse_args()
    if args.inspect_archive:
        if not args.inspection_evidence:parser.error('--inspection-evidence requires a separate working copy of evidence.json')
        inspect_archive(args.inspect_archive,args.inspection_evidence);return
    os.chdir(ROOT)
    if not json.loads(r.docker('context','inspect'))[0]['Endpoints']['docker']['Host'].startswith(('npipe://','unix://')):
        raise RuntimeError('Local Docker required')
    r.run('git','merge-base','--is-ancestor',BASELINE,'HEAD')
    protected=['HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/Migrations/','HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/HappyCityDbContext.cs','scripts/verify-database-recovery.py','scripts/verify-source.py']
    if r.run('git','diff',BASELINE,'--',*protected).strip():raise RuntimeError('Protected accepted assets changed')
    immutable=['HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/IdentityMigrations/',
               'scripts/verify-identity-contract.sql','.github/workflows/build.yml']
    if r.run('git','diff','c5b61a362fd35522718401e558961e12d1b24bab','--',*immutable).strip():
        raise RuntimeError('R01 immutable migration/contract/workflow changed')
    networks=json.loads('['+','.join(r.docker('network','ls','--format','json').splitlines())+']')
    network=None
    for item in networks:
        if item['Name'].startswith('ha002f-'):
            detail=json.loads(r.docker('network','inspect',item['Name']))[0]
            labels=detail.get('Labels') or {}
            owned=labels.get('com.happycity.task') in ['ha002a-forward','ha003a-identity']
            empty_previous=not detail.get('Containers') and labels.get('com.docker.compose.project','').startswith('ha002f-')
            if owned or empty_previous:
                network=item['Name'];break
    if network is None:
        network='ha002f-identity-'+secrets.token_hex(4)
        r.docker('network','create','--label','com.happycity.task=ha003a-identity',network)
    os.environ['HA_FORWARD_NETWORK']=network
    directory=ROOT/'artifacts/identity'/('iam-'+dt.datetime.now(dt.timezone.utc).strftime('%Y%m%d%H%M%S')+'-'+secrets.token_hex(3))
    directory.mkdir(parents=True);r.protect(directory)
    evidence={'baseline':BASELINE,'submittedHead':r.run('git','rev-parse','HEAD').strip(),'checks':{},'targetContainers':[]}
    targets=[]
    def target(label, index):
        c,e,info=f.new_target(directory,label,args.port+index);targets.append(c);f.roles(c,e)
        local=directory/label;local.mkdir();r.private_write(local/'postgres.crt',r.docker('exec',c,'cat','/var/lib/postgresql-tls/server.crt'))
        e['certificateDirectory']=local;e['port']=args.port+index
        return c,e
    def conn(env,user,password):return f.connection(env['certificateDirectory'],env['port'],user,password)
    def identity_sql(c,e,sql):return f.execute(c,sql,'happy_identity_migrator',e['happy_identity_migrator'],'-c role=happy_identity_owner')
    def check(name):evidence['checks'][name]='PASS'
    try:
        baseline,benv=target('accepted',0)
        process=dict(os.environ,HAPPYCITY_MIGRATION_CONNECTION=conn(benv,'happy_schema_migrator',benv['MIGRATION_PASSWORD'])+';Options=-c role=happy_owner')
        r.run('dotnet','ef','migrations','script','0','--context','HappyCityDbContext','--project',PROJECT,'--configuration','Release','--no-build','-o',str(directory/'accepted.sql'),env=process)
        catalog_sql=(directory/'accepted.sql').read_text(encoding='utf-8-sig')
        f.execute(baseline,catalog_sql,'happy_schema_migrator',benv['MIGRATION_PASSWORD'],'-c role=happy_owner');f.grants(baseline)
        assert f.execute(baseline,'SELECT count(*) FROM discovery.discovery_entries')=='6'
        before=original_state(baseline)
        archive=dump(baseline,'happy_backup',benv['BACKUP_PASSWORD'],['public','catalog','discovery'])
        r.private_write(directory/'accepted.dump',archive)
        upgraded,uenv=target('upgrade',1);restore_catalog(upgraded,uenv,archive)
        assert original_state(upgraded)==before;check('protectedAcceptedBackupAndFreshRestoreBeforeIdentity')
        provision(upgraded,uenv)
        process['HAPPYCITY_IDENTITY_MIGRATION_CONNECTION']=conn(uenv,'happy_identity_migrator',uenv['happy_identity_migrator'])+';Options=-c role=happy_identity_owner'
        r.run('dotnet','ef','migrations','script','0','--context','HappyCityIdentityDbContext','--project',PROJECT,'--configuration','Release','--no-build','-o',str(directory/'identity.sql'),env=process)
        sql=(directory/'identity.sql').read_text(encoding='utf-8-sig')
        assert sql.count('START TRANSACTION;')==1 and sql.count('COMMIT;')==1
        identity_sql(upgraded,uenv,HISTORY)
        pre=state(upgraded)
        fault="""DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM identity."__EFMigrationsHistory") OR to_regclass('identity.user_tokens') IS NULL THEN RAISE EXCEPTION 'Failpoint prerequisite missing'; END IF; RAISE EXCEPTION 'Controlled identity failure after schema and history'; END $$;"""
        f.execute(upgraded,sql.replace('COMMIT;',fault+'\nCOMMIT;'),'happy_identity_migrator',uenv['happy_identity_migrator'],'-c role=happy_identity_owner',expected_state='P0001',expected_reason='Controlled identity failure after schema and history')
        assert state(upgraded)==pre;check('exactP0001FullIdentityRollback')
        recovered,renv=target('failure-recovery',2);restore_catalog(recovered,renv,archive)
        assert original_state(recovered)==before
        provision(recovered,renv);identity_sql(recovered,renv,HISTORY)
        assert state(recovered)==pre
        identity_sql(recovered,renv,sql);grant_identity(recovered)
        identity_sql(upgraded,uenv,sql);grant_identity(upgraded)
        assert state(recovered)==state(upgraded);check('independentFreshRecoveryAndReapplication')
        assert original_state(upgraded)==before;check('existingDataMetadataHistoryPrivilegesPreserved')
        clean,cenv=target('clean',3)
        f.execute(clean,catalog_sql,'happy_schema_migrator',cenv['MIGRATION_PASSWORD'],'-c role=happy_owner');f.grants(clean)
        clean_before=original_state(clean)
        provision(clean,cenv);identity_sql(clean,cenv,sql);grant_identity(clean)
        clean_after=state(clean);upgrade_after=state(upgraded)
        assert clean_after['original']==clean_before
        # Independently created catalog rows have legitimate creation timestamps.
        # Preserve each path's full original state; compare Identity state exactly.
        assert f.canonical(clean)==f.canonical(upgraded)
        assert {k:v for k,v in clean_after.items() if k!='original'}=={k:v for k,v in upgrade_after.items() if k!='original'}
        check('cleanAndUpgradeBothContextsEqual')
        reference,xenv=target('contract',4)
        # Historical definitions are protected against the accepted baseline;
        # Identity is constructed from the independently written literal contract.
        f.execute(reference,catalog_sql,'happy_schema_migrator',xenv['MIGRATION_PASSWORD'],'-c role=happy_owner');f.grants(reference)
        provision(reference,xenv)
        identity_sql(reference,xenv,HISTORY+(ROOT/'scripts/verify-identity-contract.sql').read_text())
        grant_identity(reference)
        verify_permissions(reference,xenv)
        expected_authority=authority_inventory(reference)
        evidence['r01AuthorityPolicySource']='Accepted immutable historical migration/grants + independent literal Identity contract + explicit approved Identity matrix; never target-derived.'
        evidence['r01AuthorityReference']=expected_authority
        evidence['r01AuthorityPaths']={name:verify_authority(instance,expected_authority)
            for name,instance in [('clean',clean),('upgrade',upgraded),('recovered',recovered)]}
        evidence['r01Mutations']=verify_mutations(upgraded,uenv,expected_authority)
        check('r01IndependentAuthorityMutationCleanup')
        expected=identity_metadata(reference)
        for instance in [upgraded,clean,recovered]:assert_contract(identity_metadata(instance),expected)
        assert len(expected['sequences'])==2 and len([x for x in expected['constraints'] if x['type']=='p'])==8 and len([x for x in expected['constraints'] if x['type']=='f'])==6
        assert len(expected['indexes'])==15
        check('independentPhysicalContractFourPaths')
        for kind in ['columns','constraints','indexes','ownership','sequences']:
            broken=copy.deepcopy(expected);broken[kind].pop()
            try:assert_contract(broken,expected)
            except RuntimeError as error:assert str(error).endswith(kind)
            else:raise RuntimeError('Independent contract negative control did not reject '+kind)
        check('independentContractMismatchControls')
        saved=state(upgraded)
        r.run('dotnet','ef','database','update','--context','HappyCityIdentityDbContext','--project',PROJECT,'--configuration','Release','--no-build',env=process)
        assert state(upgraded)==saved;check('completeIdentityNoOp')
        evidence['privileges']=verify_permissions(upgraded,uenv);check('explicitAclMembershipGrantOptionsAndExactDenials')
        # Deterministic synthetic fixture contains no usable credentials; framework-hash tests run separately.
        fixture="""INSERT INTO identity.users(id,user_name,normalized_user_name,email,normalized_email,email_confirmed,security_stamp,concurrency_stamp,phone_number_confirmed,two_factor_enabled,lockout_enabled,access_failed_count) VALUES ('53d96df5-475e-4b96-a1f6-f4b683b37d68','53d96df5475e4b96a1f6f4b683b37d68','53D96DF5475E4B96A1F6F4B683B37D68','recovery@example.test','RECOVERY@EXAMPLE.TEST',false,'synthetic','synthetic',false,false,false,0);
        INSERT INTO identity.roles VALUES ('638dce64-ff16-48ea-99c6-1cf24a6982aa','SyntheticRecovery','SYNTHETICRECOVERY','synthetic');
        INSERT INTO identity.user_roles VALUES ('53d96df5-475e-4b96-a1f6-f4b683b37d68','638dce64-ff16-48ea-99c6-1cf24a6982aa');
        INSERT INTO identity.user_claims(user_id,claim_type,claim_value) VALUES ('53d96df5-475e-4b96-a1f6-f4b683b37d68','synthetic','fixture');
        INSERT INTO identity.role_claims(role_id,claim_type,claim_value) VALUES ('638dce64-ff16-48ea-99c6-1cf24a6982aa','synthetic','fixture');
        INSERT INTO identity.user_logins VALUES ('Synthetic','fixture',NULL,'53d96df5-475e-4b96-a1f6-f4b683b37d68');
        INSERT INTO identity.user_tokens VALUES ('53d96df5-475e-4b96-a1f6-f4b683b37d68','Synthetic','fixture','not-a-credential');"""
        f.execute(upgraded,fixture,'happy_identity_app',uenv['happy_identity_app'])
        # All test actors are quiescent; compare the entire shared state before/between/after scoped dumps.
        full=state(upgraded)
        catalog_archive=dump(upgraded,'happy_backup',uenv['BACKUP_PASSWORD'],['public','catalog','discovery'])
        assert state(upgraded)==full
        identity_archive=dump(upgraded,'happy_identity_backup',uenv['happy_identity_backup'],['identity'])
        assert state(upgraded)==full
        r.private_write(directory/'catalog-coordinated.dump',catalog_archive);r.private_write(directory/'identity-sensitive.dump',identity_archive)
        restored,venv=target('coordinated-restore',5);restore_catalog(restored,venv,catalog_archive);provision(restored,venv)
        pwd=secrets.token_hex(32)
        f.execute(restored,f"CREATE ROLE happy_identity_restore LOGIN PASSWORD '{pwd}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS; GRANT happy_identity_owner TO happy_identity_restore; GRANT CONNECT ON DATABASE happyalmere TO happy_identity_restore;")
        assert f.execute(restored,'SELECT current_user','happy_identity_restore',pwd)=='happy_identity_restore'
        listing=r.run('docker','exec','-i',restored,'pg_restore','--list',input=identity_archive,binary=True).decode()
        # Bootstrap owns the schema already; no database-wide CREATE is granted to restore identities.
        filtered='\n'.join(x for x in listing.splitlines() if not re.search(r' SCHEMA - identity ',x))+'\n'
        r.docker('exec','-i',restored,'sh','-c','cat > /tmp/identity-restore.list',input=filtered)
        r.run('docker','exec','-i','-e','PGPASSWORD','-e','PGOPTIONS=-c role=happy_identity_owner',restored,'pg_restore','--dbname=host=localhost dbname=happyalmere user=happy_identity_restore sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt','--no-owner','--no-acl','--single-transaction','--exit-on-error','--use-list=/tmp/identity-restore.list',input=identity_archive,env=dict(os.environ,PGPASSWORD=pwd),binary=True)
        grant_identity(restored)
        f.execute(restored,'ALTER ROLE happy_identity_restore NOLOGIN; REVOKE happy_identity_owner FROM happy_identity_restore; REVOKE CONNECT ON DATABASE happyalmere FROM happy_identity_restore;')
        assert f.execute(restored,"""SELECT NOT rolcanlogin AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolreplication AND NOT rolbypassrls
            AND NOT EXISTS (SELECT 1 FROM pg_auth_members WHERE member=r.oid)
            AND NOT EXISTS (SELECT 1 FROM pg_roles target WHERE target.oid<>r.oid AND (pg_has_role(r.oid,target.oid,'MEMBER') OR pg_has_role(r.oid,target.oid,'SET')))
            AND NOT has_database_privilege(r.oid,'happyalmere','CONNECT,CREATE,TEMP')
            AND NOT EXISTS (SELECT 1 FROM pg_namespace n WHERE n.nspname IN ('identity','catalog','discovery') AND has_schema_privilege(r.oid,n.oid,'USAGE,CREATE'))
            AND NOT has_schema_privilege(r.oid,'public','CREATE,USAGE WITH GRANT OPTION')
            AND NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('identity','catalog','discovery','public') AND c.relkind IN ('r','p','v','m','f') AND (has_table_privilege(r.oid,c.oid,'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN') OR has_any_column_privilege(r.oid,c.oid,'SELECT,INSERT,UPDATE,REFERENCES')))
            AND NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('identity','catalog','discovery','public') AND CASE WHEN c.relkind='S' THEN has_sequence_privilege(r.oid,c.oid,'USAGE,SELECT,UPDATE') ELSE false END)
            AND NOT EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname IN ('identity','catalog','discovery','public') AND (p.proowner=r.oid OR has_function_privilege(r.oid,p.oid,'EXECUTE')))
            AND NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('identity','catalog','discovery','public') AND c.relowner=r.oid)
            FROM pg_roles r WHERE rolname='happy_identity_restore'""")=='t'
        assert state(restored)==full;check('coordinatedSharedStateBothHistoriesSequencesAndAclRestore')
        verify_permissions(restored,venv)
        evidence['r01AuthorityPaths']['restored']=verify_authority(restored,expected_authority)
        check('r01ComprehensiveAuthorityFourPaths')
        # libpq omits SQLSTATE for connection establishment failures. Match the
        # exact server reason here; IdentityPrivilegeTests verifies 28000 via Npgsql.
        denied=subprocess.run(['docker','exec','-e','PGPASSWORD',restored,'psql','-X',
            '--dbname=host=localhost dbname=happyalmere user=happy_identity_restore sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt',
            '-c','SELECT 1'],capture_output=True,text=True,env=dict(os.environ,PGPASSWORD=pwd))
        assert denied.returncode==2 and 'FATAL:  role "happy_identity_restore" is not permitted to log in' in denied.stderr
        for statement in ['SET ROLE happy_identity_owner','SELECT * FROM identity.users','SET ROLE happy_owner']:
            f.execute(restored,'SET SESSION AUTHORIZATION happy_identity_restore; '+statement,expected_state='42501')
        assert f.execute(restored,"SELECT count(*) FROM pg_stat_activity WHERE usename='happy_identity_restore'")=='0'
        check('retiredRestoreAuthenticationAndAuthorizationDenied')
        evidence['metadata']=expected
        evidence['backupSha256']={'catalog':hashlib.sha256(catalog_archive).hexdigest(),'identity':hashlib.sha256(identity_archive).hexdigest()}
        evidence['limitations']=['Synthetic quiescent coordinated logical backups; no production snapshot/RPO/RTO claim.','Private dumps/full snapshots/env are never uploaded.','No local/preserved source DB access.']
        runtime='IdentityFoundation__Enabled=true\nConnectionStrings__HappyCity='+conn(cenv,'happy_app',cenv['HAPPY_APP_PASSWORD'])+'\nConnectionStrings__HappyCityIdentity='+conn(cenv,'happy_identity_app',cenv['happy_identity_app'])+'\n'
        r.private_write(directory/'runtime.env',runtime)
        tests=runtime+'HAPPYCITY_MIGRATION_CONNECTION='+conn(cenv,'happy_schema_migrator',cenv['MIGRATION_PASSWORD'])+';Options=-c role=happy_owner\n'
        tests+='HAPPYCITY_IDENTITY_MIGRATION_CONNECTION='+conn(cenv,'happy_identity_migrator',cenv['happy_identity_migrator'])+';Options=-c role=happy_identity_owner\n'
        for name in ['happy_identity_backup','happy_backup']:
            password=cenv[name] if name.startswith('happy_identity') else cenv['BACKUP_PASSWORD']
            tests+='HAPPYCITY_TEST_'+name.upper()+'='+conn(cenv,name,password)+'\n'
        tests+='HAPPYCITY_IDENTITY_RETIRED_CONNECTION='+conn(venv,'happy_identity_restore',pwd)+'\nHAPPYCITY_IDENTITY_RESTORE_CONTROL='+conn(venv,'happy_identity_app',venv['happy_identity_app'])+'\n'
        # Preserve original retired-discovery test semantics on a separate task-owned empty-access identity.
        retired_password=secrets.token_hex(32)
        f.execute(clean,f"CREATE ROLE happy_restore NOLOGIN PASSWORD '{retired_password}';")
        tests+='HAPPYCITY_REVOKED_RESTORE_CONNECTION='+conn(cenv,'happy_restore',retired_password)+'\nHAPPYCITY_RESTORE_CONTROL_CONNECTION='+conn(cenv,'happy_app',cenv['HAPPY_APP_PASSWORD'])+'\n'
        tests+='HAPPYCITY_IDENTITY_EVIDENCE='+str(directory/'evidence.json')+'\n'
        r.private_write(directory/'tests.env',tests)
        evidence['targetContainers']=targets;evidence['status']='PASS'
        r.private_write(directory/'evidence.json',json.dumps(evidence,indent=2)+'\n')
        print(json.dumps({'status':'PASS','checks':evidence['checks'],'evidence':str((directory/'evidence.json').relative_to(ROOT)),'testEnvironment':str((directory/'tests.env').relative_to(ROOT))},indent=2))
    except Exception:
        for container in targets:r.docker('stop',container)
        raise


if __name__=='__main__':main()
