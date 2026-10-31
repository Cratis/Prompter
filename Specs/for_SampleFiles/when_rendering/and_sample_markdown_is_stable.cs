// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_SampleFiles.when_rendering;

public class and_sample_markdown_is_stable : Specification
{
    [Fact] void should_render_readmes_without_a_fence() => SampleFiles.Markdown("LearningPath/README.md", "# Intro\n\nDetails").ShouldEqual("# LearningPath/README.md\n\n# Intro\n\nDetails");
    [Fact] void should_use_a_longer_fence_than_code() => SampleFiles.Markdown("A.cs", "var text = \"````\";").ShouldEqual("# A.cs\n\n`````csharp\nvar text = \"````\";\n`````");
    [Fact] void should_render_typescript() => SampleFiles.Markdown("A.tsx", "export {}").ShouldContain("```typescript");
    [Fact] void should_leave_unknown_languages_empty() => SampleFiles.Markdown("A.txt", "text").ShouldEqual("# A.txt\n\n```\ntext\n```");

    [Fact]
    void should_chunk_identically_on_subsequent_runs()
    {
        var page = new PageUrl("https://github.com/Cratis/Samples/blob/main/LearningPath/A.cs");
        var first = MarkdownChunker.Chunk(page, SampleFiles.Markdown("LearningPath/A.cs", "public record Sample();")).ToArray();
        var second = MarkdownChunker.Chunk(page, SampleFiles.Markdown("LearningPath/A.cs", "public record Sample();")).ToArray();
        first.Length.ShouldEqual(1);
        first.Select(chunk => chunk.Id).ShouldEqual(second.Select(chunk => chunk.Id));
        first.Select(chunk => chunk.Hash).ShouldEqual(second.Select(chunk => chunk.Hash));
    }
}
