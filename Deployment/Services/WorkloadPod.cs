// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi.Kubernetes.Types.Inputs.Core.V1;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Applies restricted workload defaults independently of optional App credentials.</summary>
public static class WorkloadPod
{
    /// <summary>Appends bounded temporary storage without replacing credential or unrelated mounts.</summary>
    /// <param name="app">The optional external App key configuration.</param>
    /// <param name="container">The configured workload container.</param>
    /// <param name="initContainers">Init containers to run first; they receive the same hardening and temporary storage.</param>
    /// <returns>The unconditionally hardened workload pod.</returns>
    public static PodSpecArgs Create(GitHubAppArgs app, ContainerArgs container, params ContainerArgs[] initContainers)
    {
        var mounts = container.VolumeMounts;
        var pod = GitHubAppPod.Create(app, container);
        if (app.IsConfigured)
        {
            mounts.AddRange(container.VolumeMounts);
            container.VolumeMounts = mounts;
        }
        pod.AutomountServiceAccountToken = false;
        pod.SecurityContext = new PodSecurityContextArgs
        {
            RunAsNonRoot = true,
            RunAsUser = 1654,
            RunAsGroup = 1654,
            FsGroup = 1654,
            SeccompProfile = new SeccompProfileArgs { Type = "RuntimeDefault" }
        };
        pod.Volumes.Add(new VolumeArgs { Name = "tmp", EmptyDir = new EmptyDirVolumeSourceArgs { Medium = "Memory", SizeLimit = "64Mi" } });
        foreach (var hardened in initContainers.Prepend(container))
        {
            hardened.SecurityContext = new SecurityContextArgs
            {
                RunAsNonRoot = true,
                AllowPrivilegeEscalation = false,
                ReadOnlyRootFilesystem = true,
                Capabilities = new CapabilitiesArgs { Drop = ["ALL"] }
            };
            hardened.VolumeMounts.Add(new VolumeMountArgs { Name = "tmp", MountPath = "/tmp", ReadOnly = false });
        }

        if (initContainers.Length > 0)
        {
            pod.InitContainers = [.. initContainers];
        }

        return pod;
    }
}
