// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_excerpt_is_large : given.a_classifier
{
    string _excerpt = string.Empty;

    async Task Because()
    {
        result = await classifier.Classify("CURRENT REQUEST", [new string('z', 5000)]);
        using var document = JsonDocument.Parse(transport.Body);
        _excerpt = document.RootElement.GetProperty("decisions")[0].GetProperty("context").GetProperty("text").GetString()!;
    }

    [Fact] void should_bound_the_entire_excerpt() => _excerpt.Length.ShouldEqual(4000);
    [Fact] void should_preserve_current_request_first() => _excerpt.StartsWith("Untrusted current request:\nCURRENT REQUEST", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_mark_history_as_untrusted() => _excerpt.Contains("Untrusted older request excerpt:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_only_send_one_request() => transport.Calls.ShouldEqual(1);
}
