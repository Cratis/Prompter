// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_HtmlText.when_converting;

public class and_the_page_has_no_main_element : Specification
{
    [Fact] public void should_use_body_without_head_text() => HtmlText.ToMarkdown("<head>Not prose</head><body><h1>Studio</h1><p>Visual modeling</p></body>").ShouldEqual("# Studio\n\nVisual modeling");
    [Fact] public void should_accept_a_fragment() => HtmlText.ToMarkdown("<h1>Direct</h1><p>Deployment</p>").ShouldEqual("# Direct\n\nDeployment");
}
