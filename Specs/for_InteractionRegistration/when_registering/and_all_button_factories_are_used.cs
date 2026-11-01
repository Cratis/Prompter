// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_registering;

public class and_all_button_factories_are_used : Specification
{
    ServiceProvider _provider = null!;
    Type[] _modules = [];
    string[] _prefixes = [];

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddPrompterInteractions();
        _provider = services.BuildServiceProvider();
        _modules = typeof(Feedback).Assembly.GetTypes()
            .Where(type => type.BaseType is { IsGenericType: true } parent && parent.GetGenericTypeDefinition() == typeof(ComponentInteractionModule<>)).ToArray();
        var question = new QuestionLink(123, 456);
        var token = new string('a', 32);
        var ids = new[]
        {
            FeedbackButton.CustomId(FeedbackVerdict.Up, 42), FeedbackButton.CustomId(FeedbackVerdict.Down, 42),
            SuggestionButton.CustomId(0), SuggestionButton.CustomId(1), SuggestionButton.CustomId(2),
            ClarificationButton.CustomId(InteractionProduct.Chronicle),
            AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, question),
            AnswerActionButton.CustomId(AnswerActionButton.DocumentedPrefix, question),
            AnswerActionButton.RequestCustomId(question, "chronicle"),
            IssueButton.CustomId(IssueButton.FileAction, token), IssueButton.CustomId(IssueButton.CancelAction, token)
        };
        var rendered = FeedbackButtonRow.For(42, new(question, true, true, "chronicle")).Components.OfType<ButtonProperties>()
            .Concat(IssuePreview.Buttons(token).Components.OfType<ButtonProperties>()).Select(button => button.CustomId);
        _prefixes = ids.Concat(rendered).Select(id => id.Split(':')[0]).Distinct().ToArray();
    }

    void Because()
    {
        foreach (var service in _provider.GetServices<IComponentInteractionService>())
        {
            service.AddModules(typeof(Feedback).Assembly);
        }
    }

    [Fact] void should_register_every_module_context() => _modules.ShouldEachConformTo(module =>
        _provider.GetService(typeof(ComponentInteractionService<>).MakeGenericType(module.BaseType!.GenericTypeArguments[0])) is not null);

    [Fact] void should_cover_every_rendered_prefix_with_a_registered_module() => _prefixes.ShouldEachConformTo(prefix =>
        _modules.Any(module => _provider.GetService(typeof(ComponentInteractionService<>).MakeGenericType(module.BaseType!.GenericTypeArguments[0])) is not null &&
            module.GetMethods().Any(method => method.GetCustomAttribute<ComponentInteractionAttribute>()?.CustomId == prefix)));

    [Fact] void should_resolve_one_service_that_knows_every_prefix()
    {
        var service = _provider.GetRequiredService<ComponentInteractionService<ButtonInteractionContext>>();
        var registered = service.GetComponentInteractions().Keys.Select(key => key.ToString()).ToArray();
        _prefixes.ShouldEachConformTo(prefix => registered.Contains(prefix));
        _provider.GetServices<IComponentInteractionService>().Count().ShouldEqual(1);
    }

    void Destroy() => _provider.Dispose();
}
