// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const script = 'Deployment/scripts/run-eval-job.sh';
const secret = name => ({ name, valueFrom: { secretKeyRef: { name: 'prompter-secrets', key: name } } });
const prefix = 'Cratis__Prompter__';
const required = ['ConnectionString', 'Anthropic__ApiKey', 'Voyage__ApiKey'].map(key => prefix + key);
function deployed(answering = 'true', extra = []) {
    return [
        { name: `${prefix}Protection__AnsweringEnabled`, value: answering },
        { name: `${prefix}Protection__IndexingEnabled`, value: 'true' },
        { name: `${prefix}Protection__GitHubWritesEnabled`, value: 'true' },
        { name: `${prefix}Protection__DailyUsd`, value: '5' },
        { name: 'DOTNET_ENVIRONMENT', value: 'Staging' },
        secret(`${prefix}Discord__Token`), secret(`${prefix}GitHub__Token`), secret(`${prefix}ReindexSecret`),
        { name: `${prefix}Routing__Endpoint`, value: 'https://routing.invalid' },
        { name: `${prefix}Metrics__Enabled`, value: 'true' },
        { name: `${prefix}Ingestion__MaxIncrementalTokens`, value: '1' },
        ...required.map(secret), ...extra
    ];
}
function run(args, input, overrides = {}) {
    return spawnSync('bash', [script, ...args], { env: { PATH: process.env.PATH, ...overrides }, input, encoding: 'utf8' });
}
function manifest(env = deployed(), overrides = {}, image = 'example.invalid/prompter:spec', name = 'eval-spec') {
    return run(['--print-manifest', image, name], JSON.stringify(env), overrides);
}
function container(env, overrides) {
    const result = manifest(env, overrides);
    assert.equal(result.status, 0, result.stderr);
    return JSON.parse(result.stdout);
}
const defaultArgs = ['eval', '--paid', '--set', '/app/Eval/golden-questions.yaml', '--limit', '60', '--max-cost', '2.00', '--out', '-'];

