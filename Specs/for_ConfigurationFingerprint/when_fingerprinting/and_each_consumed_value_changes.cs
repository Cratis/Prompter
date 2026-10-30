// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Pulumi;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_ConfigurationFingerprint.when_fingerprinting;

public class and_each_consumed_value_changes : Specification
{
    readonly string[] _keys =
    [
        "Cratis__Prompter__ConnectionString",
        "Cratis__Prompter__Discord__Token",
        "Cratis__Prompter__Anthropic__ApiKey",
        "Cratis__Prompter__Voyage__ApiKey",
        "Cratis__Prompter__ReindexSecret",
        "Cratis__Prompter__GitHub__Token",
        "Cratis__Prompter__GitHub__WebhookSecret",
        "future-input",
    ];
    readonly List<string> _digests = [];
    string _original = null!;

    async Task Because()
    {
        _original = await OutputUtilities.GetValueAsync(ConfigurationFingerprint.Create(Data(string.Empty)));
        foreach (var key in _keys)
        {
            _digests.Add(await OutputUtilities.GetValueAsync(ConfigurationFingerprint.Create(Data(key))));
        }
    }

    [Fact] void should_change_for_every_consumed_key() => _digests.TrueForAll(digest => digest != _original).ShouldBeTrue();

    InputMap<string> Data(string changed)
    {
        var data = new InputMap<string>();
        foreach (var key in _keys)
        {
            data.Add(key, key == changed ? "synthetic-replacement" : "synthetic-original");
        }
        return data;
    }
}
