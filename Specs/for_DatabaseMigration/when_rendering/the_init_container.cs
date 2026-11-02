// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Storage;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_DatabaseMigration.when_rendering;

public class the_init_container
{
    [Fact]
    public async Task should_run_the_migrate_command_with_only_the_database_credentials()
    {
        var container = DatabaseMigration.Container("image:tag");
        string.Join(',', await Value(container.Args!)).ShouldEqual("migrate");
        var env = await Value(container.Env!);
        var names = await Task.WhenAll(env.Select(item => Value(item.Name)));
        string.Join(',', names).ShouldEqual(
            "PROMPTER_POSTGRES_ADMIN_PASSWORD,Cratis__Prompter__ConnectionString,Cratis__Prompter__Migration__ApplicationRole,Cratis__Prompter__Migration__ApplicationRolePassword,DOTNET_ENVIRONMENT");
        (await Value(container.EnvFrom)).Length.ShouldEqual(0);

        await AssertKey(env[0], "password");
        await AssertKey(env[3], "application-password");
        var connection = await Value(env[1].Value!);
        connection.ShouldContain("Username=prompter;");
        connection.ShouldContain("$(PROMPTER_POSTGRES_ADMIN_PASSWORD)");
        (await Value(env[2].Value!)).ShouldEqual("prompter_app");
    }

    static Task<T> Value<T>(Input<T> input) => OutputUtilities.GetValueAsync((Output<T>)input);

    static async Task AssertKey(EnvVarArgs variable, string key)
    {
        var source = await Value(variable.ValueFrom!);
        var selector = await Value(source.SecretKeyRef!);
        (await Value(selector.Name!)).ShouldEqual("prompter-postgres");
        (await Value(selector.Key)).ShouldEqual(key);
    }
}
