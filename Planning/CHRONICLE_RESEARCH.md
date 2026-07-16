# Chronicle for the interaction log — researched, decided against (for now)

**Decision (2026-07-16):** Do **not** adopt Cratis Chronicle / event sourcing for the interaction log in the
foreseeable roadmap. Keep the simple Postgres `IInteractionLog`. "It's just a Discord bot" — the analytics
value is real but not worth the added infrastructure and coupling right now. Revisit only if there's a concrete
pull (a real docs-gap analytics ambition, or a dogfooding showcase the team wants). This resolves the open
question in [`DECISIONS.md`](DECISIONS.md) **D-6**.

This note preserves the research (four repo deep-dives across Chronicle, Arc, and Ada) so a future revisit
starts from the findings, not from zero.

## Why "not now" was the right call

The idea is architecturally sound — the interaction log (`QuestionAsked → AnswerGiven/Refused → FeedbackGiven`)
is the one genuinely event-shaped part of the system, and Chronicle's GDPR support is excellent. But two
assumptions that made it look cheap turned out false:

1. **Chronicle is a separate server, not an in-process library.** It's an Orleans "kernel" (the
   `cratis/chronicle` container) that owns all storage; the app is a thin **gRPC client**
   (`chronicle://host:35000`). Storage is configured *on the kernel*, not in app code — so "just use the same
   Postgres we already run" is impossible; the kernel owns its own schema and you query only through the client
   API. It's always a distinct deployable + a local container for dev. A self-contained bot (one image, one
   Postgres) would gain a hard runtime dependency on the kernel.
2. **The Postgres backend is very new.** `Storage.Sql` (Npgsql/EF Core) is a real, supported backend — but
   first-committed **2026-05-26**, ~50 commits, with active correctness fixes to the SQL sink as recent as
   **2026-07-12** ("Fix PII active projections stalling on the SQL sink"). MongoDB is the proven, default path.
   Putting GDPR-sensitive data on the least-battle-tested backend is the opposite of why we kept the corpus on
   Postgres (D-4).

Our current GDPR posture (D-8 as amended by D-13: the interaction log stores no personal data at all) already
satisfies the launch requirements without any of this.

## What's true and worth knowing if we revisit

- **GDPR is a platform feature (the genuine draw).** Mark a `ConceptAs<T>` with `[PII]`
  (`Cratis.Chronicle.Compliance.GDPR`) → every event field of that type is encrypted at rest, keyed by the
  subject. Erasure = crypto-shredding: `eventStore.PII.DeleteEncryptionKeyFor(subject)` (decrypt then returns
  empty, never throws), plus `IEventLog.Redact(...)` to scrub event payloads. **Caveats:** deleting the key
  does not scrub already-materialized read-model docs until you re-project; and an **open high-severity bug**
  stores PII inside read-model *child collections* as plaintext and then throws on read — so keep PII as **flat
  scalar `[PII]` fields**, never nested in collections, and smoke-test ciphertext-at-rest.
- **Chronicle-only — no Arc.** Arc's value is the backend↔frontend proxy bridge for a React UI; a headless bot
  has none, so Arc buys nothing. All the primitives we'd need are `Cratis.Chronicle.*`.
- **Programming model (no aggregate root — Dynamic Consistency Boundary):** append events with `IEventLog.Append`;
  fold state with **reducers** (`IReducerFor<T>`) or declarative **projections** (`[ReadModel]` +
  `[FromEvent<T>]`/`[SetFrom<T>]`/`[SetValue<T>]`); side effects via **reactors** (`IReactor`, must be
  idempotent). App setup: `builder.AddCratisChronicle(o => o.EventStore = "...")` + `Cratis:Chronicle`
  connection config.
- **Deploy path if adopted:** reuse the Chronicle kernel the UpCloud cluster already runs for Studio (proven,
  Mongo-backed) — connect with Prompter's own `EventStore` namespace — rather than standing up new infra. This
  is why "right after v1, on the cluster" would have been the sensible timing.

## How we'd build it (the Ada way) if we revisit

The `IInteractionLog` seam means this is a contained swap, no product change. Follow Ada's event-modeling
methodology (see `/Volumes/sourcecode/repos/hive/Ada/.ai/skills/event-modeling` + `add-slice`): write an
Event-Modeling Brief (stream boundaries + PII subjects **first**, then commands/events, read models,
automations, specs), draw an `EventModel.md`, then build the vertical slice through the gates (backend build
Debug+Release zero-warnings → specs → code/security reviewer agents). Concrete file-level template to mirror:
`/Volumes/sourcecode/repos/hive/Ada/Source/Core/Invoicing/ReceivedVouchers/` (event, command+validator,
projection, reactor, and `Erase/` for the crypto-shred pattern).

_Researched 2026-07-16 against `cratis/Chronicle`, `cratis/Arc`, and `hive/Ada`._
