// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_IngestionOptions.when_validating;

public class and_crawl_bounds_are_configured : Specification
{
    readonly List<bool> _invalid = [];
    readonly List<bool> _edges = [];
    bool _defaults;

    void Because()
    {
        _defaults = new IngestionOptions().CrawlLimitsAreValid;
        Action<IngestionOptions>[] invalid =
        [
            options => options.MaxSitemapBytes = 65535,
            options => options.MaxSitemapBytes = 52428801,
            options => options.MaxSitemapEntries = 0,
            options => options.MaxSitemapEntries = 50001,
            options => options.MaxCrawlPages = 0,
            options => options.MaxCrawlPages = 100001,
            options => options.MaxCrawlBytes = 1048575,
            options => options.MaxCrawlBytes = 268435457,
            options => options.MaxCrawlChunks = 0,
            options => options.MaxCrawlChunks = 1000001
        ];
        foreach (var change in invalid)
        {
            var options = new IngestionOptions();
            change(options);
            _invalid.Add(options.CrawlLimitsAreValid);
        }
        _edges.Add(new IngestionOptions { MaxSitemapBytes = 65536, MaxSitemapEntries = 1, MaxCrawlPages = 1, MaxCrawlBytes = 1048576, MaxCrawlChunks = 1 }.CrawlLimitsAreValid);
        _edges.Add(new IngestionOptions { MaxSitemapBytes = 52428800, MaxSitemapEntries = 50000, MaxCrawlPages = 100000, MaxCrawlBytes = 268435456, MaxCrawlChunks = 1000000 }.CrawlLimitsAreValid);
    }

    [Fact] void should_accept_defaults() => _defaults.ShouldBeTrue();
    [Fact] void should_reject_out_of_range_values() => _invalid.TrueForAll(valid => !valid).ShouldBeTrue();
    [Fact] void should_accept_both_boundaries() => _edges.TrueForAll(valid => valid).ShouldBeTrue();
}
