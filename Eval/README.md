# Eval — the golden Q&A set and, later, the harness

`golden-questions.yaml` is Prompter's answer-quality gate (backlog **P-17**). It is a curated set of
questions phrased the way real Cratis community members ask in Discord, each grounded in — or, for the
out-of-scope block, deliberately *not* grounded in — the published docs at <https://cratis.io>.

This is a content/data artifact: no code depends on it yet. The harness that consumes it (**P-18**,
`Eval/Prompter.Eval.csproj`) and the CI gate that enforces it (**P-19**, `.github/workflows/eval.yml`)
land in milestone M4.

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
