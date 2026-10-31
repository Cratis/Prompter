// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class a_response : Specification
{
    [Theory]
    [InlineData("answered", InteractionCategory.Answered)]
    [InlineData("docs-gap", InteractionCategory.DocsGap)]
    [InlineData("missing-capability", InteractionCategory.MissingCapability)]
    [InlineData("likely-bug", InteractionCategory.LikelyBug)]
    [InlineData("out-of-scope", InteractionCategory.OutOfScope)]
    [InlineData("unknown", InteractionCategory.Unknown)]
    [InlineData("surprise", InteractionCategory.Unknown)]
    [InlineData("Answered", InteractionCategory.Unknown)]
    public void should_map_the_category_explicitly(string wire, InteractionCategory expected) =>
        AnswerGeneration.Parse($$"""{"text":"Use it [1].","category":"{{wire}}","product":"Arc"}""")!.Category.ShouldEqual(expected);

    [Theory]
    [InlineData("Chronicle", InteractionProduct.Chronicle)]
    [InlineData("Arc", InteractionProduct.Arc)]
    [InlineData("Fundamentals", InteractionProduct.Fundamentals)]
    [InlineData("Components", InteractionProduct.Components)]
    [InlineData("CLI", InteractionProduct.Cli)]
    [InlineData("other/unknown", InteractionProduct.OtherOrUnknown)]
    [InlineData("Orleans", InteractionProduct.OtherOrUnknown)]
    public void should_map_the_product_explicitly(string wire, InteractionProduct expected) =>
        AnswerGeneration.Parse($$"""{"text":"Use it [1].","category":"answered","product":"{{wire}}"}""")!.Product.ShouldEqual(expected);

    [Theory]
    [InlineData("""{"text":"Keep me.","category":3,"product":2}""")]
    [InlineData("""{"text":"Keep me.","category":null}""")]
    [InlineData("""{"text":"Keep me."}""")]
    [InlineData("""{"text":"Keep me.","category":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","product":["Arc"]}""")]
    public void should_keep_valid_text_when_the_signals_are_invalid(string envelope)
    {
        var generated = AnswerGeneration.Parse(envelope)!;
        (generated.Text, generated.Category, generated.Product).ShouldEqual(("Keep me.", InteractionCategory.Unknown, InteractionProduct.OtherOrUnknown));
    }

    [Fact] void should_keep_plain_text_usable() => AnswerGeneration.Parse("Chronicle stores facts [1].")!.Text.ShouldEqual("Chronicle stores facts [1].");
    [Fact] void should_classify_plain_text_as_unknown() => AnswerGeneration.Parse("Plain.")!.Category.ShouldEqual(InteractionCategory.Unknown);
    [Fact] void should_reject_an_envelope_without_text() => AnswerGeneration.Parse("""{"category":"answered"}""").ShouldBeNull();
    [Fact] void should_reject_an_envelope_with_non_text() => AnswerGeneration.Parse("""{"text":7}""").ShouldBeNull();
    [Fact] void should_reject_an_envelope_cut_inside_the_text() => AnswerGeneration.Parse("{\"text\": \"cut off").ShouldBeNull();
    [Fact] void should_keep_the_complete_text_of_an_envelope_cut_after_it() => AnswerGeneration.Parse("{\"text\":\"Use it [1].\",\"category\":\"answ")!.Text.ShouldEqual("Use it [1].");
    [Fact] void should_classify_a_salvaged_text_as_unknown() => AnswerGeneration.Parse("{\"text\":\"Use it [1].\",\"category\":\"answ")!.Category.ShouldEqual(InteractionCategory.Unknown);
    [Fact] void should_reject_garbage_after_an_opening_brace() => AnswerGeneration.Parse("{not json").ShouldBeNull();
    [Fact] void should_reject_an_empty_response() => AnswerGeneration.Parse("  ").ShouldBeNull();
}
