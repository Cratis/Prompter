---
title: Grounded answers
description: Understand Prompter's citation contract, evidence-based refusals and documentation refresh process, and check what retrieved sources actually support.
---

A Prompter answer gives you a place to check it: open the numbered source links and compare the answer
with those pages. The model composes the reply from material retrieved for your question, so checking the
sources helps you tell what that material actually supports.

## Citations are the contract

Discord shows numbered `Sources:` links, and the `ask` CLI lists numbered source URLs: `[1]` in the answer
always points to source `[1]`. Several passages from one page share a citation number, so you can check
the answer at page level rather than hunting for each excerpt.

The model is instructed to use only the retrieved passages and cite the pages it relies on. A citation
alone does not establish that every claim is supported: read the page for the API, version or explanation
you need. If the answer has no valid references, Prompter offers up to four retrieved pages instead.
Those fallback links are suggested reading, not pages the answer cited.

[Your first answer](../getting-started/index.mdx) shows how to ask on Discord and open the source links.

## Refusals are a feature

A reasonable question can still retrieve no useful evidence. Before asking the model to compose an
answer, Prompter checks the retrieved candidate passages against a similarity threshold. If none qualify,
it refuses: *"I couldn't find anything in the Cratis documentation that answers this with
confidence"*. That tells you what happened to this search; it does not prove that the capability is absent.

A second refusal path covers off-topic questions. If the model judges that a question is not about Cratis at all,
Prompter declines it after generation, without sources, even when a page happens to share a word with it.

Refusals are logged and reviewed as documentation gaps; [Privacy](privacy.md) explains exactly what is
stored. Repeated refusals of a reasonable question, like the community's 👎 reactions, help maintainers
identify documentation to investigate or write.

## What "current" means precisely

Prompter searches indexed material, including the published documentation at [cratis.io](https://cratis.io).
Depending on configuration, the index can also include Cratis's company, Studio and Direct sites, public
repository READMEs and the organization profile, selected published release notes, and curated samples.

These sources serve different purposes. The model is instructed to prefer documentation for how features
work, release notes for changes in a version, and samples as illustrations rather than the only supported
approach.

The page you open may be newer than the copy Prompter searched. Retrieval uses the stored index, so a
source change becomes available through retrieval only after indexing picks it up. **Reindex - Production**
is scheduled for 03:52 UTC with a later catch-up check; maintainers can also run the workflow on `main`.
Scheduled indexing must be enabled, runs can be skipped or fail, and failed optional sources can retain
older content. That schedule does not guarantee an update within a day. A one-shot Job indexes the
sources without a public reindex route.

For a version-sensitive answer, check the cited page against the release notes for the version you use.
Source notes can show an index date and product version when available. An index date tells you about
the stored copy, not compatibility with your installed version.

## Answers are looked up, not remembered

Prompter uses retrieval-augmented generation (RAG): it searches indexed passages for each question and
supplies the admitted material to the language model. Updating the index changes what retrieval can
find; it is not model retraining.

Only referenced pages are listed, in page order, with numbers compacted if the answer skips pages.
For example, citing only the second and fifth retrieved pages produces sources `[1]` and `[2]`,
and the answer's original `[2]` and `[5]` markers are rewritten to match. Invalid prose markers are removed;
fenced code, inline code, escaped brackets, and attached indexing expressions such as `items[0]` stay unchanged.
Verbose CLI diagnostics label individual excerpts as `passage 1`, `passage 2`, and so on; these are not citation numbers.

## Summaries lead to page details

A summary can match your question while leaving the useful detail further down the page. For pages with
an admitted summary, Prompter looks up the best-matching non-summary excerpts, reusing the question
embedding. At most two detail excerpts receive reserved slots within the existing passage limit. They
must meet the same similarity threshold as other passages; a weak summary cannot bypass the refusal
gate. This lookup uses the existing index and needs no reindex.

## Answers read whole sections

A broad question such as "What is Cratis good at?" may need more context than a single chunk contains.
For up to three admitted passages, Prompter also reads the rest of each passage's section (everything
under the same second level heading) and the chunks immediately before and after it. When the whole
page is under 4,000 bytes, it also reads the whole page.

These extra chunks keep the page order and add at most 8,000 bytes, always less than what remains under
the answer input ceiling after the question, the history and the base passages. They attach only to an
admitted passage, so a question with no admitted passage is still refused. The model is instructed to speak about
Cratis directly without describing the retrieved excerpts.
