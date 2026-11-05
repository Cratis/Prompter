---
id: PROMPTER-0013
title: The interaction log stores no personal data
status: accepted
stage: implemented
class: strategy
reversibility: costly
decided: 2026-07-16
decider: woksin
applies-to:
  - "Source/Storage/**"
  - "Source/**/InteractionLog*"
enforced-by: "not verified — the v1_2_0 migration exists in Source/Storage; a schema spec asserting the absence of content and identifier columns would verify it"
review-trigger: "Any proposal to store message content or a user identifier, including the question-text question in PROMPTER-0014."
---

Historical decision D-13, 2026-07-16. Amends [PROMPTER-0008](PROMPTER-0008-gdpr-posture.md).

## Decision

The interaction log keeps no personal data: no message content (question/answer text) and no user identifier, not even the keyed hash PROMPTER-0008 called for. A `v1_2_0` migration drops the `question`, `answer`, `user_hash`, and `answer_message_id` columns, leaving only anonymous operational signal per answer: `source`, `cited_pages`, `confidence`, `was_refusal`, and `feedback`.

Raw Discord user IDs are also removed from the operational logs. Rate limiting still tells users apart, but only via an in-memory key (the raw id) that is never written to disk or logs, so the whole `UserHash`/keyed-hash apparatus and its required key are gone.

## Grounds

- The log was write-only: no running code read the content back. Its value was entirely prospective (analytics and the post-v1 docs-gap flywheel).
- Data minimization is a GDPR principle. Storing nothing identifiable takes the log out of scope entirely: no lawful-basis, retention, or DSAR obligations attach to anonymous rows.
- The public privacy notice can make the strongest possible claim ("we keep no message content and nothing that identifies you"), verifiable in the open source.
- The retention purge stays as housekeeping (bounding table growth), not as a privacy control.

## Consequences

Accepted trade-off: the docs-gap flywheel needs question text, so it must re-introduce content behind its own decision record (consent notice and narrow retention) rather than assuming it is already collected. The `IInteractionLog` seam is unchanged, so that is an additive change, not a redesign. That record is [PROMPTER-0014](PROMPTER-0014-storing-question-text.md).
