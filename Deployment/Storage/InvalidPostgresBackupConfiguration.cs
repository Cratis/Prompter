// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// The backup configuration is not explicitly pinned to approved tooling and object storage.
/// </summary>
public sealed class InvalidPostgresBackupConfiguration() : Exception("Postgres backups require a digest-pinned image, HTTPS endpoint, public /32 store address, bucket and age recipient.");
