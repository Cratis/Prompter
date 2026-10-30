// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { execFileSync, spawnSync } from 'node:child_process';
import { test } from 'node:test';
import { notify } from '../../.github/scripts/notify-reindex-failure.mjs';

const root = new URL('../', import.meta.url);
const rules = JSON.parse(readFileSync(new URL('rules.json', root))).groups.flatMap(group => group.rules);

test('alerts cover missing telemetry, true pipeline failure, freshness, spend and provider protection', () => {
    for (const name of ['PrompterTelemetryMissing', 'PrompterIndexFailed', 'PrompterIndexStale', 'PrompterSpendExposure', 'PrompterIndexSpendExposure', 'PrompterProviderErrors', 'PrompterProviderRateLimited', 'PrompterProviderUnavailable', 'PrompterAdmissionPressure']) {
        assert.ok(rules.some(rule => rule.alert === name), name);
    }
    for (const rule of rules) {
        assert.ok(rule.expr.length > 20);
        assert.match(rule.annotations.runbook_url, /^https:\/\//);
        assert.equal(rule.labels.service, 'prompter');
        assert.ok(rule.for);
        assert.doesNotMatch(rule.expr, /user_id|channel_id|message|exception/);
    }
    assert.match(rules.find(rule => rule.alert === 'PrompterTelemetryMissing').expr, /absent.*or.*updated_at/);
    assert.match(rules.find(rule => rule.alert === 'PrompterIndexFailed').expr, /last_failure.*last_success/);
});

test('Grafana provisioning defaults paused and requires explicit activation', () => {
    const script = new URL('grafana-alerts.mjs', root);
    const paused = JSON.parse(execFileSync(process.execPath, [script.pathname], { encoding: 'utf8' }));
    assert.equal(paused.groups[0].rules.length, rules.length);
    assert.ok(paused.groups[0].rules.every(rule => rule.isPaused && rule.execErrState === 'Alerting'));
    const enabled = JSON.parse(execFileSync(process.execPath, [script.pathname, '--enable'], { encoding: 'utf8' }));
    assert.ok(enabled.groups[0].rules.every(rule => !rule.isPaused));
    assert.equal(spawnSync(process.execPath, [script.pathname, '--invalid']).status, 1);
});

test('dashboard is importable and contains only aggregate metric queries', () => {
    const dashboard = JSON.parse(readFileSync(new URL('dashboard.json', root)));
    assert.equal(dashboard.uid, 'prompter-operations');
    assert.ok(dashboard.panels.length >= 8);
    const queries = dashboard.panels.flatMap(panel => panel.targets.map(target => target.expr));
    assert.ok(queries.every(query => query.includes('prompter_')));
    assert.doesNotMatch(JSON.stringify(dashboard), /user_id|channel_id|message_text|error_message/);
});

test('unset failure destination is a clean no-op', () => {
    const result = spawnSync(process.execPath, [new URL('../.github/scripts/notify-reindex-failure.mjs', root).pathname], {
        env: { PATH: process.env.PATH }, encoding: 'utf8'
    });
    assert.equal(result.status, 0);
    assert.match(result.stdout, /disabled/);
});

test('configured failure notifier sends only aggregate status and reports delivery failures', async () => {
    const environment = { PROMPTER_ALERT_WEBHOOK: 'https://receiver.invalid/alerts', GITHUB_RUN_ID: '123', REINDEX_JOB_STATUS: 'failure' };
    const sent = [];
    const result = await notify(environment, async (destination, request) => {
        sent.push({ destination, request });
        return new Response(null, { status: 204 });
    });
    assert.equal(result, 0);
    assert.equal(sent.length, 1);
    assert.equal(sent[0].request.redirect, 'error');
    assert.deepEqual(JSON.parse(sent[0].request.body), { service: 'prompter', event: 'scheduled_reindex_failed',
        run_url: 'https://github.com/Cratis/Prompter/actions/runs/123', status: 'failure' });
    let attempts = 0;
    const failed = await notify(environment, async () => {
        attempts++;
        throw new Error('synthetic transport failure');
    });
    assert.equal(failed, 1);
    assert.equal(attempts, 3);
    assert.equal(await notify({ PROMPTER_ALERT_WEBHOOK: 'http://receiver.invalid' }, () => assert.fail('must not dispatch')), 1);
});
