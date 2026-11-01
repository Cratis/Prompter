// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Cli;

/// <summary>
/// Reports a failed <c language="csharp">index</c> command as a message and an ordinary exit code.
/// </summary>
/// <remarks>
/// The container runs the process as PID 1, where the runtime's abort on an unhandled exception is ignored
/// by the kernel and ends as a segmentation fault (exit 139 on amd64) or a hang (arm64), hiding the real
/// cause. Every index failure therefore exits deliberately instead of escaping <c language="csharp">Main</c>.
/// </remarks>
public static class IndexFailure
{
    /// <summary>The process exit code for any failed index run.</summary>
    public const int ExitCode = 1;

    /// <summary>
    /// Renders the failure for standard error, naming the failure kind and redacting secret-shaped values.
    /// </summary>
    /// <param name="error">The failure that ended the index run.</param>
    /// <returns>The single line to write to standard error.</returns>
    public static string Message(Exception error) =>
        $"Index failed ({error.GetType().Name}): {SecretRedaction.Redact(error.Message).Text}";
}
