// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocumentationMetadata.when_reading;

public class a_page : Specification
{
    static readonly PageUrl _page = new("https://cratis.io/a.md");

    static DocumentationMetadata Read(string markdown) => DocumentationMetadata.From(_page, markdown);

    [Fact] void should_read_an_explicit_iso_date() => Read("---\nlastUpdated: 2026-03-04\n---\n# A").PageLastModified.ShouldEqual(new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero));
    [Fact] void should_read_a_quoted_timestamp() => Read("---\nlastUpdated: \"2026-03-04T10:00:00Z\"\n---\n# A").PageLastModified.ShouldEqual(new DateTimeOffset(2026, 3, 4, 10, 0, 0, TimeSpan.Zero));
    [Fact] void should_ignore_an_invalid_date() => Read("---\nlastUpdated: last tuesday\n---\n# A").PageLastModified.ShouldBeNull();
    [Fact] void should_ignore_a_duplicate_date() => Read("---\nlastUpdated: 2026-03-04\nlastUpdated: 2026-03-05\n---\n# A").PageLastModified.ShouldBeNull();
    [Fact] void should_ignore_dates_in_the_body() => Read("# A\n\nlastUpdated: 2026-03-04").PageLastModified.ShouldBeNull();
    [Fact] void should_read_a_frontmatter_version() => Read("---\nproductVersion: v2.3.4\n---\n# A").ProductVersion.ShouldEqual("2.3.4");
    [Fact] void should_read_a_prerelease_version() => Read("---\nversion: 1.0.0-rc.1\n---\n# A").ProductVersion.ShouldEqual("1.0.0-rc.1");
    [Fact] void should_read_an_explicit_product_heading() => Read("# Chronicle v15.1.0\n\nBody").ProductVersion.ShouldEqual("15.1.0");
    [Fact] void should_reject_conflicting_versions() => Read("---\nproductVersion: 1.0.0\nversion: 2.0.0\n---\n# A").ProductVersion.ShouldBeNull();
    [Fact] void should_reject_frontmatter_conflicting_with_the_heading() => Read("---\nversion: 1.0.0\n---\n# Arc v2.0.0").ProductVersion.ShouldBeNull();
    [Fact] void should_not_infer_a_version_from_prose() => Read("# Guide\n\nUse version 3.2.1 of the package.").ProductVersion.ShouldBeNull();
    [Fact] void should_not_infer_a_version_from_code() => Read("# Guide\n\n```\n# Arc v9.9.9\n```").ProductVersion.ShouldBeNull();
    [Fact] void should_reject_an_oversized_version() => Read($"---\nversion: 1.0.0-{new string('a', 80)}\n---\n# A").ProductVersion.ShouldBeNull();
    [Fact] void should_reject_a_non_semantic_version() => Read("---\nversion: latest\n---\n# A").ProductVersion.ShouldBeNull();
    [Fact] void should_state_nothing_for_a_plain_page() => Read("# Guide\n\nBody").ShouldEqual(DocumentationMetadata.Empty(_page));
    [Fact] void should_carry_the_page() => Read("# A").Page.ShouldEqual(_page);
}
