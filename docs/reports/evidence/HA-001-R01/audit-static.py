import json, pathlib, re, subprocess, xml.etree.ElementTree as ET
root = pathlib.Path.cwd()
def git(*args):
    return subprocess.check_output(['git', '-C', str(root), *args], text=True, encoding='utf-8')
files = git('ls-files', '--cached', '--others', '--exclude-standard').splitlines()
text_extensions = {'.cs', '.csproj', '.cshtml', '.json', '.props', '.slnx', '.md', '.yml', '.css', '.cjs', '.py', '.log', '.txt'}
def is_text(filename):
    return pathlib.Path(filename).suffix in text_extensions or pathlib.Path(filename).name in {'.gitignore', '.gitattributes', '.editorconfig'}
graph = {}
for filename in files:
    if filename.endswith('.csproj'):
        project = ET.parse(root / filename).getroot()
        graph[pathlib.Path(filename).stem] = [
            pathlib.Path(r.attrib['Include']).stem for r in project.iter('ProjectReference')]
expected = {
    'HappyCity.Domain': [],
    'HappyCity.Application': ['HappyCity.Domain'],
    'HappyCity.Infrastructure': ['HappyCity.Application', 'HappyCity.Domain'],
    'HappyCity.Web': ['HappyCity.Application', 'HappyCity.Infrastructure'],
    'HappyCity.FoundationTests': ['HappyCity.Web']
}
assert graph == expected, graph
for filename in files:
    if filename.endswith('.json'):
        json.loads((root / filename).read_text(encoding='utf-8-sig'))
patterns = {
    'private_key': re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
    'github_token': re.compile(r'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b'),
    'aws_access_key': re.compile(r'\bAKIA[A-Z0-9]{16}\b'),
    'credential_assignment': re.compile(r"""(?i)(?:password|pwd|clientsecret|api[_-]?key|access[_-]?token)\s*[:=]\s*["'][^"']{6,}["']""")
}
findings = []
commits = git('rev-list', 'HEAD').splitlines()
blobs_scanned = 0
for commit in commits:
    for filename in git('ls-tree', '-r', '--name-only', commit).splitlines():
        if not is_text(filename): continue
        content = git('show', commit + ':' + filename)
        blobs_scanned += 1
        for kind, pattern in patterns.items():
            for match in pattern.finditer(content):
                findings.append({'commit': commit, 'file': filename, 'line':content[:match.start()].count('\n')+1, 'kind':kind})
for filename in files:
    if not is_text(filename): continue
    content = (root / filename).read_text(encoding='utf-8-sig')
    for kind, pattern in patterns.items():
        for match in pattern.finditer(content):
            findings.append({'file': filename, 'line': content[:match.start()].count('\n')+1, 'kind': kind})
required = ['AGENTS.md','CHANGELOG.md','SECURITY.md','README.md','docs/ARCHITECTURE.md','docs/DEVELOPMENT.md']
assert all((root/f).is_file() for f in required)
result = {
    'commit':git('rev-parse','HEAD').strip(),
    'currentAuthoredFiles':len(files), 'requiredDocumentation':required, 'projectReferences':graph,
    'dependencyDirection':'PASS', 'jsonParsing':'PASS',
    'credentialScan':{'commitCount':len(commits),'fileVersionsScanned':blobs_scanned,'findings':findings,
        'scope':'V0.1 reachable tracked history and current nonignored authored files; no ignored credentials opened or recorded'},
    'sourceReviewChecks':{
        name:[filename for filename in files if filename.endswith(('.cs','.cshtml')) and re.search(pattern,(root/filename).read_text(encoding='utf-8-sig'))]
        for name,pattern in {
            'rawHtml':r'Html\.Raw|HtmlString',
            'blockingAsync':r'\.Wait\(|\.Result\b|Task\.Run',
            'sqlOrDatabase':r'DbContext|SqlConnection|NpgsqlConnection',
            'outboundNetwork':r'new HttpClient|WebRequest'
        }.items()
    }
}
(root/'artifacts/r01/static-results.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
