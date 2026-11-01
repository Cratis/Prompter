// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Hosting;

/// <summary>Reports bot startup and run failures on stderr with an ordinary exit code.</summary>
/// <remarks>Unhandled exceptions can abort PID 1 with exit 139 or a hang, hiding the real cause.</remarks>
public static class BotFailure
{
    /// <summary>The process exit code for a failed bot.</summary>
    public const int ExitCode = 1;

    /// <summary>Renders the failure kind and redacted message.</summary>
    /// <param name="error">The escaping failure.</param>
    /// <returns>The line for standard error.</returns>
    public static string Message(Exception error) =>
        $"Bot failed ({error.GetType().Name}): {SecretRedaction.Redact(error.Message).Text}";

    /// <summary>Runs bot startup and lifetime without letting failures abort PID 1.</summary>
    /// <param name="bot">The bot startup and lifetime.</param>
    /// <param name="error">The error output.</param>
    /// <returns>Zero on completion, otherwise one.</returns>
    public static async Task<int> Run(Func<Task> bot, TextWriter error)
    {
        try
        {
            await bot();
            return 0;
        }
        catch (Exception failure)
        {
            await error.WriteLineAsync(Message(failure));
            return ExitCode;
        }
    }
}
