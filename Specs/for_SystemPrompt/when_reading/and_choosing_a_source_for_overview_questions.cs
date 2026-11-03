// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_SystemPrompt.when_reading;

public class and_choosing_a_source_for_overview_questions : Specification
{
    [Fact] public void should_prefer_documentation_for_broad_overview_questions() =>
        SystemPrompt.Text.ShouldContain("for broad overview questions about what Cratis");
    [Fact] public void should_treat_documentation_overview_pages_as_canonical() =>
        SystemPrompt.Text.ShouldContain("the documentation's own overview pages are the canonical");
    [Fact] public void should_reserve_the_company_site_for_company_facts() =>
        SystemPrompt.Text.ShouldContain("Use the company site for company, people, Assurance support and trust");
}
