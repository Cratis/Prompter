// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.Fakes;

public sealed class FakeDocsSite(IReadOnlyList<DocsPage> pages) : IDocsSite
{
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var page in pages)
        {
            yield return page;
        }

        await Task.CompletedTask;
    }
}
