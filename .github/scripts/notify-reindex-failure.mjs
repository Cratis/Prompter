// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pathToFileURL } from 'node:url';

// A dedicated HTTPS JSON receiver, not a Discord message webhook. Never log its URL or response.
export async function notify(environment, send = fetch) {
    const destination = environment.PROMPTER_ALERT_WEBHOOK;
    if (!destination) {
        console.log('Reindex failure notification disabled: configure PROMPTER_ALERT_WEBHOOK.');
        return 0;
    }
    try {
        if (new URL(destination).protocol !== 'https:') return 1;
    } catch {
        return 1;
    }
    const body = JSON.stringify({
        service: 'prompter', event: 'scheduled_reindex_failed',
        run_url: `${environment.GITHUB_SERVER_URL || 'https://github.com'}/${environment.GITHUB_REPOSITORY || 'Cratis/Prompter'}/actions/runs/${environment.GITHUB_RUN_ID || 'unknown'}`,
        status: environment.REINDEX_JOB_STATUS || 'failure'
    });
    for (let attempt = 0; attempt < 3; attempt++) {
        try {
            const response = await send(destination, {
                method: 'POST', headers: { 'Content-Type': 'application/json' }, body,
                signal: AbortSignal.timeout(20000), redirect: 'error'
            });
            await response.body?.cancel();
            if (response.ok) return 0;
        } catch {
            // Network details and receiver responses may contain credentials. Never forward them.
        }
    }
    return 1;
}

if (process.argv[1] && pathToFileURL(process.argv[1]).href === import.meta.url) {
    const result = await notify(process.env);
    if (result !== 0) console.error('Reindex failure notification delivery failed; inspect the HTTPS receiver configuration.');
    process.exitCode = result;
}
