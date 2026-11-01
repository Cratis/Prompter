// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_SecretRedaction.when_redacting;

public class and_urls_contain_user_info : Specification
{
    static string Compose(params string[] fragments) => string.Concat(fragments);

    [Fact]
    void should_preserve_the_url_delimiters_and_redact_multiple_credentials()
    {
        var credential = Compose("https", "://", "example", ":", "synthetic", "@example.invalid");
        var text = credential + " " + credential;
        const string sanitized = "https://" + SecretRedaction.Marker + "@example.invalid";
        SecretRedaction.Redact(text).Text.ShouldEqual(sanitized + " " + sanitized);
    }

    [Fact]
    void should_redact_user_info_after_a_nested_scheme()
    {
        var text = Compose("https", "://outer", "://", "example", ":", "synthetic", "@example.invalid");
        SecretRedaction.Redact(text).Text.ShouldEqual("https://outer://" + SecretRedaction.Marker + "@example.invalid");
    }

    [Fact]
    void should_preserve_a_url_without_a_password()
    {
        var text = Compose("https", "://", "example", ":", "@example.invalid");
        SecretRedaction.Redact(text).Text.ShouldEqual(text);
    }

    [Fact]
    void should_redact_a_long_user_info_run()
    {
        var text = Compose("https", "://", new string('x', 4000), ":", new string('y', 4000), "@example.invalid");
        SecretRedaction.Redact(text).Text.ShouldEqual("https://" + SecretRedaction.Marker + "@example.invalid");
    }
}
