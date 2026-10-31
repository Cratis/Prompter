// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Render file provisioning JSON for Core's Grafana, whose Prometheus datasource UID is "prometheus".
// Notification policies/contact points remain operator-owned. Without --enable every rule is paused.
import { readFileSync } from 'node:fs';

if (process.argv.slice(2).some(argument => argument !== '--enable')) {
    console.error('Usage: node Monitoring/grafana-alerts.mjs [--enable]');
    process.exit(1);
}
const groups = JSON.parse(readFileSync(new URL('rules.json', import.meta.url))).groups;
const result = {
    apiVersion: 1,
    groups: groups.map(group => ({
        orgId: 1, name: group.name, folder: 'Prompter', interval: group.interval,
        rules: group.rules.map((rule, index) => ({
            uid: `prompter-${index + 1}`, title: rule.alert, condition: 'C',
            for: rule.for, labels: rule.labels, annotations: rule.annotations,
            noDataState: 'OK', execErrState: 'Alerting', isPaused: !process.argv.includes('--enable'),
            data: [
                { refId: 'A', relativeTimeRange: { from: 600, to: 0 }, datasourceUid: 'prometheus',
                    model: { refId: 'A', expr: rule.expr, instant: true, range: false, intervalMs: 1000, maxDataPoints: 43200 } },
                { refId: 'C', relativeTimeRange: { from: 0, to: 0 }, datasourceUid: '__expr__',
                    model: { refId: 'C', type: 'threshold', expression: 'A',
                        conditions: [{ type: 'query', query: { params: ['C'] }, reducer: { type: 'last', params: [] },
                            evaluator: { type: 'gt', params: [0] }, operator: { type: 'and' } }] } }
            ]
        }))
    }))
};
console.log(JSON.stringify(result, null, 2));
