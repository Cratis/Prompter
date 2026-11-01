// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { appendFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export function due(event, runs, currentRunId, now = new Date()) {
    if (event !== 'schedule') return true;
    const today = now.toISOString().slice(0, 10);
    return !runs.some(run => String(run.id) !== String(currentRunId) &&
        new Date(run.created_at).toISOString().slice(0, 10) === today &&
        !['cancelled', 'skipped', 'startup_failure', 'stale'].includes(run.conclusion));
}

export async function check(environment, request = fetch) {
    if (environment.EVENT !== 'schedule') return true;
    try {
        const today = new Date().toISOString().slice(0, 10);
        const url = new URL(`${environment.GITHUB_API_URL || 'https://api.github.com'}/repos/${environment.GITHUB_REPOSITORY}/actions/workflows/reindex-production.yml/runs`);
        url.search = new URLSearchParams({ event: 'schedule', created: `>=${today}`, per_page: '20' });
        const response = await request(url, {
            headers: { Authorization: `Bearer ${environment.GH_TOKEN}`, Accept: 'application/vnd.github+json' },
            signal: AbortSignal.timeout(20000), redirect: 'error'
        });
        if (!response.ok) throw new Error('Run lookup failed');
        const body = await response.json();
        return due(environment.EVENT, body.workflow_runs, environment.GITHUB_RUN_ID);
    } catch {
        console.warn('::warning::Could not check scheduled runs; the database daily slot remains the cost guard.');
        return true;
    }
}

if (process.argv[1] && pathToFileURL(process.argv[1]).href === import.meta.url) {
    const run = await check(process.env);
    appendFileSync(process.env.GITHUB_OUTPUT, `run=${run}\n`);
    console.log(`Scheduled reindex due: ${run}`);
}
