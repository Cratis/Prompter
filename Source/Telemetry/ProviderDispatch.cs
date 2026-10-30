// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Telemetry;

/// <summary>
/// Metadata for one provider attempt. Operation id joins attempts, never users or messages.
/// </summary>
/// <param name="OperationId">Ephemeral logical-operation id.</param>
/// <param name="Provider">Fixed provider name.</param>
/// <param name="Account">Fixed logical account name.</param>
/// <param name="Task">Fixed task name.</param>
/// <param name="Attempt">One-based dispatch number.</param>
/// <param name="MaxInputTokens">Conservative input-token ceiling.</param>
/// <param name="MaxOutputTokens">Maximum output tokens.</param>
/// <param name="Model">Configured model identity for pricing.</param>
/// <param name="Effort">Bounded reasoning effort, empty for embeddings.</param>
public sealed record ProviderDispatch(Guid OperationId, string Provider, string Account, string Task, int Attempt, int MaxInputTokens, int MaxOutputTokens, string Model, string Effort);
