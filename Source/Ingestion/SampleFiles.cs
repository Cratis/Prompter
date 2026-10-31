// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Ingestion;

/// <summary>Reads only curated public sample files, excluding sensitive content.</summary>
/// <param name="httpClient">The unauthenticated HTTP client.</param>
/// <param name="options">The ingestion configuration.</param>
/// <param name="logger">The diagnostic logger.</param>
public partial class SampleFiles(HttpClient httpClient, IOptions<PrompterOptions> options, ILogger<SampleFiles> logger)
{
    static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    [GeneratedRegex(@"BEGIN .*PRIVATE KEY|gh[pousr]_\w{16,}|github_pat_|sk-[\w-]{16,}|AKIA[0-9A-Z]{16}|(password|secret|api[_-]?key)\s*[:=]\s*[""']?[^\s""']{8,}", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SensitiveContent { get; }

    [GeneratedRegex("`+", RegexOptions.None, 1000)]
    private static partial Regex Backticks { get; }

    /// <summary>Wraps a sample with its path and, for code, a non-conflicting fence.</summary>
    /// <param name="path">The sample path.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The source Markdown.</returns>
    public static string Markdown(string path, string content)
    {
        if (Path.GetFileName(path).Equals("README.md", StringComparison.OrdinalIgnoreCase))
        {
            return $"# {path}\n\n{content}";
        }

        var language = Path.GetExtension(path) switch { ".cs" => "csharp", ".ts" or ".tsx" => "typescript", _ => string.Empty };
        var longest = Backticks.Matches(content).Select(match => match.Length).DefaultIfEmpty(0).Max();
        var fence = new string('`', Math.Max(3, longest + 1));
        return $"# {path}\n\n{fence}{language}\n{content}\n{fence}";
    }

    /// <summary>Enumerates the bounded allowlisted sample corpus.</summary>
    /// <param name="cancellationToken">Cancels the crawl.</param>
    /// <returns>The curated sample pages.</returns>
    /// <exception cref="IncompleteCrawl">A tree or raw file could not be fetched completely.</exception>
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Ingestion.Samples;
        var tree = await ReadTree(settings, cancellationToken);
        var paths = tree.Tree.Where(entry => entry.Type == "blob" && SampleAllowlist.Allows(entry.Path, settings.Paths, entry.Size, settings.MaxFileBytes))
            .OrderBy(entry => entry.Path, StringComparer.Ordinal).Take(settings.MaxFiles);
        foreach (var entry in paths)
        {
            var path = string.Join('/', entry.Path.Split('/').Select(Uri.EscapeDataString));
            var branch = Uri.EscapeDataString(settings.Branch);
            var content = await Fetch($"https://raw.githubusercontent.com/{settings.Repository}/{branch}/{path}", cancellationToken);
            if (LooksSensitive(content))
            {
                logger.SkippedSensitiveSample(entry.Path);
                continue;
            }

            if (Encoding.UTF8.GetByteCount(content) <= settings.MaxFileBytes)
            {
                yield return new(new PageUrl($"https://github.com/{settings.Repository}/blob/{branch}/{path}"), Markdown(entry.Path, content));
            }
        }
    }

    /// <summary>Detects secret shapes without exposing any matched value.</summary>
    /// <param name="content">The fetched file.</param>
    /// <returns>Whether the file must be excluded.</returns>
    internal static bool LooksSensitive(string content) => SensitiveContent.IsMatch(content);

    async Task<SampleTree> ReadTree(SamplesOptions settings, CancellationToken cancellationToken)
    {
        var json = await Fetch($"https://api.github.com/repos/{settings.Repository}/git/trees/{Uri.EscapeDataString(settings.Branch)}?recursive=1", cancellationToken);
        try
        {
            var tree = JsonSerializer.Deserialize<SampleTree>(json, _jsonOptions);
            return tree is { Truncated: false, Tree: not null } ? tree : throw new IncompleteCrawl();
        }
        catch (JsonException)
        {
            logger.FailedReadingSamples(null);
            throw new IncompleteCrawl();
        }
    }

    async Task<string> Fetch(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Cratis-Prompter");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.FailedReadingSamples((int)response.StatusCode);
                throw new IncompleteCrawl();
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception error) when (error is HttpRequestException || (error is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.FailedReadingSamples(null);
            throw new IncompleteCrawl();
        }
    }
}
