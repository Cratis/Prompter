// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PendingIssues.when_confirming;

public class and_scope_or_delivery_is_invalid : Specification
{
    readonly PendingIssues _pending = new(new ControlledTime(DateTimeOffset.UnixEpoch));
    readonly IssueConsent _scope = new(1, 2, 3);
    readonly PendingIssue _payload = new("Cratis", "Chronicle", "Title", "Body", []);
    bool _provisionalWasHidden;
    bool _wrongScopesWereHidden;
    bool _failedSendWasRevoked;
    PendingIssue? _legitimate;

    async Task Because()
    {
        var token = await _pending.Present(_payload, _scope, provisional =>
        {
            _provisionalWasHidden = _pending.Take(provisional, _scope, 4) is null;
            return Task.FromResult(4UL);
        });
        _wrongScopesWereHidden = _pending.Take(token, new(9, 2, 3), 4) is null &&
            _pending.Take(token, new(1, 9, 3), 4) is null &&
            _pending.Take(token, new(1, 2, 9), 4) is null &&
            _pending.Take(token, _scope, 9) is null;
        _legitimate = _pending.Take(token, _scope, 4);
        var failedToken = string.Empty;
        try
        {
            await _pending.Present(_payload, _scope, provisional =>
            {
                failedToken = provisional;
                return Task.FromException<ulong>(new HttpRequestException("Synthetic send failure"));
            });
        }
        catch (HttpRequestException)
        {
            _failedSendWasRevoked = _pending.Take(failedToken, _scope, 4) is null;
        }
    }

    [Fact] void should_not_activate_before_delivery() => _provisionalWasHidden.ShouldBeTrue();
    [Fact] void should_not_reveal_wrong_scope_payloads() => _wrongScopesWereHidden.ShouldBeTrue();
    [Fact] void should_not_consume_for_wrong_scope() => _legitimate.ShouldNotBeNull();
    [Fact] void should_revoke_failed_delivery() => _failedSendWasRevoked.ShouldBeTrue();
}
