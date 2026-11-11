// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.given;

public class delayed_forum_answers : Specification
{
    protected const ulong Forum = 42;
    protected const ulong Owner = 3;
    protected const ulong Bot = 99;
    protected static readonly DateTimeOffset Start = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    readonly List<ForumThreadSnapshot> _posts = [];

    protected IForumThreads Threads { get; private set; } = null!;
    protected IForumAnswerer Answerer { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected ControlledTime Clock { get; private set; } = null!;
    protected DelayedForumAnswers Sweeper { get; private set; } = null!;
    protected AnsweringSwitch Answering { get; } = Switch.Running();
    protected int Answered { get; set; }

    protected DelayedForumAnswers Create()
    {
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        return new(Threads, Answerer, new(config, new RateLimiter(config), Clock, Answering), Answering, config, Clock, NullLogger<DelayedForumAnswers>.Instance);
    }

    protected void Post(ulong id, int minutesOld, bool closed = false, ulong forum = Forum)
    {
        _posts.Add(new(id, 1, forum, Owner, Start.AddMinutes(-minutesOld), closed));
        Threads.Active(forum, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ForumThreadSnapshot>>([.. _posts.Where(post => post.ParentId == forum)]));
        Messages(id, new ForumMessage(id, Owner, false, false, true));
    }

    protected void Messages(ulong threadId, params ForumMessage[] messages) =>
        Threads.Recent(threadId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ForumMessage>>(messages));

    protected IEnumerable<ulong> AnsweredThreads() =>
        Answerer.ReceivedCalls().Select(call => ((ForumThreadRef)call.GetArguments()[0]!).Id);

    void Establish()
    {
        Options = new PrompterOptions();
        Options.Protection.AllowedGuilds = [1];
        Options.Discord.DelayedForums = [new() { ChannelId = Forum, DelayMinutes = 30 }];
        Clock = new(Start);
        Threads = Substitute.For<IForumThreads>();
        Threads.BotId(Arg.Any<CancellationToken>()).Returns(Bot);
        Threads.Active(Arg.Any<ulong>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ForumThreadSnapshot>>([]));
        Answerer = Substitute.For<IForumAnswerer>();
        Answerer.Answer(Arg.Any<ForumThreadRef>(), Arg.Any<ulong>(), Arg.Any<NetCord.Rest.RestMessage?>(), Arg.Any<bool>()).Returns(Task.CompletedTask);
        Sweeper = Create();
    }
}
