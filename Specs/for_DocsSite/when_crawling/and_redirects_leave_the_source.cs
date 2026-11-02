// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_redirects_leave_the_source : given.a_docs_fetch
{
    readonly List<Exception?> _errors = [];
    readonly List<int> _counts = [];

    async Task Because()
    {
        foreach (var destination in new[] { "https://evil.invalid/a", "https://docs.invalid.evil.invalid/a", "http://docs.invalid/a", "https://reader@docs.invalid/a", "https://docs.invalid:444/a" })
        {
            foreach (var pageRedirect in new[] { false, true })
            {
                Requests.Clear();
                Respond = uri => pageRedirect && uri.AbsolutePath == "/sitemap-0.xml"
                    ? Text("<loc>https://docs.invalid/a</loc>")
                    : Redirect(destination);
                await Crawl();
                _errors.Add(Error);
                _counts.Add(Requests.Count - (pageRedirect ? 1 : 0));
            }
        }
    }

    [Fact] void should_reject_every_unapproved_destination() => _errors.TrueForAll(error => error is IncompleteCrawl).ShouldBeTrue();
    [Fact] void should_not_request_any_redirect_destination() => _counts.TrueForAll(count => count == 1).ShouldBeTrue();
}
