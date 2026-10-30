// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// A crawl cannot safely replace the last-good corpus.
/// </summary>
public class IncompleteCrawl() : Exception("The documentation crawl was empty or incomplete; the previous corpus was preserved.");
