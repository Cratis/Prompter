// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const script = 'Deployment/scripts/run-index-job.sh';
const settings = {
    PROMPTER_INDEX_MODEL: 'voyage-4',
    PROMPTER_INDEX_PRICE_PER_MILLION_TOKENS: '0.06',
    PROMPTER_INDEX_MAX_RUN_DOLLARS: '0.10',
    PROMPTER_INDEX_MAX_DAILY_DOLLARS: '0.50'
};
function manifest(overrides = {}, image = 'example.invalid/prompter:spec', name = 'index-spec') {
    return spawnSync('bash', [script, '--print-manifest', image, name], {
        env: { PATH: process.env.PATH, ...settings, ...overrides }, encoding: 'utf8'
    });
}
for (const flag of [undefined, '', 'false', 'TRUE', '1']) {
    test(`scheduled mode refuses gate ${flag}`, () => {
        const result = manifest({ PROMPTER_INDEX_MODE: 'scheduled', PROMPTER_SCHEDULED_INDEXING_ENABLED: flag });
        assert.notEqual(result.status, 0);
        assert.equal(result.stdout, '');
    });
}
test('authorized schedule has bounded configuration and restricted security', () => {
    const result = manifest({ PROMPTER_INDEX_MODE: 'scheduled', PROMPTER_SCHEDULED_INDEXING_ENABLED: 'true' });
    assert.equal(result.status, 0, result.stderr);
    const job = JSON.parse(result.stdout);
    assert.equal(job.spec.activeDeadlineSeconds, 1500);
    assert.equal(job.spec.backoffLimit, 0);
    assert.equal(job.spec.template.spec.automountServiceAccountToken, false);
    assert.deepEqual(job.metadata.labels, { app: 'prompter-index', environment: 'production' });
    const pod = job.spec.template.spec;
    assert.equal(pod.securityContext.runAsNonRoot, true);
    assert.equal(pod.securityContext.runAsUser, 1654);
    assert.equal(pod.securityContext.seccompProfile.type, 'RuntimeDefault');
    const container = pod.containers[0];
    assert.deepEqual(container.args, ['index', '--scheduled']);
    assert.equal(container.securityContext.allowPrivilegeEscalation, false);
    assert.deepEqual(container.securityContext.capabilities.drop, ['ALL']);
    assert.equal(container.envFrom, undefined);
    const values = Object.fromEntries(container.env.filter(item => 'value' in item).map(item => [item.name, item.value]));
    const expected = {
        Protection__AnsweringEnabled: 'false', Protection__GitHubWritesEnabled: 'false', Protection__IndexingEnabled: 'true',
        Protection__DailyUsd: '5', Protection__Voyage__DailyUsd: '5', Protection__Voyage__Model: 'voyage-4',
        Protection__Voyage__InputUsdPerMillion: '0.06', Protection__Voyage__OutputUsdPerMillion: '0',
        Protection__Voyage__MaxInputTokens: '32000', Voyage__Model: 'voyage-4', Voyage__Dimensions: '1024',
        Voyage__BatchSize: '128', Voyage__MaxRetries: '3', Voyage__IndexMaxRetries: '5',
        Voyage__RetryBaseDelayMilliseconds: '500', Voyage__TimeoutSeconds: '60', Voyage__IndexTimeoutSeconds: '120',
        Ingestion__MaxIncrementalTokens: '200000', Ingestion__PricePerMillionTokens: '0.06',
        Ingestion__MaxRunDollars: '0.10', Ingestion__MaxDailyDollars: '0.50'
    };
    for (const [key, value] of Object.entries(expected)) assert.equal(values[`Cratis__Prompter__${key}`], value, key);
    const references = container.env.filter(item => item.valueFrom);
    assert.deepEqual(references.map(item => item.name), ['Cratis__Prompter__ConnectionString', 'Cratis__Prompter__Voyage__ApiKey']);
    for (const item of references) assert.deepEqual(item.valueFrom.secretKeyRef, { name: 'prompter-secrets', key: item.name });
    const deployment = readFileSync('Deployment/Services/AnsweringReadinessEnvironment.cs', 'utf8');
    for (const key of Object.keys(expected).filter(key => /^(Protection__Daily|Protection__Voyage|Voyage__(Model|Dimensions))/.test(key))) {
        assert.ok(deployment.includes(`["${key}"] = "${expected[key]}"`), key);
    }
});
for (const key of Object.keys(settings)) {
    for (const value of ['', 'NaN', 'Infinity', '-1', '9999999999999999999999999999999999999999', '0', '0,06']) {
        test(`refuses invalid ${key}: ${value}`, () => {
            const result = manifest({ [key]: value });
            assert.notEqual(result.status, 0);
            assert.equal(result.stdout, '');
            assert.ok(!result.stderr.includes(value) || value.length < 3);
        });
    }
}
for (const overrides of [
    { PROMPTER_INDEX_MODEL: 'voyage-3' }, { PROMPTER_INDEX_PRICE_PER_MILLION_TOKENS: '0.5' },
    { PROMPTER_INDEX_MAX_RUN_DOLLARS: '0.51' }, { PROMPTER_INDEX_MAX_DAILY_DOLLARS: '5.01' },
    { PROMPTER_INDEX_TIMEOUT_SECONDS: '1501' }, { PROMPTER_INDEX_TIMEOUT_SECONDS: '999999999999999999999999' }
]) test(`refuses unsupported settings ${JSON.stringify(overrides)}`, () => assert.notEqual(manifest(overrides).status, 0));
for (const [image, name] of [['--bad', 'index-spec'], ['image\nsecret', 'index-spec'], ['example.invalid/prompter:spec', '../bad'], ['example.invalid/prompter:spec', 'A'.repeat(64)]]) {
    test(`refuses malformed image/name ${JSON.stringify([image, name])}`, () => {
        const result = manifest({}, image, name);
        assert.notEqual(result.status, 0);
        assert.equal(result.stdout, '');
    });
}
test('workflow gates schedules but preserves explicit manual invocations', () => {
    const workflow = readFileSync('.github/workflows/reindex-production.yml', 'utf8');
    assert.match(workflow, /if: github.ref == 'refs\/heads\/main' && \(github.event_name == 'workflow_dispatch' \|\| \(github.event_name == 'schedule' && vars.PROMPTER_SCHEDULED_INDEXING_ENABLED == 'true'\)\)/);
    assert.match(workflow, /PROMPTER_SCHEDULED_INDEXING_ENABLED: \$\{\{ vars.PROMPTER_SCHEDULED_INDEXING_ENABLED \}\}/);
    for (const text of ['environment: production', 'group: prompter-production-operations', 'persist-credentials: false', "github.event_name == 'schedule' && 'scheduled' || inputs.full && 'full' || 'incremental'"]) assert.ok(workflow.includes(text));
    assert.doesNotMatch(workflow, /^\s+(pull_request|repository_dispatch):/m);
    assert.equal((workflow.match(/uses: [^\n]+@[a-f0-9]{40}\b/g) || []).length, 2);
    const build = readFileSync('.github/workflows/build.yml', 'utf8');
    for (const command of ['node --test Specs/ScheduledIndexing/*.test.mjs', 'bash Deployment/scripts/run-index-job.spec.sh', 'shellcheck Deployment/scripts/run-index-job.sh Deployment/scripts/run-index-job.spec.sh']) assert.ok(build.includes(command));
});
