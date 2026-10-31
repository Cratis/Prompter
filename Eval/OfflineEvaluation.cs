// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Prompter.Eval.Scoring;

namespace Cratis.Prompter.Eval;

/// <summary>Deterministic synthetic parser/scoring/report exercise. No host, DB or provider is constructed.</summary>
internal static class OfflineEvaluation
{
    /// <summary>Scores checked-in synthetic inputs with the shared deterministic scorers.</summary>
    /// <param name="revision">Non-secret source revision.</param>
    /// <returns>A deterministic report, never paid quality evidence.</returns>
    public static EvalReport Run(string revision)
    {
        using var stream = typeof(OfflineEvaluation).Assembly.GetManifestResourceStream("offline-fixtures.json")!;
        using var reader = new StreamReader(stream);
        var fixture = reader.ReadToEnd();
        using var document = JsonDocument.Parse(fixture);
        var root = document.RootElement;
        var questions = GoldenSet.Parse(root.GetProperty("golden").GetString()!).Questions.ToArray();
        var responses = root.GetProperty("responses").EnumerateArray().ToArray();
        var results = questions.Select((question, index) =>
        {
            var response = responses[index];
            var refusal = response.GetProperty("refusal").GetBoolean();
            var citations = response.GetProperty("citations").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var score = response.GetProperty("judgeScore");
            return new QuestionResult(
                question.Id,
                question.Product,
                question.Type,
                question.ExpectsRefusal,
                refusal,
                RefusalAccuracy.IsCorrect(question.ExpectsRefusal, refusal),
                question.ExpectsRefusal ? null : CitationHit.IsHit(question.ExpectedPages, citations),
                score.ValueKind == JsonValueKind.Null ? null : score.GetDouble(),
                1,
                citations,
                null);
        }).ToArray();
        var provenance = new EvalProvenance("offline", "synthetic", Hash(fixture), Hash(root.GetProperty("corpus").GetString()!), revision, "canned-v1", "canned-score-v1", "none", "groundedness-1-to-5-v1");
        return new EvalReport(results, DateTimeOffset.UnixEpoch, provenance);
    }

    static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
