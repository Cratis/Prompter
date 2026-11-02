// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ProtectionOptions.when_validating;

public class and_webhook_limits_are_configured : Specification
{
    bool _invalid;
    bool _valid;

    void Because()
    {
        ProtectionOptions[] invalid =
        [
            new() { WebhookConcurrency = 0 }, new() { WebhookConcurrency = 9 },
            new() { WebhookPerMinute = -1 }, new() { WebhookPerMinute = 121 },
            new() { WebhookNotificationsPerMinute = -1 }, new() { WebhookNotificationsPerMinute = 121 }
        ];
        _invalid = invalid.All(options => !options.IsValid);
        _valid = new ProtectionOptions { WebhookConcurrency = 8, WebhookPerMinute = 0, WebhookNotificationsPerMinute = 0 }.IsValid &&
            new ProtectionOptions { WebhookPerMinute = 120, WebhookNotificationsPerMinute = 120 }.IsValid;
    }

    [Fact] void should_reject_invalid_bounds() => _invalid.ShouldBeTrue();
    [Fact] void should_accept_the_documented_boundaries() => _valid.ShouldBeTrue();
    [Fact] void should_default_to_two_concurrent_webhooks() => new ProtectionOptions().WebhookConcurrency.ShouldEqual(2);
    [Fact] void should_default_to_twelve_webhooks_per_minute() => new ProtectionOptions().WebhookPerMinute.ShouldEqual(12);
    [Fact] void should_default_to_six_notifications_per_minute() => new ProtectionOptions().WebhookNotificationsPerMinute.ShouldEqual(6);
}
