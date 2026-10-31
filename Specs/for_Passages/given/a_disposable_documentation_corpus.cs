// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Specs;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Passages.given;

public class a_disposable_documentation_corpus : Specification
{
    protected const string PositiveQuestion = "What is Cratis Chronicle, according to the public documentation?";
    protected const string NegativeQuestion = "Cratis Chronicle guaranteed lottery numbers?";
    protected const string WeakQuestion = "Cratis Chronicle guaranteed lottery numbers terms?";
    protected const string LexicalOnlyQuestion = "Cratis Chronicle guaranteed lottery numbers glossary?";
    protected const string ChronicleContent = "Chronicle stores immutable facts and builds read models from events.";
    readonly string _schema = $"retrieval_{Guid.NewGuid():N}";
    NpgsqlDataSource? _database;

    protected static bool HasDatabase => Environment.GetEnvironmentVariable("PROMPTER_RETRIEVAL_TEST_PORT") is not null;
    protected IEmbeddingGenerator<string, Embedding<float>> embeddings { get; private set; } = null!;
    protected IChatClient chat { get; private set; } = null!;
    protected Answers answers { get; private set; } = null!;
    protected IReadOnlyList<Passage> retrieved { get; private set; } = [];
    protected IReadOnlyList<ChatMessage> sent { get; private set; } = [];

    protected static void RequireDatabase() => Assert.SkipUnless(HasDatabase, "Requires disposable CI pgvector in PROMPTER_RETRIEVAL_TEST_PORT.");

    protected static float[] QueryVector(Question question) => question.Value switch
    {
        PositiveQuestion => Vector(new(1, 0, 0, 0)),
        NegativeQuestion => Vector(new(0, 0, 0, 1)),
        WeakQuestion => Vector(new(0, 0, 0.2f, MathF.Sqrt(0.96f))),
        _ => Vector(new(0, 0, -1, 0))
    };

    static float[] Vector(Vector4 components)
    {
        var values = new float[1024];
        values[0] = components.X;
        values[1] = components.Y;
        values[2] = components.Z;
        values[3] = components.W;
        return values;
    }

    async Task Establish()
    {
        if (!HasDatabase)
        {
            return;
        }

        Assert.True(int.TryParse(Environment.GetEnvironmentVariable("PROMPTER_RETRIEVAL_TEST_PORT"), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var port) && port is > 0 and <= 65535, "Invalid disposable retrieval port.");
        var settings = new NpgsqlConnectionStringBuilder { Host = "127.0.0.1", Port = port, Username = "postgres", Database = "postgres", SearchPath = "public" };
        _database = NpgsqlDataSource.Create(settings);
        await VectorExtension.EnsureInstalled(_database);
        await Execute($"CREATE SCHEMA {_schema}");
        settings.SearchPath = $"{_schema},public";
        var builder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
        builder.UseVector();
        var mapped = builder.Build();
        await _database.DisposeAsync();
        _database = mapped;
        var chunks = new Chunks(_database);
        await chunks.EnsureSchema();
        await chunks.Upsert(new(new("chronicle"), new("https://cratis.io/chronicle/"), "Chronicle", "Overview", ChronicleContent, new("synthetic")), Vector(new(0.8f, 0.6f, 0, 0)));
        await chunks.Upsert(new(new("orthogonal"), new("https://cratis.io/other/"), "Other", "Unrelated", "An unrelated documentation excerpt.", new("synthetic")), Vector(new(0, 1, 0, 0)));
        await chunks.Upsert(new(new("glossary"), new("https://cratis.io/glossary/"), "Glossary", "Terms", "Glossary terms: Cratis Chronicle guaranteed lottery numbers.", new("synthetic")), Vector(new(0, 0, 1, 0)));
        for (var index = 0; index < 21; index++)
        {
            var third = (index + 1) * 0.01f;
            await chunks.Upsert(new(new($"filler_{index}"), new("https://cratis.io/filler/"), "Filler", "Other", "Unrelated context.", new("synthetic")), Vector(new(0, MathF.Sqrt(1 - (third * third)), third, 0)));
        }

        ConfigureAnswerer();
    }

    void ConfigureAnswerer()
    {
        embeddings = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        embeddings.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => new GeneratedEmbeddings<Embedding<float>>([new(QueryVector(new(call.Arg<IEnumerable<string>>().Single())))]));
        var real = new Passages(_database!, embeddings);
        var search = Substitute.For<IPassages>();
        search.Search(Arg.Any<Question>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns<Task<IEnumerable<Passage>>>(async call =>
        {
            retrieved = (await real.Search(call.Arg<Question>(), call.Arg<int>(), call.Arg<CancellationToken>())).ToArray();
            return retrieved;
        });
        chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            sent = call.Arg<IEnumerable<ChatMessage>>().ToArray();
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Chronicle stores facts [1]."));
        });
        var interactions = Substitute.For<IInteractionLog>();
        interactions.Record(Arg.Any<Interaction>(), Arg.Any<CancellationToken>()).Returns(7L);
        answers = new(search, chat, interactions, Options.Create(new PrompterOptions()), NullLogger<Answers>.Instance);
    }

    async Task Execute(string sql)
    {
        await using var command = _database!.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    async Task Destroy()
    {
        if (_database is not null)
        {
            try
            {
                await Execute($"DROP SCHEMA IF EXISTS {_schema} CASCADE");
            }
            finally
            {
                await _database.DisposeAsync();
            }
        }
    }
}
