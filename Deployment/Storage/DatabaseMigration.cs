// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// The init container that applies migrations and provisions the application role as the bootstrap role,
/// so the running workload never needs that role's credentials.
/// </summary>
public static class DatabaseMigration
{
    /// <summary>
    /// The Kubernetes variable that carries the bootstrap role's password into the connection string.
    /// </summary>
    public const string AdminPasswordVariable = "PROMPTER_POSTGRES_ADMIN_PASSWORD";

    /// <summary>
    /// Creates the init container.
    /// </summary>
    /// <param name="image">The workload image, so the schema is migrated by the version that will run.</param>
    /// <returns>The init container.</returns>
    public static ContainerArgs Container(Input<string> image) => new()
    {
        Name = "migrate",
        Image = image,
        Args = ["migrate"],
        Env =
        [
            SecretValue(AdminPasswordVariable, PostgresDeployment.PasswordKey),
            new EnvVarArgs
            {
                Name = "Cratis__Prompter__ConnectionString",
                Value = PostgresConnection.For(PostgresDeployment.UserName, $"$({AdminPasswordVariable})"),
            },
            new EnvVarArgs { Name = "Cratis__Prompter__Migration__ApplicationRole", Value = PostgresDeployment.ApplicationUserName },
            SecretValue("Cratis__Prompter__Migration__ApplicationRolePassword", PostgresDeployment.ApplicationPasswordKey),
            new EnvVarArgs { Name = "DOTNET_ENVIRONMENT", Value = "Production" },
        ],
        Resources = new ResourceRequirementsArgs
        {
            Requests = { ["cpu"] = "50m", ["memory"] = "128Mi" },
            Limits = { ["cpu"] = "500m", ["memory"] = "256Mi" },
        },
    };

    static EnvVarArgs SecretValue(string name, string key) => new()
    {
        Name = name,
        ValueFrom = new EnvVarSourceArgs
        {
            SecretKeyRef = new SecretKeySelectorArgs { Name = PostgresDeployment.SecretName, Key = key },
        },
    };
}
