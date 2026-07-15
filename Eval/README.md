# Eval — the golden Q&A set and the harness

`golden-questions.yaml` is Prompter's answer-quality gate (backlog **P-17**). It is a curated set of
questions phrased the way real Cratis community members ask in Discord, each grounded in — or, for the
out-of-scope block, deliberately *not* grounded in — the published docs at <https://cratis.io>.

The harness that consumes it is **P-18** (`Prompter.Eval.csproj`, this folder); the CI gate that enforces
it (**P-19**, `.github/workflows/eval.yml`) runs the harness on demand and fails the build when scores
regress below the committed floor in `baseline.json` — see [The CI gate](#the-ci-gate) below.

## Running the harness

`Prompter.Eval` is a developer-only console project (referenced in `Prompter.slnx`, never pulled into the
bot's Docker publish). It runs each golden question through the bot's real `IAnswers` pipeline against the
live corpus and scores three things per question:

- **Citation-hit** (in-scope): did retrieval surface at least one `expected_pages` entry? Compared after
  normalizing the ingested `.md` mirror form against the canonical golden form (`Scoring/CitationHit.cs`,
  `Scoring/PageMatching.cs`).
- **Refusal-accuracy**: out-of-scope questions must refuse; an in-scope question must not
  (`Scoring/RefusalAccuracy.cs`). A confident answer to an out-of-scope question is a hard failure.
- **Groundedness** (in-scope answers): an `IChatClient` judge scores 1–5 whether every claim is supported by
  the retrieved passages (`Scoring/GroundednessEvaluator.cs`, using `Microsoft.Extensions.AI.Evaluation`,
  cribbed from `dotnet/eShopSupport`'s `AnswerScoringEvaluator`).

It needs the same infrastructure and keys as the bot: a populated Postgres/pgvector corpus (`docker compose
up -d` then `dotnet run --project Source -- index`), a Voyage key for retrieval embeddings, and an Anthropic
key for both the answer and the groundedness judge — supplied the usual way
(`Cratis__Prompter__Voyage__ApiKey`, `Cratis__Prompter__Anthropic__ApiKey`).

```bash
dotnet run --project Eval
```

It writes a timestamped markdown + JSON report (per-question rows plus aggregate scores) to
`Eval/results/`, which is git-ignored. Optional flags: `--golden <path>` and `--out <dir>`.

## The CI gate

`.github/workflows/eval.yml` (**P-19**) turns the harness into a regression gate. Because a run costs Voyage
+ Anthropic calls and a full index of cratis.io, it is **not** on every PR — it runs only on
`workflow_dispatch` or when a pull request carries the **`eval`** label (add the label to have a PR scored;
an unlabeled PR never spends API budget). The job stands up a `pgvector/pgvector:pg17` service container
matching `docker-compose.yml`, indexes the corpus, runs the harness, uploads the report as a workflow
artifact, and then enforces the baseline.

It needs two repository secrets (Settings → Secrets and variables → Actions), mapped onto the config the app
binds:

| Secret | Bound to | Used for |
|---|---|---|
| `VOYAGE_API_KEY` | `Cratis__Prompter__Voyage__ApiKey` | retrieval embeddings (index + eval) |
| `ANTHROPIC_API_KEY` | `Cratis__Prompter__Anthropic__ApiKey` | the answer and the groundedness judge |

`Eval/check-baseline.py` compares the newest `Eval/results/report-*.json` against `Eval/baseline.json`. For
each metric in the baseline it reads the report's `summary.<metric>` and fails the job if any value is below
`floor - tolerance`. The tracked metrics — and their keys — are exactly what `EvalReport.ToJson()` emits:

| Baseline metric | Report field | Scale |
|---|---|---|
| `citationHitRate` | `summary.citationHitRate` | fraction 0–1 |
| `refusalAccuracy` | `summary.refusalAccuracy` | fraction 0–1 |
| `meanGroundedness` | `summary.meanGroundedness` | mean 1–5 |

**The committed `baseline.json` currently holds placeholder floors.** They were seeded without a real run
(the harness needs live keys and a corpus). Regenerate them from an actual run once the secrets exist, and
commit the observed scores as the real floor:

```bash
docker compose up -d
dotnet run --project Source -- index         # populate the pgvector corpus
dotnet run --project Eval                     # writes Eval/results/report-<stamp>.json
```

Open the newest `Eval/results/report-*.json`, copy `summary.citationHitRate`, `summary.refusalAccuracy`, and
`summary.meanGroundedness` into the `metrics` block of `Eval/baseline.json` (round down a touch so normal
run-to-run noise doesn't trip the gate — `tolerance` also absorbs a little), and commit. The gate is proven
the way the plan's "done when" describes it: a deliberately broken prompt drops a metric below its floor and
fails the workflow; the good prompt stays above it and passes.

## What's in the set

| Bucket | Count | Notes |
|---|---:|---|
| In-scope (`expected: answer`) | 57 | Must be answered, grounded, and cited |
| Out-of-scope (`expected: refuse`) | 12 | Must be refused honestly |

In-scope coverage by product:

| Product | Count | `id` prefix |
|---|---:|---|
| Chronicle | 19 | `chr-` |
| Arc | 13 | `arc-` |
| Fundamentals | 7 | `fun-` |
| Components | 8 | `cmp-` |
| CLI | 6 | `cli-` |
| Cross-cutting (the stack, FAQ-style) | 4 | `gen-` |

Question types span definition, how-to, configuration, troubleshooting, concept, comparison, and
getting-started, so the set exercises retrieval and answering across the shapes real questions take.

## Schema and how it's scored

The file header comments in `golden-questions.yaml` are the authoritative schema reference. In short, each
entry carries a stable `id`, its `product`/`type`, the verbatim `question`, an `expected`
(`answer` | `refuse`), and — for in-scope entries — `expected_pages`, the real doc URLs (canonical form,
**without** the `.md` mirror suffix) that should ground a correct answer.

The harness scores two things:

- **In-scope** — retrieval must surface at least one `expected_pages` URL (normalize by stripping a trailing
  `.md`, since the ingested form is the per-page mirror), *and* the answer must be grounded in the retrieved
  passages and cite at least one source.
- **Out-of-scope** — the bot must refuse; a confident, cited-looking answer is a hard failure. These
  calibrate the refusal threshold (`Answering:MinScore`, P-07).

`expected_pages` are grounding *candidates*, not an exhaustive citation allow-list — a good answer may
additionally cite closely related pages.

## Maintaining it

- **Never renumber an `id`.** Baselines and regression diffs key off it. Add new questions with fresh ids;
  retire stale ones by removing the entry, not by reusing its id.
- **Keep `expected_pages` real.** Every URL must resolve on the live site (excluding `client-snippets/` and
  `api-reference/`, which ingestion skips). When the docs restructure, fix the URLs here in the same change.
- Grow the set from real signal over time: Discord history, the docs-gap flywheel (refusals + 👎 answers,
  P-33), and the FAQ.
