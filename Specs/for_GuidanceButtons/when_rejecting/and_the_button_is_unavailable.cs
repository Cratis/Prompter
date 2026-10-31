// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_GuidanceButtons.when_rejecting;

public class and_the_button_is_unavailable : for_AskPrompter.given.an_interaction
{
    [Theory]
    [InlineData("suggest:tampered", false)]
    [InlineData("suggest:99", false)]
    [InlineData("clarify:tampered", true)]
    [InlineData("clarify:arc", true)]
    public async Task should_notify_without_consuming_admission(string customId, bool clarify)
    {
        Button(customId: customId);
        if (clarify) await Buttons.Clarify();
        else await Buttons.Suggest();
        NoPaidWork();
        Admission.TryEnter(new(1, 70, 3), "An answer").Allowed.ShouldBeTrue();
        using var payload = JsonDocument.Parse(Payloads.Single());
        var data = payload.RootElement.GetProperty("data");
        data.GetProperty("content").GetString().ShouldEqual(clarify ? GuidanceButtons.ClarificationUnavailable : GuidanceButtons.SuggestionUnavailable);
        data.GetProperty("flags").GetInt32().ShouldEqual(64);
        data.GetProperty("allowed_mentions").GetProperty("parse").GetArrayLength().ShouldEqual(0);
    }
}
