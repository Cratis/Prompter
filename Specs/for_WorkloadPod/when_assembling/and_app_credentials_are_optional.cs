// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_WorkloadPod.when_assembling;

public class and_app_credentials_are_optional
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_always_render_restricted_security_with_bounded_tmp(bool configured)
    {
        var args = configured ? for_GitHubAppArgs.when_validating.and_inputs_are_explicit.Approved : new GitHubAppArgs();
        var container = new ContainerArgs { Name = "prompter", VolumeMounts = [new VolumeMountArgs { Name = "unrelated", MountPath = "/data", ReadOnly = true }] };
        var pod = WorkloadPod.Create(args, container);
        await AssertPod(pod);
        await AssertContainer(container);
        await AssertTmp(pod, container);
        var mounts = await Value(container.VolumeMounts);
        mounts.Length.ShouldEqual(configured ? 3 : 2);
        (await Value(mounts[0].Name)).ShouldEqual("unrelated");
        (await Value(mounts[0].MountPath)).ShouldEqual("/data");
        (await Value(pod.Volumes)).Length.ShouldEqual(configured ? 2 : 1);
        if (configured) await AssertKey(pod, container);
    }

    static Task<T> Value<T>(Input<T> input) => OutputUtilities.GetValueAsync((Output<T>)input);

    static async Task AssertPod(PodSpecArgs pod)
    {
        (await Value(pod.AutomountServiceAccountToken!)).ShouldBeFalse();
        var security = await Value(pod.SecurityContext!);
        (await Value(security.RunAsNonRoot!)).ShouldBeTrue();
        (await Value(security.RunAsUser!)).ShouldEqual(1654);
        (await Value(security.RunAsGroup!)).ShouldEqual(1654);
        (await Value(security.FsGroup!)).ShouldEqual(1654);
        var seccomp = await Value(security.SeccompProfile!);
        (await Value(seccomp.Type)).ShouldEqual("RuntimeDefault");
    }

    static async Task AssertContainer(ContainerArgs container)
    {
        var security = await Value(container.SecurityContext!);
        (await Value(security.RunAsNonRoot!)).ShouldBeTrue();
        (await Value(security.AllowPrivilegeEscalation!)).ShouldBeFalse();
        (await Value(security.ReadOnlyRootFilesystem!)).ShouldBeTrue();
        var capabilities = await Value(security.Capabilities!);
        (await Value(capabilities.Drop)).ShouldContain("ALL");
    }

    static async Task AssertKey(PodSpecArgs pod, ContainerArgs container)
    {
        var volumes = await Value(pod.Volumes);
        var projection = await Value(volumes[0].Projected!);
        (await Value(projection.DefaultMode!)).ShouldEqual(288);
        (await Value(projection.Sources)).Length.ShouldEqual(1);
        var mounts = await Value(container.VolumeMounts);
        (await Value(mounts[1].Name)).ShouldEqual("github-app-key");
        (await Value(mounts[1].ReadOnly!)).ShouldBeTrue();
    }

    static async Task AssertTmp(PodSpecArgs pod, ContainerArgs container)
    {
        var volumes = await Value(pod.Volumes);
        var names = await Task.WhenAll(volumes.Select(volume => Value(volume.Name)));
        var tmp = volumes[Array.IndexOf(names, "tmp")];
        var directory = await Value(tmp.EmptyDir!);
        (await Value(directory.SizeLimit!)).ShouldEqual("64Mi");
        var mounts = await Value(container.VolumeMounts);
        var mountNames = await Task.WhenAll(mounts.Select(item => Value(item.Name)));
        var mount = mounts[Array.IndexOf(mountNames, "tmp")];
        (await Value(mount.MountPath)).ShouldEqual("/tmp");
        (await Value(mount.ReadOnly!)).ShouldBeFalse();
    }
}
