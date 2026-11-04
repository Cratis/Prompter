// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalSet.when_loading;

public class and_the_golden_set_is_read : Specification
{
    static readonly string[] _broadCases = ["gen-010", "gen-011", "gen-012"];

    EvalSet _set = null!;

    void Because() => _set = EvalSet.Load(EvalSet.DefaultPath(AppContext.BaseDirectory)!);

    IReadOnlyList<string> Pages(string id) => _set.Questions.Single(item => item.Id == id).ExpectedPages;

    [Fact] void should_read_all_cases() => _set.Questions.Count.ShouldEqual(88);
    [Fact] void should_keep_ids_unique() => _set.Questions.Select(item => item.Id).Distinct().Count().ShouldEqual(88);
    [Fact] void should_include_the_broad_overview_cases() => _broadCases.All(id => _set.Questions.Any(item => item.Id == id && item.MustNotMention.Contains("excerpt"))).ShouldBeTrue();
    [Fact] void should_require_facts_in_at_least_fifteen_cases() => (_set.Questions.Count(item => item.MustMention.Count > 0) >= 15).ShouldBeTrue();
    [Fact] void should_accept_the_company_overview_alongside_existing_product_pages() => _set.Questions.Single(item => item.Id == "gen-005").ExpectedPages.SequenceEqual(["https://cratis.io/cratis-stack/", "https://cratis.io/", "https://cratis.no/"]).ShouldBeTrue();
    [Fact] void should_accept_the_hosting_model_page_for_running_chronicle_locally() => Pages("chr-005").ShouldContain("https://cratis.io/chronicle/get-started/choose-hosting-model/");
    [Fact] void should_accept_the_connection_string_and_configuration_pages_for_the_dotnet_client() => Pages("chr-012").ShouldContain(["https://cratis.io/chronicle/connection-strings/configuration/", "https://cratis.io/chronicle/configuration/"]);
    [Fact] void should_accept_the_event_store_concept_page_for_supported_databases() => Pages("chr-013").ShouldContain("https://cratis.io/chronicle/concepts/event-store/");
    [Fact] void should_accept_the_observer_patterns_page_for_telling_observers_apart() => Pages("chr-015").ShouldContain("https://cratis.io/chronicle/concepts/observer-patterns/");
    [Fact] void should_replace_the_sequence_state_page_with_the_consistency_page_for_entity_state() => (Pages("chr-018").Contains("https://cratis.io/chronicle/read-models/consistency/") && !Pages("chr-018").Contains("https://cratis.io/chronicle/events/getting-state/") && Pages("chr-018").Contains("https://cratis.io/chronicle/read-models/getting-single-instance/")).ShouldBeTrue();
    [Fact] void should_accept_the_proxy_boundary_page_for_proxy_generation() => Pages("arc-009").ShouldContain("https://cratis.io/arc/understanding-the-proxy-boundary/");
    [Fact] void should_accept_the_typescript_backend_page_for_observable_queries() => Pages("arc-013").ShouldContain("https://cratis.io/arc/backend/typescript/queries/observable-queries/");
    [Fact] void should_score_the_company_overview_as_an_alternative_citation() => EvalScoring.Score(_set.Questions.Single(item => item.Id == "gen-005"), new("Chronicle Arc Components", [new("https://cratis.no/")], 0.8, false, [])).CitationHit.ShouldBeTrue();
}
