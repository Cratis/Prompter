<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Retrieval policy readiness

## Configuration migration

`Answering:MinScore` / `Cratis__Prompter__Answering__MinScore` is retired and ignored, including
operator overrides. It remains bindable so a legacy default does not break startup. Replace it
with an explicitly reviewed `Cratis__Prompter__Answering__MinSemanticSimilarity` value. Do not
reinterpret or copy the old `0.02` RRF threshold as a cosine cutoff. This is a user-observable
policy migration, not an SDK compatibility promise.

The CLI/runtime and deployment defaults remain **0.5**. The public Pulumi key
`prompter-deployment:minSemanticSimilarity` explicitly sets production to **0.4**, a reversible,
unvalidated initial trial, not a calibrated threshold or a claim of answer quality. Deployment
projects it as `Cratis__Prompter__Answering__MinSemanticSimilarity` using invariant culture.
The cutoff must be finite and in `(0, 1]`; deployment and startup validation reject invalid values. RRF
continues to order candidates. Every answer-context passage must independently pass the cosine
cutoff and have finite similarity in `[-1, 1]`. No qualifying passages means refusal without chat.
The existing top-eight candidate limit, lexical query parsing, prompt trust boundary, and citation
rules remain unchanged. Cosine relevance is not a correctness probability or prompt-injection defense.

Verbose output labels `rrf` and `semantic similarity` independently. New successful answer and
interaction confidence values are the RRF-first admitted passage's cosine similarity. Refusal
confidence is the highest valid retrieved similarity (zero without valid candidates). Historical
interaction confidence remains RRF and must not be mixed with new values for calibration.

## Acceptance remains unverified

Two live retrieval observations motivated the bounded trial: the documentation positive's cosine
similarity was **0.437856**, and the deliberately unanswerable negative's was **0.354734**.
Public diagnostic candidates included `compare.md` (**0.438**), Chronicle integration documentation
(**0.426**), and CLI Chronicle documentation (**0.420**). These were relevant but only partially
grounding passages, not adequate proof that the question can be answered. The 0.4 trial makes the
positive eligible for retrieval; the model must still refuse if admitted passages do not support
the answer. These observations establish neither calibration nor answer quality. Revert the public
Pulumi setting to 0.5 to restore the default policy; application defaults need no change.

Offline fixtures verify implementation behavior only. Production recall and answer quality remain
unknown. Separately authorized bounded live acceptance must include both a known documentation
positive and a deliberately unanswerable negative, against the unchanged admitted budget/provider
path. The positive must produce a grounded answer with relevant citations; the negative must refuse
without a chat call. Do not activate or claim production readiness from synthetic or build results.
A failed positive is a stop for review, not permission to lower policy, switch accounts, change
providers, or repeat paid attempts. This change preserves existing admission, budget, provider, and
account-selection controls and does not authorize live calls or deployment.

A native macOS GSSAPI warning during database access is a separate diagnostic concern. It is not
evidence that this managed refusal came from GSSAPI, and this fix does not change native packages,
Docker packaging, credentials, or database authentication to address it.
