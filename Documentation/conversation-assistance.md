# Conversation assistance and related issues

For an admitted Discord request through `/ask`, a mention, a configured ask channel or a help forum,
Prompter can provide an answer and separate advice about related issues. The advice helps you find an
existing discussion; it does not replace checking the answer against its cited sources. CLI and webhook
answers do not use this Discord assistance wrapper.

Use the [asking guide](guides/using-prompter.md) to choose an enabled server channel and continue a conversation.
Remove credentials and personal details before posting. Bounded earlier messages from the same public
conversation can be used as context; asking the bot does not make that conversation private.

If Prompter asks you to clarify the product or task, supply the missing detail. If it refuses, try a more
specific question or ask the community; a lack of grounding does not establish that the feature is unavailable.
A missing issue hint is not proof that no issue exists: product routing, configuration, limits and service
availability can all affect whether advice appears.

When related-issue advice arrives, open a linked issue and compare its problem and version with yours.
A **Recently closed** label means the issue was closed, not that a fix works in your project. Advice lists
validated issue numbers and links, not remote issue titles, bodies, author identities or error text.
Lookup follows answer generation, so those issue results are not fed back as evidence to the answer model.
Classification and issue data are advisory, not instructions or filing consent.

If you need a person, ask the community directly or use **Ask a human** when it is offered to the asker.
That requests help in the same channel. **This should be documented** and **Request this**, when available,
let the asker forward a sanitized question excerpt and link to a configured maintainer channel. These
explicit actions are separate from related-issue advice and feedback; none guarantees a human response.

## Configuration and safe defaults

The host registers routing from `Cratis:Prompter:Routing` and product keys from
`Cratis:Prompter:GitHub:Repositories`. An empty Laya endpoint uses local deterministic product matching;
it does not require a classifier service. With an approved private endpoint configured, classification
can send the sanitized current request and bounded older user-request excerpts to that service.
See [Laya routing](reference/request-routing.md) for the endpoint contract. Registration or a configured
endpoint does not establish that the service is reachable, authentic or evaluated.

After the answer, issue lookup requires a configured product route; an answer asking for product
clarification gets no issue advice. Prompter first tries the authenticated mapped search. It needs the
configured repository authorization and App authentication, or an explicitly approved legacy-token fallback.
Unusable authorization prevents that lookup and filing, not documentation answering. A legacy token is
never silently substituted for missing App configuration.

For the authenticated mapped lookup:

- A known product maps through the configured product-to-repository map, then the installation allowlist
  is checked. A model cannot choose a repository.
- Only sanitized first-line keywords from the current question are used, never conversation history.
  At most eight plain ASCII words of 2–24 letters are retained: at most 199 characters including spaces.
  Keyword selection excludes qualifiers, operators, mention syntax and URLs; recognized secret-like text
  is redacted first. This does not guarantee detection of every secret or sensitive detail in prose.
- One open-issue search and one search for issues closed in the last 90 days use `per_page=3`, no pagination
  and no retry. Both share one second of the admitted request deadline, including authentication.
  No issue/body endpoint is fetched; only validated metadata becomes a hint.
- At most three records per bucket and six distinct records overall are accepted, open first. Responses
  are capped at 64 KiB/depth 16. Incomplete/malformed responses, pull requests, invalid numbers,
  stale/future closed timestamps and records outside the exact repository are rejected. Links are
  constructed locally as canonical `https://github.com/<owner>/<repo>/issues/<number>` URLs.
- Handled timeouts, authorization failures, malformed data and API failures return no hints; caller
  cancellation still propagates. This mapped helper has no query/result cache.

If the mapped lookup returns no hints, the separate public lookup can search a fixed set of public Cratis
repositories without App credentials. It is controlled by `Cratis:Prompter:GitHub:PublicRelatedIssuesEnabled`,
which defaults to true in code. It can make two GET searches under a combined two-second deadline with
`per_page=6`, returning at most three links. These are different limits from the mapped helper above.

The public lookup keeps derived keywords and validated hint records in a bounded in-memory cache,
reusable for 15 minutes. That cache is separate from the temporary question/context passed to classification
and answering. Neither its expiry nor a helper's lack of a cache establishes external-service retention
or what every operator log contains. Disable the public lookup explicitly if that request path is not wanted.

The explicit `/issue` preview's duplicate lookup uses the mapped helper's open-only path. It displays
neutral issue-number labels rather than remote titles. That lookup remains advisory and cannot file or comment.

## Drafting is not consent

Only a high-confidence, non-fallback `bug`, `feature`, or `idea` classification with an offer action,
enabled GitHub writes, configured authorization and an installation-approved mapped repository can
produce a fixed invitation to use `/issue`. Ordinary questions, uncertainty, unknown products and disabled
writes do not produce that offer. The classifier has no write-capable collaborator.

The invitation creates no preview and performs no write. Before invoking `/issue`, remove personal
information from what you intend to share. Then review the private immutable preview and explicitly confirm
public filing. If the preview needs a change, cancel it and request a new one; later configuration changes
do not edit the held snapshot. [GitHub consent](github-consent.md) explains that separate flow, including
expiry, cancellation and an ambiguous submission outcome. The expiring held preview includes the draft
and reporter/scope identifiers; a related issue link, feedback verdict or human-help request does not supply filing consent.

If you only want an answer, continue the question with the relevant product, feature or error instead of
accepting a filing invitation. [Privacy](concepts/privacy.md) distinguishes interaction rows from temporary
request state, the public keyword cache, Discord messages and external processing. Sanitization and the
absence of user IDs from an interaction row do not establish anonymity, provider training terms or legal obligations.

Offline executable specs use fake HTTP and classifier/answer collaborators;
they do not establish live provider or Discord behavior. Operators still need to check their actual
configured services, authorization and delivery; configuration alone is not that check.
