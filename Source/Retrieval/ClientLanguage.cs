// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Retrieval;

/// <summary>The client languages a question can name, used to keep secondary variant pages out of retrieval.</summary>
[Flags]
public enum ClientLanguage
{
    /// <summary>No client language is named.</summary>
    None = 0,

    /// <summary>The .NET / C# client.</summary>
    DotNet = 1,

    /// <summary>The TypeScript / JavaScript client.</summary>
    TypeScript = 2,

    /// <summary>The Kotlin / Java (JVM) client.</summary>
    Jvm = 4,

    /// <summary>The Elixir client.</summary>
    Elixir = 8,

    /// <summary>The Go client.</summary>
    Go = 16
}
