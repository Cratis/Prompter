// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_SourceHosts.when_checking_citations;

public class and_the_page_is_the_llms_txt_index : Specification
{
    [Theory]
    [InlineData("https://cratis.no/llms.txt", false)]
    [InlineData("https://cratis.studio/llms.txt", false)]
    [InlineData("https://cratis.io/llms.txt", false)]
    [InlineData("https://cratis.no/LLMS.TXT", false)]
    [InlineData("https://cratis.no/stack/", true)]
    [InlineData("https://cratis.no/llms.txt/more", true)]
    public void should_exclude_only_the_exact_llms_txt_path(string url, bool expected) =>
        SourceHosts.IsCitable(new(url), new()).ShouldEqual(expected);
}
