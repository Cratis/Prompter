// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Cli;

/// <summary>Parses index-only flags without passing boolean switches to configuration binding.</summary>
public static class IndexArguments
{
    const string ConfirmationPrefix = "--confirm-full-reindex=";

    /// <summary>Parses and validates the operator's requested mode.</summary>
    /// <param name="args">The CLI arguments.</param>
    /// <returns>The authorized request.</returns>
    public static IndexRequest Parse(string[] args)
    {
        var confirmation = args.FirstOrDefault(argument => argument.StartsWith(ConfirmationPrefix, StringComparison.Ordinal))?[ConfirmationPrefix.Length..] ?? string.Empty;
        var request = new IndexRequest(args.Contains("--full"), confirmation, args.Contains("--scheduled"));
        request.Validate();
        return request;
    }

    /// <summary>Preserves existing configuration overrides, excluding index-only switches.</summary>
    /// <param name="args">The CLI arguments.</param>
    /// <returns>The configuration arguments.</returns>
    public static string[] ConfigurationArguments(string[] args) => args.Where(argument => argument is not "--full" and not "--scheduled" && !argument.StartsWith(ConfirmationPrefix, StringComparison.Ordinal)).ToArray();
}
