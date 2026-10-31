// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_SecretRedaction.when_redacting;

public class and_text_contains_secret_shapes : Specification
{
    static string Compose(params string[] fragments) => string.Concat(fragments);

    static string[] Shapes() =>
    [
        Compose("Bearer ", new string('a', 20)),
        Compose("api_key", "=", new string('x', 12)),
        Compose("Account", "Key", "=", new string('x', 12)),
        Compose("sk", "-ant-", new string('a', 24)),
        Compose("gh", "p_", new string('a', 36)),
        Compose("github", "_pat_", new string('a', 36)),
        Compose("M", new string('a', 23), ".", new string('a', 6), ".", new string('a', 27)),
        Compose("pa", "-", new string('a', 24)),
        Compose("ey", "J", new string('a', 20), ".", new string('b', 20), ".", new string('c', 20)),
        Compose("-----BEGIN ", "PRIVATE KEY-----\nfake\n-----END ", "PRIVATE KEY-----"),
        Compose("https", "://", "fake", ":", "example", "@example.invalid"),
        Compose("AK", "IA", new string('A', 16))
    ];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    void should_remove_the_value_without_echoing_it(int shape)
    {
        var text = Shapes()[shape];
        var result = SecretRedaction.Redact(text);
        result.Removed.ShouldBeTrue();
        result.Text.Contains(SecretRedaction.Marker, StringComparison.Ordinal).ShouldBeTrue();
        result.Text.Contains(text, StringComparison.Ordinal).ShouldBeFalse();
    }
}
