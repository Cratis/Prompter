// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Cli;

/// <summary>
/// Reports a failed <c language="csharp">migrate</c> command as a message and an ordinary exit code.
/// </summary>
/// <remarks>
/// The command runs as PID 1 in the init container, where an unhandled exception ends as a segmentation
/// fault or a hang, hiding the cause; every failure therefore exits deliberately.
/// </remarks>
public static class MigrateFailure
{
    /// <summary>The process exit code for any failed migrate run.</summary>
    public const int ExitCode = 1;

    /// <summary>
    /// Renders the failure for standard error, naming the failure kind and redacting secret-shaped values.
    /// </summary>
    /// <param name="error">The failure that ended the migrate run.</param>
    /// <returns>The single line to write to standard error.</returns>
    public static string Message(Exception error) =>
        $"Migrate failed ({error.GetType().Name}): {SecretRedaction.Redact(error.Message).Text}";
}
