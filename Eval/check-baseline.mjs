// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';

const ranges = { citationHitRate: [0, 1], refusalAccuracy: [0, 1], meanGroundedness: [1, 5] };
function requireValue(condition, message) {
    if (!condition) throw new Error(message);
}
function metric(value, min, max) {
    return typeof value === 'number' && Number.isFinite(value) && value >= min && value <= max;
}

function checkProvenance(provenance, baseline, synthetic) {
    requireValue(baseline.status === (synthetic ? 'synthetic' : 'measured'), 'Baseline is not measured (or explicitly synthetic).');
    requireValue(provenance, 'Missing report provenance.');
    requireValue(baseline.provenance, 'Missing baseline provenance.');
    requireValue(provenance.evidence === (synthetic ? 'synthetic' : 'measured'), 'Evidence mismatch.');
    requireValue(provenance.mode === (synthetic ? 'offline' : 'paid'), 'Mode mismatch.');
    for (const key of ['mode', 'evidence', 'fixtureHash', 'corpusHash', 'answerModel', 'judgeModel', 'embeddingModel', 'evaluator']) {
        checkIdentity(provenance[key], baseline.provenance[key], key);
    }
    requireValue(typeof provenance.sourceRevision === 'string', 'Missing source revision.');
    requireValue(provenance.sourceRevision.length > 0, 'Empty source revision.');
}

function checkIdentity(actual, expected, key) {
    requireValue(typeof actual === 'string', `Missing provenance: ${key}`);
    requireValue(actual.length > 0, `Empty provenance: ${key}`);
    requireValue(actual === expected, `Provenance mismatch: ${key}`);
}

function checkSamples(report) {
    const summary = report.summary;
    const questions = report.questions;
    requireValue(report.complete === true, 'Incomplete report.');
    requireValue(summary?.errors === 0, 'Report has errors.');
    requireValue(Array.isArray(questions), 'Missing samples.');
    requireValue(questions.length > 0, 'Empty samples.');
    requireValue(summary.questions === questions.length, 'Sample count mismatch.');
    requireValue(summary.inScope > 0, 'Missing in-scope samples.');
    requireValue(summary.outOfScope > 0, 'Missing out-of-scope samples.');
    requireValue(new Set(questions.map(q => q.id)).size === questions.length, 'Duplicate samples.');
    requireValue(questions.filter(q => !q.expectedRefusal).length === summary.inScope, 'In-scope count differs.');
    requireValue(questions.filter(q => q.expectedRefusal).length === summary.outOfScope, 'Out-of-scope count differs.');
    questions.forEach(checkSample);
}

function checkSample(question) {
    requireValue(typeof question.id === 'string', 'Missing sample id.');
    requireValue(question.id.length > 0, 'Empty sample id.');
    requireValue(question.error === null, 'Sample has errors.');
    requireValue(typeof question.expectedRefusal === 'boolean', 'Invalid expected refusal.');
    requireValue(typeof question.actualRefusal === 'boolean', 'Invalid actual refusal.');
    if (question.expectedRefusal) return;
    requireValue(typeof question.citationHit === 'boolean', 'Missing citation score.');
    if (!question.actualRefusal) requireValue(metric(question.groundedness, 1, 5), 'Missing/invalid judge score.');
}

export function compare(report, baseline, synthetic = false) {
    checkProvenance(report.provenance, baseline, synthetic);
    checkSamples(report);
    const summary = report.summary;
    requireValue(metric(baseline.tolerance, 0, 1), 'Invalid tolerance.');
    for (const [name, [min, max]] of Object.entries(ranges)) {
        requireValue(metric(summary[name], min, max), `Invalid report metric: ${name}`);
        requireValue(metric(baseline.metrics?.[name], min, max), `Invalid baseline metric: ${name}`);
        requireValue(summary[name] >= baseline.metrics[name] - baseline.tolerance, `Regression: ${name}`);
    }
    return report.questions.length;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    try {
        const { values } = parseArgs({ options: { report: { type: 'string' }, baseline: { type: 'string' }, synthetic: { type: 'boolean', default: false } } });
        const count = compare(JSON.parse(readFileSync(values.report, 'utf8')), JSON.parse(readFileSync(values.baseline, 'utf8')), values.synthetic);
        console.log(`Checked ${count} samples and 3 metrics (${values.synthetic ? 'synthetic only' : 'measured'}).`);
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
