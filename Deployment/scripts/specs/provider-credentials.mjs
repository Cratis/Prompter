// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { chmodSync, existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const root = resolve(import.meta.dirname, '../../..');

for (const present of [true, false]) {
    test(`secondary field ${present ? 'present' : 'absent'} is never confused with the primary note`, () => {
        mkdirSync(resolve(root, '.ai-work'), { recursive: true });
        const fixture = mkdtempSync(resolve(root, '.ai-work/provider-credentials-'));
        const bin = resolve(fixture, 'bin');
        mkdirSync(bin);
        writeFileSync(resolve(fixture, 'config'), 'dummy-kubeconfig');
        const entries = [
            { name: 'Production: Studio Environment (~/.studio-deploy.env)', notes: 'export PULUMI_CONFIG_PASSPHRASE="dummy-passphrase"\nexport AWS_ACCESS_KEY_ID="dummy-access"\nexport AWS_SECRET_ACCESS_KEY="dummy-state"' },
            { name: 'Anthropic - Prompter - API Key', notes: 'dummy-primary', fields: present ? [{ name: 'woksin-org-api-token-prod', value: 'dummy-secondary' }] : [] },
            { name: 'Voyage AI - Cratis', fields: [{ name: 'prompter-production token', value: 'dummy-voyage' }] }
        ];
        writeFileSync(resolve(fixture, 'items.json'), JSON.stringify(entries));
        writeFileSync(resolve(bin, 'bw'), '#!/bin/sh\ncat "$FIXTURE/items.json"\n');
        writeFileSync(resolve(bin, 'gh'), '#!/bin/sh\n[ "$1" = secret ] && [ "$2" = set ] || exit 91\ncat > "$FIXTURE/$3"\n');
        for (const tool of ['bw', 'gh']) chmodSync(resolve(bin, tool), 0o700);
        const run = () => execFileSync('bash', [resolve(root, 'Deployment/scripts/set-github-secrets.sh')], {
            encoding: 'utf8',
            stdio: ['pipe', 'pipe', 'pipe'],
            env: { PATH: `${bin}:${process.env.PATH}`, HOME: fixture, BW_SESSION: 'dummy-session', PROMPTER_KUBE_DIR: fixture, FIXTURE: fixture }
        });
        if (!present) {
            assert.throws(run, error => error.status === 1 && error.stderr.includes("has no 'woksin-org-api-token-prod' field"));
            assert.equal(existsSync(resolve(fixture, 'ANTHROPIC_API_KEY')), false, 'preflight must fail before any writes');
            return;
        }
        const output = run();
        assert.equal(readFileSync(resolve(fixture, 'ANTHROPIC_API_KEY'), 'utf8'), 'dummy-primary');
        assert.equal(readFileSync(resolve(fixture, 'ANTHROPIC_SECONDARY_API_KEY'), 'utf8'), 'dummy-secondary');
        assert.equal(output.includes('dummy-'), false, 'script output must contain names only');
        assert.match(output, /set ANTHROPIC_SECONDARY_API_KEY \(environment production\)/);
    });
}
