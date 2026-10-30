# Conversation assistance and related issues

`/ask`, mentions/the configured ask channel, and help-forum replies keep the
existing admission, public-scope checks, paid-work reservation, and conversation
history limits. Only after admission and lease activation do they classify the
request, call the unchanged documentation answer pipeline, and optionally look up
related issues. CLI and webhook answering are unchanged.

The original answer, citations, refusal, interaction ID, feedback buttons, and
accounting source are preserved. Advisory links and an optional drafting offer
are sent in a separate message. Issue results are not citations or proof of a
fix: closed issues are labeled **recently closed**, not fixed. No issue titles,
bodies, author identities, or remote error text are shown or sent to the answer
model. Returned classification and issue data are never instructions or consent.

## Configuration and safe defaults

The host registers request routing using the `Cratis:Prompter:Routing` section
and product keys from `Cratis:Prompter:GitHub:Repositories`. An empty Laya endpoint
uses the deterministic product-matching fallback; no private hostname is assumed
and registration makes no network probe. See [Laya routing](reference/request-routing.md) for
the opt-in endpoint contract. A configured endpoint is not evidence that Laya is
live or evaluated.

Related search uses a separate GET-only interface and a redirect-free client
with the same GitHub App authentication and explicit installation repository
allowlist as issue operations. See [GitHub consent](github-consent.md) for App
configuration. Missing or incomplete credentials disable lookup and filing,
not documentation answering. No credential is required at startup. A legacy
token is never silently substituted for missing App configuration.

Each lookup:

- Maps a known product through the configured product-to-repository map, then
  checks the pinned installation allowlist. A model cannot choose a repository.
- Uses only sanitized first-line keywords from the current question (never
  conversation history). At most eight plain ASCII words of 2–24 letters are
  retained: at most 199 characters including spaces. User qualifiers, operators,
  mention syntax, URLs, and token-shaped strings are discarded. Existing
  secret-like text redaction is applied first. This is data minimization, not a
  guarantee of detecting every possible secret in prose.
- Performs one open-issue search and one search for issues closed in the last
  90 days, with `per_page=3`, no pagination and no retry. Both share one second
  of the admitted request deadline, including authentication. No issue/body
  endpoint is fetched and no raw body is retained.
- Accepts at most three records per bucket, six distinct records overall, open
  first. Responses are capped at 64 KiB/depth 16. Incomplete/malformed responses,
  pull requests, invalid numbers, stale/future closed timestamps, and records
  outside the exact repository are rejected. Links are constructed locally as
  canonical `https://github.com/<owner>/<repo>/issues/<number>` URLs.
- Fails soft for timeouts, authorization failures, malformed data, and API
  failures; caller cancellation still propagates. Nothing is persisted or logged
  from queries or results.

The existing explicit `/issue` preview's duplicate lookup uses the same bounded,
open-only helper. It displays neutral issue-number labels rather than remote
titles. It remains advisory and cannot file or comment.

## Drafting is not consent

Only a high-confidence, non-fallback `bug`, `feature`, or `idea` classification
with an offer action, enabled GitHub writes, configured credentials, and an
installation-approved mapped repository can produce a fixed invitation to use
`/issue`. Ordinary questions, uncertainty, unknown products, and disabled writes
never produce that offer. The invitation creates no preview and performs no
write. The user must invoke `/issue`, review its private immutable preview, and
explicitly confirm using the existing consent flow. The classifier never receives
a write-capable collaborator.

Offline executable specs use fake HTTP and classifier/answer collaborators;
they do not establish live provider or Discord behavior. The full train gate
remains a separate integration checkpoint.
