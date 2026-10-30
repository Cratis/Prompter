// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { compare } from '../check-baseline.mjs';

function fixture() {
    const provenance = { mode: 'offline', evidence: 'synthetic', fixtureHash: 'fixture', corpusHash: 'corpus', sourceRevision: 'fixture-revision', answerModel: 'canned', judgeModel: 'canned', embeddingModel: 'none', evaluator: 'fixture' };
    const metrics = { citationHitRate: 1, refusalAccuracy: 1, meanGroundedness: 4 };
    return {
        baseline: { status: 'synthetic', provenance, metrics, tolerance: 0 },
        report: { provenance, complete: true, summary: { ...metrics, errors: 0, questions: 2, inScope: 1, outOfScope: 1 }, questions: [
            { id: 'answer', expectedRefusal: false, actualRefusal: false, citationHit: true, groundedness: 4, error: null },
            { id: 'refusal', expectedRefusal: true, actualRefusal: true, error: null }
        ] }
    };
}

test('explicit synthetic comparison succeeds without promoting paid evidence', () => {
    const { report, baseline } = fixture();
    assert.equal(compare(report, baseline, true), 2);
    assert.throws(() => compare(report, baseline));
});

for (const invalid of [undefined, null, NaN, Infinity, -Infinity, -1, 6, '4']) {
    test(`invalid judge metric ${invalid} fails`, () => {
        const { report, baseline } = fixture();
        report.questions[0].groundedness = invalid;
        assert.throws(() => compare(report, baseline, true));
    });
}
for (const change of [
    f => { f.baseline.status = 'unmeasured'; },
    f => { f.report.complete = false; },
    f => { f.report.summary.errors = 1; },
    f => { f.report.questions[0].error = 'failed'; },
    f => { f.report.questions = []; },
    f => { f.report.summary.inScope = 0; },
    f => { f.report.summary.citationHitRate = 2; },
    f => { f.report.summary.meanGroundedness = NaN; },
    f => { delete f.report.summary.refusalAccuracy; },
    f => { f.report.provenance = { ...f.report.provenance, corpusHash: 'other' }; }
]) {
    test('rejects incomplete or mismatched evidence', () => {
        const f = fixture();
        change(f);
        assert.throws(() => compare(f.report, f.baseline, true));
    });
}