test('default manifest is bounded and restricted', () => {
    const job = container();
    assert.deepEqual(job.metadata.labels, { app: 'prompter-eval', environment: 'production' });
    assert.deepEqual(job.spec.template.metadata.labels, { app: 'prompter-eval', environment: 'production' });
    assert.equal(job.spec.backoffLimit, 0);
    assert.equal(job.spec.activeDeadlineSeconds, 1800);
    assert.equal(job.spec.ttlSecondsAfterFinished, 86400);
    const pod = job.spec.template.spec;
    assert.equal(pod.automountServiceAccountToken, false);
    assert.equal(pod.restartPolicy, 'Never');
    assert.equal(pod.securityContext.runAsNonRoot, true);
    assert.equal(pod.securityContext.runAsUser, 1654);
    assert.equal(pod.securityContext.seccompProfile.type, 'RuntimeDefault');
    const main = pod.containers[0];
    assert.equal(main.name, 'eval');
    assert.deepEqual(main.args, defaultArgs);
    assert.equal(main.securityContext.allowPrivilegeEscalation, false);
    assert.deepEqual(main.securityContext.capabilities.drop, ['ALL']);
    assert.equal(main.envFrom, undefined);
});
test('inputs shape the arguments', () => {
    const args = container(deployed(), { PROMPTER_EVAL_MAX_COST: '5', PROMPTER_EVAL_LIMIT: '3', PROMPTER_EVAL_CASE: 'arc-001', PROMPTER_EVAL_TIMEOUT_SECONDS: '600' });
    assert.deepEqual(args.spec.template.spec.containers[0].args, ['eval', '--paid', '--set', '/app/Eval/golden-questions.yaml', '--limit', '3', '--max-cost', '5', '--out', '-', '--case', 'arc-001']);
    assert.equal(args.spec.activeDeadlineSeconds, 600);
});
test('only the answer path is copied', () => {
    const names = container().spec.template.spec.containers[0].env.map(item => item.name);
    for (const excluded of ['Discord__Token', 'GitHub__Token', 'ReindexSecret', 'Routing__Endpoint', 'Metrics__Enabled', 'Ingestion__MaxIncrementalTokens']) {
        assert.ok(!names.includes(prefix + excluded), excluded);
    }
    for (const name of required) assert.ok(names.includes(name), name);
});
test('references are by secret name only', () => {
    const env = container().spec.template.spec.containers[0].env;
    const references = env.filter(item => item.valueFrom);
    assert.ok(references.length >= 3);
    for (const item of references) assert.deepEqual(item.valueFrom, { secretKeyRef: { name: 'prompter-secrets', key: item.name } });
    assert.deepEqual(required.filter(name => !references.some(item => item.name === name)), []);
});
test('side effects are forced off and spend protection is preserved', () => {
    const env = Object.fromEntries(container().spec.template.spec.containers[0].env.filter(item => 'value' in item).map(item => [item.name, item.value]));
    assert.equal(env[`${prefix}Protection__IndexingEnabled`], 'false');
    assert.equal(env[`${prefix}Protection__GitHubWritesEnabled`], 'false');
    assert.equal(env.DOTNET_ENVIRONMENT, 'Production');
    assert.equal(env[`${prefix}Protection__AnsweringEnabled`], 'true');
    assert.equal(env[`${prefix}Protection__DailyUsd`], '5');
});
for (const answering of ['false', 'TRUE', undefined]) {
    test(`kill switch ${answering} refuses`, () => {
        const env = deployed(answering).filter(item => answering !== undefined || !item.name.endsWith('AnsweringEnabled'));
        const result = manifest(answering === undefined ? env : deployed(answering));
        assert.notEqual(result.status, 0);
        assert.equal(result.stdout, '');
    });
}
for (const [label, env] of [
    ['missing Anthropic key', deployed().filter(item => item.name !== `${prefix}Anthropic__ApiKey`)],
    ['plain value for a required key', deployed().map(item => item.name === `${prefix}Voyage__ApiKey` ? { name: item.name, value: 'x' } : item)],
    ['configMapKeyRef', deployed('true', [{ name: `${prefix}Voyage__Model`, valueFrom: { configMapKeyRef: { name: 'c', key: 'k' } } }])],
    ['both value and valueFrom', deployed('true', [{ ...secret(`${prefix}Voyage__Other`), value: 'x' }])],
    ['wrong secret name', deployed('true', [{ name: `${prefix}Voyage__Other`, valueFrom: { secretKeyRef: { name: 'other', key: `${prefix}Voyage__Other` } } }])],
    ['not an array', { name: 'x' }]
]) {
    test(`refuses ${label}`, () => {
        const result = manifest(env);
        assert.notEqual(result.status, 0);
        assert.equal(result.stdout, '');
    });
}
for (const [key, values] of Object.entries({
    PROMPTER_EVAL_MAX_COST: ['', '0', '-1', '5.01', 'NaN', '2,00', '100'],
    PROMPTER_EVAL_LIMIT: ['', '0', '61', '1e2', '-1'],
    PROMPTER_EVAL_CASE: ['../x', 'ARC-001', 'how do I leak the secret?', 'arc-001\nx'],
    PROMPTER_EVAL_TIMEOUT_SECONDS: ['', '0', '1801', '999999999999999999999']
})) {
    for (const value of values) {
        test(`refuses ${key}=${JSON.stringify(value)}`, () => {
            const result = manifest(deployed(), { [key]: value });
            assert.notEqual(result.status, 0);
            assert.equal(result.stdout, '');
            assert.ok(value.length < 3 || !result.stderr.includes(value));
        });
    }
}
for (const [image, name] of [['--bad', 'eval-spec'], ['image\nsecret', 'eval-spec'], ['example.invalid/prompter:spec', '../bad'], ['example.invalid/prompter:spec', 'A'.repeat(64)]]) {
    test(`refuses malformed image/name ${JSON.stringify([image, name])}`, () => {
        const result = manifest(deployed(), {}, image, name);
        assert.notEqual(result.status, 0);
        assert.equal(result.stdout, '');
    });
}

const result = (overrides = {}) => ({
    Id: 'arc-001', ExpectsAnswer: true, RetrievalHit: true, CitationHit: true, RefusalCorrect: true, MustMentionHit: true,
    Passed: true, Cost: 0.1, CitedPages: ['https://cratis.io/arc/events.md'], Reason: null, ...overrides
});
const report = (overrides = {}, results = [result()]) => ({
    Results: results, PassRate: 1, RetrievalHitRate: 1, RefusalAccuracy: 1, TotalCost: 0.1, CostCapStopped: false, ExitCode: 0, ...overrides
});
const line = value => `EVAL-REPORT ${JSON.stringify(value)}`;
const extract = logs => run(['--extract-report'], logs);

