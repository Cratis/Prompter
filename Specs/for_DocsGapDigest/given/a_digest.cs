// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DocsGapDigest.given;

public class a_digest : Specification
{
    protected IDocsGapInteractions Interactions { get; private set; } = null!;
    protected IMaintainerChannel Channel { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected DocsGapDigest Digest { get; private set; } = null!;
    protected DateTimeOffset Slot { get; } = new(2026, 11, 9, 8, 0, 0, TimeSpan.Zero);
    protected bool Posted { get; set; }

    void Establish()
    {
        Interactions = Substitute.For<IDocsGapInteractions>();
        Interactions.Read(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<DocsGapRow>>([]));
        Channel = Substitute.For<IMaintainerChannel>();
        Options = new PrompterOptions();
        Options.Discord.MaintainerChannelId = 123;
        Digest = new(Interactions, Channel, Microsoft.Extensions.Options.Options.Create(Options), new ControlledTime(Slot), NullLogger<DocsGapDigest>.Instance);
    }
}
