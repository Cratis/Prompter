// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_GitHubAppPod.when_assembling;

public class and_the_key_is_external
{
    static Task<T> Value<T>(Input<T> input) => OutputUtilities.GetValueAsync((Output<T>)input);

    [Fact]
    async Task should_project_only_one_readonly_key_for_the_nonroot_process()
    {
        var args = for_GitHubAppArgs.when_validating.and_inputs_are_explicit.Approved;
        var container = new ContainerArgs { Name = "prompter", Image = "offline" };
        var pod = GitHubAppPod.Create(args, container);
        var security = await Value(pod.SecurityContext!);
        (await Value(security.RunAsNonRoot!)).ShouldBeTrue();
        (await Value(security.RunAsUser!)).ShouldEqual(1654);
        (await Value(security.RunAsGroup!)).ShouldEqual(1654);
        (await Value(security.FsGroup!)).ShouldEqual(1654);
        var volumes = await Value(pod.Volumes);
        volumes.Length.ShouldEqual(1);
        var projection = await Value(volumes[0].Projected!);
        (await Value(projection.DefaultMode!)).ShouldEqual(288);
        var sources = await Value(projection.Sources);
        sources.Length.ShouldEqual(1);
        var secret = await Value(sources[0].Secret!);
        (await Value(secret.Name!)).ShouldEqual(args.ExternalSecretName);
        (await Value(secret.Optional!)).ShouldBeFalse();
        var items = await Value(secret.Items);
        items.Length.ShouldEqual(1);
        (await Value(items[0].Key)).ShouldEqual(args.KeyReference);
        (await Value(items[0].Path)).ShouldEqual("private-key.pem");
        (await Value(items[0].Mode!)).ShouldEqual(288);
        var mounts = await Value(container.VolumeMounts);
        mounts.Length.ShouldEqual(1);
        (await Value(mounts[0].ReadOnly!)).ShouldBeTrue();
        (await Value(mounts[0].MountPath)).ShouldEqual("/var/run/prompter/github-app");
    }

    [Fact]
    async Task should_leave_legacy_security_and_mounts_unchanged()
    {
        var pod = GitHubAppPod.Create(new(), new ContainerArgs { Name = "prompter" });
        pod.SecurityContext.ShouldBeNull();
        (await Value(pod.Volumes)).Length.ShouldEqual(0);
    }

    [Fact]
    async Task should_preserve_only_known_credential_references_without_duplicate_names()
    {
        var args = for_GitHubAppArgs.when_validating.and_inputs_are_explicit.Approved;
        var environment = GitHubAppPod.Environment(args, "workload-credentials").ToArray();
        var names = await Task.WhenAll(environment.Select(item => Value(item.Name)));
        names.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldEqual(names.Length);
        var references = environment.Where(item => item.ValueFrom is not null).ToArray();
        references.Length.ShouldEqual(8);
        foreach (var item in references)
        {
            var source = await Value(item.ValueFrom!);
            var reference = await Value(source.SecretKeyRef!);
            (await Value(reference.Name!)).ShouldEqual("workload-credentials");
            (await Value(reference.Key)).ShouldEqual(await Value(item.Name));
        }
        var legacy = GitHubAppPod.Environment(new(), "workload-credentials").ToArray();
        legacy.Length.ShouldEqual(8);
        (await Task.WhenAll(legacy.Select(item => Value(item.Name)))).Order(StringComparer.Ordinal)
            .SequenceEqual((await Task.WhenAll(references.Select(item => Value(item.Name)))).Order(StringComparer.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_revalidate_direct_construction() => Catch.Exception(() => GitHubAppPod.Create(new GitHubAppArgs { AppId = "123" }, new())).ShouldBeOfExactType<InvalidGitHubApp>();
}