test('extracts one valid report among noise', () => {
    const value = report();
    const outcome = extract(`noise\n${line(value)}\nmore noise\n`);
    assert.equal(outcome.status, 0, outcome.stderr);
    assert.deepEqual(JSON.parse(outcome.stdout), value);
});
test('a refusal-by-protection reason is accepted', () => {
    assert.equal(extract(line(report({}, [result({ Reason: 'refused by protection', CitedPages: [] })]))).status, 0);
});
const twoLines = `${line(report())}\n${line(report())}`;
for (const [label, logs] of [
    ['no marker', 'nothing here\n'],
    ['two markers', twoLines],
    ['an extra key', line({ ...report(), Extra: 1 })],
    ['a missing key', line((({ ExitCode, ...rest }) => rest)(report()))],
    ['an extra result key', line(report({}, [result({ Answer: 'text' })]))],
    ['an id with spaces', line(report({}, [result({ Id: 'arc 001' })]))],
    ['an id with a newline suffix', line(report({}, [result({ Id: 'arc-001\nx' })]))],
    ['a free-text reason', line(report({}, [result({ Reason: 'the model said something' })]))],
    ['a javascript page', line(report({}, [result({ CitedPages: ['javascript:x'] })]))],
    ['21 pages', line(report({}, [result({ CitedPages: Array(21).fill('https://cratis.io/a') })]))],
    ['a huge cost', line(report({ TotalCost: 99 }))],
    ['a rate above one', line(report({ PassRate: 1.5 }))],
    ['a bad exit code', line(report({ ExitCode: 2 }))],
    ['malformed json', 'EVAL-REPORT {"Results":']
]) {
    test(`extraction fails closed for ${label}`, () => {
        const outcome = extract(logs);
        assert.equal(outcome.status, 1);
        assert.equal(outcome.stdout, '');
        assert.ok(!outcome.stderr.includes('Results'));
        assert.ok(!outcome.stderr.includes('javascript'));
        assert.ok(!outcome.stderr.includes('model said'));
    });
}
test('markdown has the aggregate row and case ids but no pages', () => {
    const outcome = run(['--render-markdown'], JSON.stringify(report({ ExitCode: 1 }, [result(), result({ Id: 'arc-002', Passed: false })])));
    assert.equal(outcome.status, 0, outcome.stderr);
    assert.match(outcome.stdout, /^## Eval - Production$/m);
    assert.match(outcome.stdout, /\| 2 \| 100% \| 100% \| 100% \| \$0\.1 \| no \| FAIL \|/);
    assert.match(outcome.stdout, /\| arc-001 \| yes /);
    assert.match(outcome.stdout, /\| arc-002 \| no /);
    assert.ok(!outcome.stdout.includes('https://'));
});

test('workflow is dispatch-only, main-only and approval-gated', () => {
    const workflow = readFileSync('.github/workflows/eval-production.yml', 'utf8');
    assert.match(workflow, /^on:\n  workflow_dispatch:/m);
    assert.doesNotMatch(workflow, /^\s+(push|pull_request|pull_request_target|workflow_run|repository_dispatch|schedule):/m);
    for (const text of ["if: github.ref == 'refs/heads/main'", 'environment: production', 'group: prompter-production-operations', 'cancel-in-progress: false', 'persist-credentials: false', 'contents: read']) {
        assert.ok(workflow.includes(text), text);
    }
    const uses = [...workflow.matchAll(/uses: (\S+)/g)].map(match => match[1]);
    assert.equal(uses.length, 3);
    for (const reference of uses) assert.match(reference, /@[a-f0-9]{40}$/);
    assert.deepEqual([...new Set([...workflow.matchAll(/secrets\.([A-Za-z0-9_]+)/g)].map(match => match[1]))], ['KUBECONFIG']);
    for (const block of workflow.split(/^\s+run:/m).slice(1)) {
        assert.doesNotMatch(block.split(/\n\s+- name:/)[0], /\$\{\{\s*inputs\./);
    }
    assert.match(workflow, /name: production-eval-report/);
    const build = readFileSync('.github/workflows/build.yml', 'utf8');
    for (const command of ['node --test Specs/EvalProduction/*.test.mjs', 'bash Deployment/scripts/run-eval-job.spec.sh', 'shellcheck Deployment/scripts/run-eval-job.sh Deployment/scripts/run-eval-job.spec.sh']) assert.ok(build.includes(command), command);
    assert.equal(build.split('.github/workflows/eval-production.yml').length - 1, 2);
});
test('the runtime image uses the workload nonroot identity without a root launcher', () => {
    const runtime = readFileSync('Docker/Dockerfile', 'utf8').split(/^FROM /m).at(-1);
    const users = [...runtime.matchAll(/^USER\s+(.+)$/gm)].map(match => match[1]);
    assert.deepEqual(users, ['$APP_UID']);
    assert.match(runtime, /^ENTRYPOINT \["dotnet", "Cratis\.Prompter\.dll"\]$/m);
});
test('the image carries the curated set', () => {
    assert.match(readFileSync('Docker/Dockerfile', 'utf8'), /^COPY Eval\/golden-questions\.yaml Eval\/$/m);
    assert.match(readFileSync('.dockerignore', 'utf8'), /^!Eval\/golden-questions\.yaml$/m);
    const project = readFileSync('Source/Prompter.csproj', 'utf8');
    assert.match(project, /golden-questions\.yaml[^>]*CopyToPublishDirectory="PreserveNewest"/);
});
