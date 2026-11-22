"""CITY-R12 isolated recovery lifecycle, invoked only by the city verification tests.

Private artifacts never match the workflow upload patterns. Historical helpers are
imported selectively; their main routines, network selection and destinations are not used.
"""
import argparse
import datetime as dt
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import secrets
import signal
import socket
import subprocess
import sys
import time
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
BASE = '7cb61f839aa0a2940e7349fc81cd13e035906475'
TREE = '3f06c8c6f4cf682884a9a38804bd35fcd63882e7'
AUTHORITY = 'https://github.com/Shiva721254/HappyAlmere/issues/29#issuecomment-7045979709'
ARCHITECTURE = 'https://github.com/Shiva721254/HappyAlmere/issues/29#issuecomment-7045706043'
IMAGE = 'postgres:18-bookworm@sha256:1757c628698dd1ca67ff7a5fc386bfd91040afc62a45484c346c788b0cbe5402'


def load_identity():
    spec = importlib.util.spec_from_file_location('city_identity_reference', ROOT / 'scripts/verify-identity-foundation.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def workflow_number(name):
    value = os.getenv(name)
    if value is None:
        return None
    # Preserve the string/null evidence schema; accept positive ASCII signed-64-bit values only.
    require(re.fullmatch(r'[1-9][0-9]{0,18}', value) is not None,
            'Workflow provenance identifier rejected')
    require(int(value) <= 9223372036854775807, 'Workflow provenance identifier rejected')
    return value


def canonical_routines(value):
    # Same whitespace-only normalization as the protected forward contract verifier.
    for row in value or []:
        row['definition'] = re.sub(r"'(?:''|[^'])*'|\s+",
                                   lambda m: m[0] if m[0].startswith("'") else ' ',
                                   row['definition']).strip()
    return value


def capture_sql(forward, action):
    """Collect immutable helper query text, without contacting any database."""
    statements = []
    previous_execute, previous_run = forward.execute, subprocess.run
    def execute(container, statement, *args, **kwargs):
        statements.append(statement)
        return '[]'
    def run(*args, **kwargs):
        statements.append(kwargs['input'])
        return SimpleNamespace(returncode=0, stdout='{}')
    try:
        forward.execute, subprocess.run = execute, run
        shape = action()
        return statements, shape
    finally:
        forward.execute, subprocess.run = previous_execute, previous_run


def diagnostic_command(command):
    """Fixed identifiers only; never serialize argv, SQL, input or environment."""
    argv = command[0] if command and isinstance(command[0], (list, tuple)) else command
    if not argv:
        return 'unknown'
    program = Path(str(argv[0])).stem.lower()
    if program == 'docker':
        for prefix in [('context', 'inspect'), ('network', 'ls'), ('network', 'create'),
                       ('network', 'inspect'), ('volume', 'ls'), ('volume', 'inspect')]:
            if tuple(argv[1:3]) == prefix:
                return 'docker.' + '.'.join(prefix)
        if len(argv) > 1 and argv[1] in ('version', 'inspect', 'ps', 'stop', 'compose'):
            return 'docker.' + argv[1]
        if len(argv) > 1 and argv[1] == 'exec':
            return next(('docker.exec.' + tool for tool in ('psql', 'pg_dump', 'pg_restore') if tool in argv), 'docker.exec')
        return 'docker.other'
    return program if program in ('git', 'dotnet', 'icacls', 'whoami') else 'other'


def diagnostic_category(error):
    """Classify by trusted exception types only, never by text or class names."""
    for kind, category in ((KeyboardInterrupt, 'INTERRUPTED'),
                           (SystemExit, 'EXIT_REQUESTED'),
                           (subprocess.TimeoutExpired, 'PROCESS_TIMEOUT'),
                           (subprocess.CalledProcessError, 'PROCESS_ERROR'),
                           (PermissionError, 'PERMISSION_ERROR'),
                           (FileNotFoundError, 'FILE_NOT_FOUND'),
                           (OSError, 'OS_ERROR'),
                           (json.JSONDecodeError, 'JSON_DECODE_ERROR'),
                           (ValueError, 'VALUE_ERROR'),
                           (RuntimeError, 'RUNTIME_ERROR')):
        if isinstance(error, kind):
            return category
    return 'UNKNOWN_ERROR'


def diagnostic_elapsed(started):
    # Diagnostic failure must not replace a lifecycle exception.
    try:
        return max(0, min(180000, int((time.monotonic() - started) * 1000)))
    except BaseException:
        return None


def diagnostic_exit(value):
    return value if type(value) is int and -(2 ** 31) <= value < 2 ** 32 else None


APPLICATION_CHECKS = (
    'restoredResolverMetadata', 'oneResolverCommand', 'physicalPositiveAndSqlstateControls',
    'contextOverlap', 'sequentialIsolation', 'publicationIsolation', 'safeAlias',
    'invalidAliasInputs', 'foreignCursor', 'hostRejection', 'platformZeroCommands',
    'unsafeMethodNoRedirect', 'rateRejectionZeroCommands')
PARAMETER_TYPES = frozenset(('AnsiString', 'Binary', 'Byte', 'Boolean', 'Currency',
    'Date', 'DateTime', 'Decimal', 'Double', 'Guid', 'Int16', 'Int32', 'Int64',
    'Object', 'SByte', 'Single', 'String', 'Time', 'UInt16', 'UInt32', 'UInt64',
    'VarNumeric', 'AnsiStringFixedLength', 'StringFixedLength', 'Xml', 'DateTime2',
    'DateTimeOffset'))


def application_evidence(value):
    """Closed wire contract: no caller-controlled free text reaches artifacts."""
    def fields(item, expected):
        require(type(item) is dict and set(item) == set(expected), 'Application evidence shape rejected')
    fields(value, ('version', 'status', 'checks', 'commands', 'overlap'))
    require(type(value['version']) is int and value['version'] == 1, 'Application evidence version rejected')
    require(value['status'] == 'PASS', 'Restored application verification failed')
    require(type(value['checks']) is list and value['checks'] == list(APPLICATION_CHECKS),
            'Application verification checklist rejected')
    commands = value['commands']
    require(type(commands) is list and 1 <= len(commands) <= 2048, 'Application command count rejected')
    for command in commands:
        fields(command, ('Kind', 'ParameterTypes', 'ParameterCount', 'ParametersReferenced'))
        require(type(command['Kind']) is str and command['Kind'] in
                ('registeredResolver', 'localResolver', 'discovery'), 'Application command kind rejected')
        types = command['ParameterTypes']
        require(type(types) is list and 1 <= len(types) <= 32 and
                all(type(t) is str and t in PARAMETER_TYPES for t in types),
                'Application parameter types rejected')
        require(type(command['ParameterCount']) is int and command['ParameterCount'] == len(types),
                'Application parameter count rejected')
        require(command['ParametersReferenced'] is True, 'Application parameterization not verified')
    overlap = value['overlap']
    fields(overlap, ('participants', 'distinctScopes', 'distinctConnectionObjects', 'barrierCompleted'))
    require(type(overlap['participants']) is int and overlap['participants'] == 2 and
            all(overlap[k] is True for k in ('distinctScopes', 'distinctConnectionObjects', 'barrierCompleted')),
            'Application overlap evidence rejected')
    # Every leaf has been validated; never persist the input before this boundary.
    return value


def city_main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--mode', choices=['disabled', 'enabled'], required=True)
    args = parser.parse_args()
    os.chdir(ROOT)
    identity = load_identity()
    forward, recovery = identity.f, identity.r
    started = time.monotonic()
    deadline = started + 150
    original_run = subprocess.run
    stage = 'INITIALIZATION'
    last_operation = {}
    first_attempted_stage = 'INITIALIZATION'
    first_attempted_operation = None
    last_completed_stage = None

    def enter_stage(value):
        nonlocal stage, last_operation, first_attempted_stage
        # All callers supply code-defined literals or labels from fixed tuples.
        stage = value
        last_operation = {}
        if first_attempted_stage is None:
            first_attempted_stage = value

    def bounded_run(*command, **options):
        nonlocal last_operation, first_attempted_operation
        operation_started = time.monotonic()
        last_operation = {'stage': stage, 'commandIdentifier': 'unknown',
                          'exitCode': None, 'errorCategory': None,
                          'state': 'PENDING', 'invocationAttempted': False,
                          'subprocessCompleted': False, 'elapsedMilliseconds': None,
                          'diagnosticAvailable': False}
        try:
            last_operation['commandIdentifier'] = diagnostic_command(command)
        except BaseException:
            # No command, exception text, or stream is used as a fallback.
            pass
        if first_attempted_operation is None:
            first_attempted_operation = last_operation['commandIdentifier']
        remaining = deadline - time.monotonic()
        require(remaining > 0, 'City lifecycle time budget exceeded')
        options['timeout'] = min(options.get('timeout', 120), remaining)
        last_operation.update(state='ATTEMPTED', invocationAttempted=True)
        try:
            result = original_run(*command, **options)
        except BaseException as error:
            try:
                last_operation.update(
                    state='FAILED', errorCategory=diagnostic_category(error),
                    exitCode=diagnostic_exit(error.returncode) if isinstance(error, subprocess.CalledProcessError) else None,
                    subprocessCompleted=isinstance(error, subprocess.CalledProcessError),
                    elapsedMilliseconds=diagnostic_elapsed(operation_started))
            except BaseException:
                last_operation = {'state': 'INCOMPLETE', 'diagnosticAvailable': False}
            raise
        last_operation.update(exitCode=diagnostic_exit(result.returncode),
                              state='RETURNED', subprocessCompleted=True,
                              elapsedMilliseconds=diagnostic_elapsed(operation_started))
        # Nonzero returns may be expected negative controls: only the caller's
        # existing assertions decide whether the lifecycle failed.
        return result

    # All reused helpers share this standard-library module, including direct SQL calls.
    subprocess.run = bounded_run
    unique = os.environ.get('HAPPYCITY_CITY_RUN_ID', '')
    require(re.fullmatch(r'[0-9]{14}-[0-9a-f]{10}', unique) is not None,
            'Parent-owned City run identity required')
    public = ROOT / 'artifacts/forward' / ('ha006a-' + args.mode + '-' + unique)
    private = ROOT / 'artifacts/city-private' / (args.mode + '-' + unique)
    public.mkdir(parents=True)
    private.mkdir(parents=True)
    recovery.protect(private)
    evidence = {
        'schemaVersion': 1, 'task': 'HA-006-A', 'protocol': 'CITY-R12-RECOVERY',
        'status': 'INCOMPLETE', 'architecture': ARCHITECTURE, 'authority': AUTHORITY,
        'acceptedBase': BASE, 'acceptedTree': TREE, 'mode': args.mode,
        'checkout': recovery.run('git', 'rev-parse', 'HEAD').strip(),
        'tree': recovery.run('git', 'rev-parse', 'HEAD^{tree}').strip(),
        'run': workflow_number('GITHUB_RUN_ID'), 'attempt': workflow_number('GITHUB_RUN_ATTEMPT'),
        'startedUtc': utc(), 'targetContainers': [], 'resources': [], 'checks': {},
        'cleanupPending': True, 'cleanup': 'NOT STARTED',
        'diagnosticVersion': 2, 'cityEvidenceVersion': 3, 'applicationEvidenceVersion': 1,
        'checkoutMeaning': 'Git HEAD only; not an identity of uncommitted source',
        'progress': {'firstAttemptedStage': None, 'firstAttemptedOperation': None,
                     'lastVerifiedCompletedStage': None,
                     'readyEmitted': False, 'restoredStateVerified': False,
                     'restoredApplicationVerified': False,
                     'targetsDiscovered': 0, 'targetsOwnershipVerified': 0},
        'cleanupDiagnostic': {'state': 'PENDING', 'attemptedTargets': 0,
                              'verifiedStoppedTargets': 0, 'failureCategory': None},
    }

    def save():
        evidence['progress']['firstAttemptedStage'] = first_attempted_stage
        evidence['progress']['firstAttemptedOperation'] = first_attempted_operation
        evidence['progress']['lastVerifiedCompletedStage'] = last_completed_stage
        temporary = public / 'evidence.pending'
        recovery.private_write(temporary, json.dumps(evidence, indent=2) + '\n')
        os.replace(temporary, public / 'evidence.json')

    def check(name, references, details=None):
        nonlocal last_completed_stage
        # Called only after the existing gate assertions have succeeded.
        last_completed_stage = name
        evidence['checks'][name] = {'status': 'PASS', 'requirements': references, 'details': details}
        save()

    save()
    targets = []
    intentions = []
    evidence['resourceIntentions'] = intentions
    network = 'ha002f-city-' + args.mode + '-' + unique
    network_id = None
    original_environment = os.environ.get('HA_FORWARD_NETWORK')

    def ownership(resource):
        require(type(resource['container']) is str and re.fullmatch(r'[0-9a-f]{64}', resource['container']) is not None,
                'Container identifier rejected')
        info = json.loads(recovery.docker('inspect', resource['container']))[0]
        require(info['Id'] == resource['container'], 'Container identity changed')
        require(info['Config']['Labels'].get('com.docker.compose.project') == resource['project'], 'Project ownership mismatch')
        require(info['Config']['Image'] == IMAGE, 'Image mismatch')
        require(set(info['NetworkSettings']['Networks']) == {network}, 'Network ownership mismatch')
        expected_volumes = {resource['project'] + '_' + suffix for suffix in ('recovery-data', 'recovery-tls')}
        require(set(resource['volumes']) == expected_volumes, 'Unexpected intended volumes')
        require({m['Name'] for m in info['Mounts'] if m['Type'] == 'volume'} == expected_volumes, 'Volume ownership mismatch')
        require({(m['Name'], m['Destination']) for m in info['Mounts'] if m['Type'] == 'volume'} ==
                {(resource['project'] + '_recovery-data', '/var/lib/postgresql'),
                 (resource['project'] + '_recovery-tls', '/var/lib/postgresql-tls')},
                'Volume mount destination mismatch')
        require(info['Config']['Labels'].get('com.docker.compose.service') == 'postgres', 'Service ownership mismatch')
        bindings = info['HostConfig']['PortBindings']['5432/tcp']
        require(len(bindings) == 1 and bindings[0] == {'HostIp': '127.0.0.1', 'HostPort': str(resource['port'])}, 'Port boundary mismatch')
        detail = json.loads(recovery.docker('network', 'inspect', network_id))[0]
        require(detail['Id'] == network_id and detail['Labels'].get('com.happycity.task') == 'ha006a-' + unique, 'Network identity mismatch')
        require(info['NetworkSettings']['Networks'][network].get('NetworkID') == network_id, 'Container network identity mismatch')
        for volume in resource['volumes']:
            detail = json.loads(recovery.docker('volume', 'inspect', volume))[0]
            require(detail['Labels'].get('com.docker.compose.project') == resource['project'], 'Volume label mismatch')
            require(detail['Labels'].get('com.docker.compose.volume') == volume.removeprefix(resource['project'] + '_'),
                    'Volume role mismatch')


    def discover_intended(intention):
        # Discovery is one bounded exact-project query, never a historical prefix scan.
        # Intention alone is not authority to stop a resource.
        ids = recovery.docker('ps', '-aq', '--no-trunc', '--filter',
                              'label=com.docker.compose.project=' + intention['project']).split()
        require(len(ids) == 1 and re.fullmatch(r'[0-9a-f]{64}', ids[0]) is not None,
                'Partial target ownership unresolved')
        resource = {'container': ids[0], 'project': intention['project'],
                    'network': intention['network'], 'port': intention['port'],
                    'volumes': [intention['project'] + '_' + suffix
                                for suffix in ('recovery-data', 'recovery-tls')]}
        if intention['state'] == 'INTENDED':
            evidence['progress']['targetsDiscovered'] += 1
        intention['state'] = 'DISCOVERED'
        ownership(resource)
        evidence['progress']['targetsOwnershipVerified'] += 1
        intention.update(state='OWNERSHIP_VERIFIED', container=ids[0])
        if ids[0] not in evidence['targetContainers']:
            evidence['targetContainers'].append(ids[0])
            evidence['resources'].append(resource)
        return resource

    def target(label):
        enter_stage('OWNERSHIP.' + label + '.collision-check')
        project = 'ha002f-' + private.name + '-' + label
        require(not recovery.docker('ps', '-aq', '--filter', 'label=com.docker.compose.project=' + project).strip(), 'Project collision')
        for suffix in ['recovery-data', 'recovery-tls']:
            require(not recovery.docker('volume', 'ls', '-q', '--filter', 'name=^' + project + '_' + suffix + '$').strip(), 'Volume collision')
        with socket.socket() as reservation:
            reservation.bind(('127.0.0.1', 0))
            port = reservation.getsockname()[1]
        intention = {'role': label, 'project': project, 'network': network_id, 'port': port,
                     'state': 'INTENDED', 'container': None}
        intentions.append(intention)
        save()
        enter_stage('OWNERSHIP.' + label + '.create-target')
        container, env, info = forward.new_target(private, label, port)
        resource = {'container': info['Id'], 'project': project, 'network': network_id, 'port': port,
                    'volumes': sorted(m['Name'] for m in info['Mounts'] if m['Type'] == 'volume')}
        intention['state'] = 'DISCOVERED'
        evidence['progress']['targetsDiscovered'] += 1
        enter_stage('OWNERSHIP.' + label + '.validate-target')
        ownership(resource)
        targets.append(resource)
        intention.update(state='OWNERSHIP_VERIFIED', container=resource['container'])
        evidence['progress']['targetsOwnershipVerified'] += 1
        evidence['targetContainers'].append(resource['container'])
        evidence['resources'].append(resource)
        save()
        enter_stage('PROVISIONING.' + label + '.accepted-roles')
        forward.roles(container, env)
        local = private / label
        local.mkdir()
        recovery.private_write(local / 'postgres.crt', recovery.docker('exec', container, 'cat', '/var/lib/postgresql-tls/server.crt'))
        env['certificateDirectory'], env['port'] = local, port
        if args.mode == 'enabled':
            ownership(resource)
            identity.provision(container, env)
        return resource, env

    def sql(resource, statement, user='happy_migrator', credential=None, options=None, **negative):
        ownership(resource)
        return forward.execute(resource['container'], statement, user, credential, options, **negative)

    def connection(env, user, credential):
        return forward.connection(env['certificateDirectory'], env['port'], user, credential)

    try:
        enter_stage('OWNERSHIP.docker-context')
        host = json.loads(recovery.docker('context', 'inspect'))[0]['Endpoints']['docker']['Host']
        require(host.startswith(('npipe://', 'unix://')), 'Local Docker endpoint required')
        enter_stage('OWNERSHIP.docker-server')
        recovery.docker('version', '--format', '{{.Server.Version}}')
        enter_stage('OWNERSHIP.network-collision-check')
        require(not recovery.docker('network', 'ls', '-q', '--filter', 'name=^' + network + '$').strip(), 'Network collision')
        enter_stage('OWNERSHIP.network-create')
        evidence['networkIntention'] = {'state': 'INTENDED', 'task': 'ha006a-' + unique}
        save()
        network_id = recovery.docker('network', 'create', '--label', 'com.happycity.task=ha006a-' + unique, network).strip()
        require(re.fullmatch(r'[0-9a-f]{64}', network_id) is not None, 'Network identity unavailable')
        network_info = json.loads(recovery.docker('network', 'inspect', network_id))[0]
        require(network_info['Id'] == network_id and network_info['Name'] == network and
                network_info['Labels'].get('com.happycity.task') == 'ha006a-' + unique,
                'Created network ownership mismatch')
        evidence['networkIntention']['state'] = 'OWNERSHIP_VERIFIED_RETAINED'
        os.environ['HA_FORWARD_NETWORK'] = network
        source, source_env = target('source')
        restored, restored_env = target('restore')
        require(not set(source['volumes']) & set(restored['volumes']), 'Shared source/restore volumes')
        require(source['port'] != restored['port'], 'Shared source/restore port')
        check('OWNERSHIP', ['CITY-PF08', 'CITY-R12-AC02'], {'distinctResources': True})

        enter_stage('PROVISIONING.accepted-definition-scripts')
        project = 'HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure'
        process = dict(os.environ, HAPPYCITY_MIGRATION_CONNECTION=connection(source_env, 'happy_schema_migrator', source_env['MIGRATION_PASSWORD']) + ';Options=-c role=happy_owner')
        if args.mode == 'enabled':
            process['HAPPYCITY_IDENTITY_MIGRATION_CONNECTION'] = connection(source_env, 'happy_identity_migrator', source_env['happy_identity_migrator']) + ';Options=-c role=happy_identity_owner'
        for name, context, model in [('catalog', 'HappyCityDbContext', False), ('model', 'HappyCityDbContext', True)]:
            command = ['dotnet', 'ef', 'dbcontext', 'script'] if model else ['dotnet', 'ef', 'migrations', 'script', '0']
            recovery.run(*command, '--context', context, '--project', project, '--configuration', 'Release', '--no-build', '--output', str(private / (name + '.sql')), env=process)
        if args.mode == 'enabled':
            recovery.run('dotnet', 'ef', 'migrations', 'script', '0', '--context', 'HappyCityIdentityDbContext', '--project', project,
                         '--configuration', 'Release', '--no-build', '--output', str(private / 'identity.sql'), env=process)

        enter_stage('PROVISIONING.independent-reference')
        # Independent reference on the still-empty restoration instance. No committed
        # reference objects, reseeding of a restored database, or third instance.
        empty = sql(restored, "SELECT coalesce(json_agg(t ORDER BY nspname,relname),'[]') FROM (SELECT n.nspname,c.relname,c.relkind FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('public','catalog','discovery','identity')) t")
        require(empty == '[]', 'Restoration target not empty')
        history = 'CREATE TABLE public."__EFMigrationsHistory" ("MigrationId" varchar(150) NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,"ProductVersion" varchar(32) NOT NULL);'
        reference_script = 'BEGIN; SET LOCAL ROLE happy_owner;\n' + (private / 'model.sql').read_text(encoding='utf-8-sig')
        reference_script += '\n' + (ROOT / 'scripts/verify-catalog-contract.sql').read_text() + '\n' + history + '\nRESET ROLE;\n'
        grants, _ = capture_sql(forward, lambda: forward.grants('reference'))
        reference_script += '\n'.join(grants)
        if args.mode == 'enabled':
            reference_script += '\nSET LOCAL ROLE happy_identity_owner;\n' + identity.HISTORY + (ROOT / 'scripts/verify-identity-contract.sql').read_text() + '\nRESET ROLE;\n'
            grants, _ = capture_sql(forward, lambda: identity.grant_identity('reference'))
            reference_script += '\n'.join(grants) + '\nRESET ROLE;\n'
        groups = [('relational', lambda: forward.relational_metadata('reference')),
                  ('executable', lambda: forward.executable_metadata('reference')),
                  ('authority', lambda: identity.authority_inventory('reference'))]
        if args.mode == 'enabled':
            groups.append(('identity', lambda: identity.identity_metadata('reference')))
        collected = []
        for name, action in groups:
            queries, shape = capture_sql(forward, action)
            collected.append((name, list(shape) if name != 'authority' else None, len(queries)))
            reference_script += '\n' + ';\n'.join(q.rstrip().rstrip(';') for q in queries) + ';\n'
        reference_script += '\nROLLBACK;'
        decoder, records, raw_output, offset = json.JSONDecoder(), [], sql(restored, reference_script), 0
        while offset < len(raw_output):
            while offset < len(raw_output) and raw_output[offset].isspace():
                offset += 1
            if offset >= len(raw_output):
                break
            parsed, offset = decoder.raw_decode(raw_output, offset)
            records.append(parsed)
        reference = {}
        position = 0
        for name, keys, count in collected:
            values = records[position:position + count]
            require(len(values) == count, 'Independent reference output incomplete')
            reference[name] = values[0] if keys is None else dict(zip(keys, values, strict=True))
            position += count
        require(position == len(records), 'Unexpected reference output')
        canonical_routines(reference['executable']['functions'])
        canonical_routines(reference['authority']['routines'])
        require(sql(restored, "SELECT coalesce(json_agg(t ORDER BY nspname,relname),'[]') FROM (SELECT n.nspname,c.relname,c.relkind FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('public','catalog','discovery','identity')) t") == empty, 'Reference rollback left objects')
        if args.mode == 'enabled':
            # Full accepted reference was inspected from the retained, hash-verified
            # accepted artifact14630842132. Only routine whitespace is canonicalized.
            require(identity.authority_digest(reference['authority']) == '580de5221e3e12dc8fca86455cb7c1ccc84d3866c8c18164de451ba80c2d5674', 'Accepted full authority reference differs')
        evidence['independentReferenceProvenance'] = {
            'artifact': 14630842132, 'run': 44731429660, 'attempt': 2,
            'archiveSha256': '9b3b2394a6b2f3588b55db2a5aa8f7c6b7d31edc69b2c385ead3f95d0cebf1ef',
            'originalEnabledAuthoritySha256': '58ae3663078f168ceb4a2db93e2af582b9d008e86098090020cadf09cef16b58',
            'physicalSource': 'Protected EF model plus independent literal catalog and Identity SQL; transaction rolled back before restore',
            'disabledPolicy': 'Same accepted definitions without Identity activation; separate complete inventory, not enabled hash assumption'}

        enter_stage('PROVISIONING.accepted-replay')
        sql(source, (private / 'catalog.sql').read_text(encoding='utf-8-sig'), 'happy_schema_migrator', source_env['MIGRATION_PASSWORD'], '-c role=happy_owner')
        ownership(source); forward.grants(source['container'])
        if args.mode == 'enabled':
            sql(source, (private / 'identity.sql').read_text(encoding='utf-8-sig'), 'happy_identity_migrator', source_env['happy_identity_migrator'], '-c role=happy_identity_owner')
            ownership(source); identity.grant_identity(source['container'])

        def verify_reference(resource):
            ownership(resource)
            actual = {'relational': forward.relational_metadata(resource['container']), 'executable': forward.executable_metadata(resource['container'])}
            require(actual['relational'] == reference['relational'] and actual['executable'] == reference['executable'], 'Independent physical contract mismatch')
            actual_authority = identity.authority_inventory(resource['container'])
            canonical_routines(actual_authority['routines'])
            require(actual_authority == reference['authority'], 'Independent authority contract mismatch')
            if args.mode == 'enabled':
                identity.assert_contract(identity.identity_metadata(resource['container']), reference['identity'])
            return {'physicalSha256': recovery.digest(actual), 'authoritySha256': identity.authority_digest(actual_authority)}

        enter_stage('PROVISIONING.source-reference-comparison')
        evidence['sourceReferenceComparison'] = verify_reference(source)
        require(sql(source, 'SELECT count(*) FROM discovery.discovery_entries') == '6', 'Original source fixtures changed')
        check('PROVISIONING', ['CITY-A07', 'CITY-R12-AC03', 'CITY-R12-T03'], evidence['sourceReferenceComparison'])
        fixture = """
INSERT INTO catalog.cities(id,slug,name,country_code,default_language,time_zone,is_active) VALUES
('60000000-0000-4000-8000-000000000002','city-utrecht','Synthetic Utrecht','NL','nl','Europe/Amsterdam',true),
('60000000-0000-4000-8000-000000000003','city-inactive','Synthetic inactive','NL','nl','Europe/Amsterdam',false),
('60000000-0000-4000-8000-000000000004','city-no-primary','Synthetic no primary','NL','nl','Europe/Amsterdam',true);
INSERT INTO catalog.city_domains(id,city_id,hostname,is_primary,is_active) VALUES
('61000000-0000-4000-8000-000000000001','00000001-0000-8000-8000-000000000001','almere.test',true,true),
('61000000-0000-4000-8000-000000000002','60000000-0000-4000-8000-000000000002','utrecht.test',true,true),
('61000000-0000-4000-8000-000000000003','60000000-0000-4000-8000-000000000002','alias.almere.test',false,true),
('61000000-0000-4000-8000-000000000004','60000000-0000-4000-8000-000000000002','inactive.test',false,false),
('61000000-0000-4000-8000-000000000005','60000000-0000-4000-8000-000000000003','city-inactive.test',true,true),
('61000000-0000-4000-8000-000000000006','60000000-0000-4000-8000-000000000004','no-primary.test',false,true);
INSERT INTO discovery.discovery_entries(id,city_id,category_id,slug,title,description,district,icon,is_published)
SELECT ('62000000-0000-4000-8000-'||lpad(to_hex(n),12,'0'))::uuid,
CASE WHEN n<=102 THEN '00000001-0000-8000-8000-000000000001'::uuid ELSE '60000000-0000-4000-8000-000000000002'::uuid END,
'00000002-0000-8000-8000-000000000001','city-fixture-'||n,'CITY shared title',
CASE WHEN n<=102 THEN 'CITY-ALMERE-ONLY' ELSE 'CITY-UTRECHT-ONLY' END,'Synthetic','*',true FROM generate_series(1,204) n;
INSERT INTO discovery.discovery_entries(id,city_id,category_id,slug,title,description,district,icon,is_published)
VALUES('62000000-0000-4000-8000-000000000fff','60000000-0000-4000-8000-000000000002','00000002-0000-8000-8000-000000000001','city-unpublished','CITY never public','Synthetic','Synthetic','*',false);
"""
        enter_stage('RECOVERY.synthetic-fixtures')
        sql(source, fixture, 'happy_schema_migrator', source_env['MIGRATION_PASSWORD'], '-c role=happy_owner')
        if args.mode == 'enabled':
            # Deterministic synthetic account without a usable password or activation.
            sql(source, """INSERT INTO identity.users(id,user_name,normalized_user_name,email,normalized_email,email_confirmed,security_stamp,concurrency_stamp,phone_number_confirmed,two_factor_enabled,lockout_enabled,access_failed_count) VALUES ('63000000-0000-4000-8000-000000000001','city-synthetic','CITY-SYNTHETIC','city@example.test','CITY@EXAMPLE.TEST',false,'synthetic','synthetic',false,false,false,0);
INSERT INTO identity.user_claims(user_id,claim_type,claim_value) VALUES ('63000000-0000-4000-8000-000000000001','synthetic','city-recovery');""", 'happy_identity_app', source_env['happy_identity_app'])

        def state(resource):
            ownership(resource)
            return identity.state(resource['container']) if args.mode == 'enabled' else forward.complete_state(resource['container'])

        enter_stage('RECOVERY.restricted-backup')
        before = state(source)
        archives = {}
        for label, user, credential, schemas in [('catalog', 'happy_backup', source_env['BACKUP_PASSWORD'], ['public', 'catalog', 'discovery'])] + (
                [('identity', 'happy_identity_backup', source_env['happy_identity_backup'], ['identity'])] if args.mode == 'enabled' else []):
            require(sql(source, 'SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()', user, credential) == 't', 'Backup TLS missing')
            ownership(source)
            archive = identity.dump(source['container'], user, credential, schemas)
            recovery.private_write(private / (label + '.dump'), archive)
            archives[label] = archive
            require(state(source) == before, 'Source changed during scoped backup')
        evidence['archiveSha256'] = {name: hashlib.sha256(data).hexdigest() for name, data in archives.items()}
        evidence['sourceStateSha256'] = recovery.digest(before)
        evidence['fixtureCounts'] = {'originalEntries': 6, 'additionalPublished': 204, 'additionalUnpublished': 1, 'cities': 4}

        for label, archive in archives.items():
            enter_stage('RECOVERY.' + label + '.restricted-restore')
            role = 'happy_restore' if label == 'catalog' else 'happy_identity_restore'
            owner = 'happy_owner' if label == 'catalog' else 'happy_identity_owner'
            credential = secrets.token_hex(32)
            sql(restored, f"CREATE ROLE {role} LOGIN PASSWORD '{credential}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS; GRANT {owner} TO {role}; GRANT CONNECT ON DATABASE happyalmere TO {role};")
            require(sql(restored, 'SELECT current_user', role, credential) == role, 'Restore identity positive control failed')
            listing = recovery.run('docker', 'exec', '-i', restored['container'], 'pg_restore', '--list', input=archive, binary=True).decode()
            omit = r' (?:SCHEMA - public|COMMENT - SCHEMA public) ' if label == 'catalog' else r' SCHEMA - identity '
            listing = '\n'.join(line for line in listing.splitlines() if not re.search(omit, line)) + '\n'
            ownership(restored)
            recovery.docker('exec', '-i', restored['container'], 'sh', '-c', 'cat > /tmp/city-restore.list', input=listing)
            recovery.run('docker', 'exec', '-i', '-e', 'PGPASSWORD', '-e', 'PGOPTIONS=-c role=' + owner, restored['container'], 'pg_restore',
                         '--dbname=host=localhost dbname=happyalmere user=' + role + ' sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt',
                         '--no-owner', '--no-acl', '--single-transaction', '--exit-on-error', '--use-list=/tmp/city-restore.list', input=archive, binary=True, env=dict(os.environ, PGPASSWORD=credential))
            ownership(restored)
            (forward.grants if label == 'catalog' else identity.grant_identity)(restored['container'])
            sql(restored, f'ALTER ROLE {role} NOLOGIN; REVOKE {owner} FROM {role}; REVOKE CONNECT ON DATABASE happyalmere FROM {role};')
            require(sql(restored, f"SELECT NOT rolcanlogin AND NOT has_database_privilege(oid,'happyalmere','CONNECT,CREATE,TEMP') AND NOT EXISTS(SELECT 1 FROM pg_auth_members WHERE member=pg_roles.oid) FROM pg_roles WHERE rolname='{role}'") == 't', 'Restore authority not retired')
            denied = subprocess.run(['docker', 'exec', '-e', 'PGPASSWORD', restored['container'], 'psql', '-X',
                                     '--dbname=host=localhost dbname=happyalmere user=' + role + ' sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt', '-c', 'SELECT 1'],
                                    capture_output=True, text=True, env=dict(os.environ, PGPASSWORD=credential))
            require(denied.returncode == 2 and 'role "' + role + '" is not permitted to log in' in denied.stderr, 'Retired login negative control failed')
            sql(restored, f'SET SESSION AUTHORIZATION {role}; SET ROLE {owner}', expected_state='42501')
        enter_stage('RECOVERY.restored-state-comparison')
        after = state(restored)
        require(after == before, 'Complete restored state mismatch')
        evidence['progress']['restoredStateVerified'] = True
        evidence['restoredStateSha256'] = recovery.digest(after)
        check('RECOVERY', ['CITY-R12-AC04', 'CITY-R11-AC04', 'CITY-SEC25', 'CITY-R12-T05', 'CITY-R12-T06', 'CITY-R12-T07'])
        enter_stage('AUTHORITY.restored-reference-comparison')
        evidence['restoredReferenceComparison'] = verify_reference(restored)
        require(sql(restored, 'SELECT current_user', 'happy_app', restored_env['HAPPY_APP_PASSWORD']) == 'happy_app', 'Runtime positive control failed')
        for statement in ['DELETE FROM discovery.discovery_entries WHERE false', 'SET ROLE happy_owner', 'CREATE ROLE city_forbidden']:
            sql(restored, statement, 'happy_app', restored_env['HAPPY_APP_PASSWORD'], expected_state='42501')
        check('AUTHORITY', ['CITY-R12-AC04', 'CITY-R12-R03'], evidence['restoredReferenceComparison'])
        enter_stage('APPLICATION.private-configuration')
        runtime = {'catalog': connection(restored_env, 'happy_app', restored_env['HAPPY_APP_PASSWORD']),
                   'migration': connection(restored_env, 'happy_schema_migrator', restored_env['MIGRATION_PASSWORD']) + ';Options=-c role=happy_owner',
                   'identity': connection(restored_env, 'happy_identity_app', restored_env['happy_identity_app']) if args.mode == 'enabled' else None}
        config = private / 'city-runtime.private.env'
        recovery.private_write(config, json.dumps(runtime))
        save()
        print(json.dumps({'status': 'READY', 'protocolVersion': 3, 'runId': unique, 'mode': args.mode,
                          'configuration': str(config), 'evidence': str(public / 'evidence.json')}), flush=True)
        evidence['progress']['readyEmitted'] = True
        # Parent supplies only sanitized assertion outcomes. No credentials or searches.
        enter_stage('APPLICATION.restored-tests')
        application_line = sys.stdin.readline(1048577)
        require(len(application_line) <= 1048576 and application_line.endswith('\n'),
                'Application evidence size rejected')
        def unique_fields(pairs):
            value = {}
            for key, item in pairs:
                require(key not in value, 'Duplicate application evidence key')
                value[key] = item
            return value
        application = json.loads(application_line, object_pairs_hook=unique_fields)
        evidence['application'] = application_evidence(application)
        require(state(restored) == after and state(source) == before, 'Application fixture cleanup changed source/restored state')
        evidence['progress']['restoredApplicationVerified'] = True
        check('APPLICATION', ['CITY-A06', 'CITY-R12-AC05', 'CITY-R12-T08'])
        evidence['status'] = 'PASS'
    except BaseException as error:
        evidence['status'] = 'FAIL'
        evidence['failureType'] = 'UNKNOWN_ERROR'
        evidence['failureDiagnostic'] = {'stage': stage, 'operationId': stage,
                                         'errorCategory': 'UNKNOWN_ERROR',
                                         'lastSubprocess': {}}
        # Only fixed categories and the allowlisted process record are published.
        # A failed evidence write must not replace the original exception.
        try:
            evidence['failureType'] = diagnostic_category(error)
            evidence['failureDiagnostic'] = {
                'stage': stage, 'operationId': stage,
                'errorCategory': diagnostic_category(error),
                'lastSubprocess': dict(last_operation)}
            save()
        except BaseException:
            pass
        raise
    finally:
        original_failure = sys.exc_info()[1]
        enter_stage('CLEANUP.owned-targets')
        # Cleanup has a separate bounded allowance; preserve containers/volumes for fallback.
        deadline = time.monotonic() + 30
        clean = True
        for intention in intentions:
            enter_stage('CLEANUP.owned-targets')
            evidence['cleanupDiagnostic']['state'] = 'ATTEMPTED'
            evidence['cleanupDiagnostic']['attemptedTargets'] += 1
            try:
                resource = next((item for item in targets if item['project'] == intention['project']), None)
                if resource is None:
                    resource = discover_intended(intention)
                ownership(resource)
                intention['state'] = 'STOP_ATTEMPTED'
                recovery.docker('stop', '--time', '5', resource['container'])
                require(not json.loads(recovery.docker('inspect', resource['container']))[0]['State']['Running'], 'Owned target still running')
                intention['state'] = 'CONFIRMED_STOPPED'
                evidence['cleanupDiagnostic']['verifiedStoppedTargets'] += 1
            except BaseException as error:
                intention['state'] = 'UNKNOWN' if intention['container'] is None else 'STILL_PENDING'
                # Do not offer a resource with unresolved current checks to broad CI fallback.
                evidence['targetContainers'] = [item for item in evidence['targetContainers']
                                                if item != intention['container']]
                clean = False
                if evidence['cleanupDiagnostic']['failureCategory'] is None:
                    evidence['cleanupDiagnostic']['failureCategory'] = diagnostic_category(error)
                if 'failureDiagnostic' not in evidence:
                    evidence['failureType'] = diagnostic_category(error)
                    evidence['failureDiagnostic'] = {
                        'stage': stage, 'operationId': stage,
                        'errorCategory': diagnostic_category(error),
                        'lastSubprocess': dict(last_operation)}
        clean = clean and bool(intentions)
        evidence['cleanupPending'] = not clean
        evidence['cleanup'] = ('PASS' if clean else 'FAIL') if intentions else 'NOT EXERCISED'
        evidence['cleanupDiagnostic']['state'] = (
            ('VERIFIED_STOPPED' if clean else 'FAIL') if intentions else 'NO_REGISTERED_TARGETS')
        # Empty registration does not prove that a partially created resource is absent.
        evidence['completedUtc'] = utc()
        evidence['elapsedSeconds'] = round(time.monotonic() - started, 3)
        if not clean:
            evidence['status'] = 'FAIL'
        try:
            save()
        except BaseException:
            # No PASS has been persisted before this final write. Preserve a
            # pending lifecycle exception; otherwise propagate the write failure.
            if original_failure is None:
                raise
        finally:
            if original_environment is None:
                os.environ.pop('HA_FORWARD_NETWORK', None)
            else:
                os.environ['HA_FORWARD_NETWORK'] = original_environment
            subprocess.run = original_run

    if not clean:
        raise RuntimeError('City owned-resource cleanup incomplete')


def main():
    # Covers initialization failures before city_main's evidence/cleanup boundary.
    original_run = subprocess.run
    original_environment = os.environ.get('HA_FORWARD_NETWORK')
    try:
        city_main()
    finally:
        subprocess.run = original_run
        if original_environment is None:
            os.environ.pop('HA_FORWARD_NETWORK', None)
        else:
            os.environ['HA_FORWARD_NETWORK'] = original_environment


if __name__ == '__main__':
    try:
        main()
    except BaseException:
        print('CITY verification failed; see sanitized evidence and protected local diagnostics.', file=sys.stderr)
        sys.exit(1)
