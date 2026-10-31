// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

internal static partial class CorpusSourcesLogging
{
    [LoggerMessage(LogLevel.Warning, "Repository source {Source} failed ({Failure}); retaining its previous snapshot while indexing healthy sources")]
    internal static partial void RepositorySourceFailed(this ILogger<CorpusSources> logger, string source, string failure);
}
