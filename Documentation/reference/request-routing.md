# Request routing

Prompter's optional request classifier supplies advisory product, intent, and response
branches. It does not answer questions, choose repositories, establish documentation
facts, or authorize GitHub writes. Host integration must explicitly opt in; adding
these types alone does not change message handling.

## Configuration

Bind `RoutingOptions` in the composition root and call `AddRequestRouting(options,
productKeys)`. Supply keys from `GitHubOptions.Repositories`, not repository names
or user input. The classifier accepts up to 31 distinct lowercase ASCII keys
(letters, digits, and hyphens; at most 40 characters each) plus `unknown`.
Invalid keys are excluded, and excess keys are excluded in ordinal order.

| Option | Default | Meaning |
| --- | --- | --- |
| `Endpoint` | Unset | Explicit private service origin, such as `http://laya.internal:8080` |
| `AllowedHost` | Empty | Exact independently approved endpoint host |
| `Mode` | `Active` | Use configured Laya; `Shadow` compares without changing results |
| `ShadowPercentage` | `0` | Shadow off; clamped to 0–10 percent |
| `MinimumConfidence` | `0.75` | Minimum winning probability for every decision |
| `MinimumMargin` | `0.15` | Minimum winner-to-runner-up probability difference |

Only HTTP(S) origins without credentials, paths other than `/`, queries, or
fragments are accepted. Hosts must match `AllowedHost` and be loopback, an RFC1918
IPv4 address, an IPv6 unique-local address, `localhost`, or a name ending in
`.internal`, `.svc`, or `.cluster.local`. Link-local metadata addresses and public
origins are rejected. The host's DNS, service identity, authentication needs, and
network isolation remain operator responsibilities: a private-looking DNS suffix
is not proof that the deployed service is authentic or privately resolved.

The dedicated registration disables redirects and proxies. Do not replace its
HTTP pipeline with redirecting, retrying, or payload-logging middleware. No
credential, public endpoint, hosted-provider fallback, or model download is supplied
by this library. Leave the endpoint unset until the operator approves the actual
private service and its contract.

## Decision API contract

The adapter targets Cratis Decision API v1, not a guessed stock Laya `/classify`
endpoint. It sends one `POST /v1/decisions/batch` with three `decisions` entries:

- `requestId`: a new per-decision correlation value, never a Discord identifier.
- `context`: bounded untrusted `text` and null `structured` data.
- `choices`, `question`, and `descriptions`: caller-owned finite choice sets.

The three decisions are product (configured keys plus `unknown`), intent
(`question`, `bug`, `feature`, `idea`, `documentation`), and advisory action
(`answer`, `offer-issue`, `clarify`).

The response must have three ordered `results`, each echoing the exact `requestId`
and containing a `choices` object with exactly the requested keys. Duplicate keys,
missing or invented choices, mismatched correlations, malformed JSON, nonfinite or
out-of-range probabilities, and distributions whose sum differs from 1 by more
than 0.001 are rejected. Provider `model`, `engine`, `latencyMs`, and other metadata
are not copied into classifications or observations. Ties and insufficient
confidence or margin use the deterministic fallback. Thresholds are uncalibrated
configuration defaults, not measured accuracy claims.

There is one HTTP attempt, a one-second classifier subdeadline linked to caller
cancellation, and a 64 KiB response limit (including streams without content
length). There are no retries or paid fallback calls. Caller cancellation
propagates; classifier timeouts and service failures return safe fallback data.
A shared singleton slot retains ownership of a noncooperative transport until it
completes; subsequent requests fall back immediately instead of accumulating
abandoned operations. Shadow calls are awaited within the same deadline, never
launched as background comparison work.

## Admission and trust boundary

The host must classify **after** protection admission and inside the admitted
request lease. Pass the lease cancellation token. Both interaction and mention
paths should resolve the same `IRequestClassifier` singleton.

Pass only the sanitized current request and already bounded, sanitized request
excerpts from the same public conversation scope. Do not pass bot answers,
private messages, cross-channel/thread history, Discord names or IDs, tokens,
credentials, or guessed history. This API deliberately accepts request text rather
than Discord transport objects; callers own scope checks and redaction. Current
text is first in the 4000-character excerpt; older excerpts use only the remaining
space, newest first. All excerpt text is explicitly marked untrusted.

Treat the returned `Classification` as advisory data, not prompt instructions.
Never add it to documentation evidence, trusted system instructions, or generated
GitHub content. Resolve `Product.Value` again through `IssueRouting` and the
configured repository allowlist. `OfferIssue` can only offer drafting help; a
separate explicit filing action, a held sanitized draft, admission, and the
existing GitHub configuration guard are still required to write anything.
Unknown products always clarify. An offer with question/documentation intent is
also downgraded to clarification.

The deterministic fallback recognizes a configured whole-word product only if
unambiguous and always returns question/clarify. It never infers filing intent,
repository names, or permission from suspicious text.

## Shadow observations

Shadow is opt-in and samples deterministically by admitted invocation count, at
most 10 percent, rounding down for each prefix. It returns the deterministic
baseline regardless of Laya's result. It does not invoke another model.

`IClassificationSink` is synchronous and must not throw. The default
`NoOpClassificationSink` stores nothing. Observations contain only enum-valued
mode, intent, action, baseline action, confidence, fallback reason, and elapsed
bucket. No raw prompt, provider output, product string, channel, user, message,
correlation, repository, or exception text crosses this boundary. Metrics wiring
is separate from routing registration.
