// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi.Kubernetes.Types.Inputs.Core.V1;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Pure PodSpec assembly for the single restricted external key projection.</summary>
public static class GitHubAppPod
{
    static readonly string[] _secretKeys =
    [
        "ConnectionString", "Discord__Token", "Anthropic__ApiKey", "Anthropic__SecondaryApiKey",
        "Voyage__ApiKey", "ReindexSecret", "GitHub__Token", "GitHub__WebhookSecret"
    ];

    /// <summary>Preserves known Secret mappings without importing unexpected keys as configuration.</summary>
    /// <param name="args">The explicit installation identity.</param>
    /// <param name="secretName">The workload's existing credential Secret name.</param>
    /// <returns>Unique explicit environment references and non-secret App metadata.</returns>
    public static IEnumerable<EnvVarArgs> Environment(GitHubAppArgs args, string secretName) =>
    [
        .. GitHubAppEnvironment.Values(args).Select(pair => new EnvVarArgs { Name = pair.Key, Value = pair.Value }),
        .. _secretKeys.Select(key => new EnvVarArgs
        {
            Name = $"Cratis__Prompter__{key}",
            ValueFrom = new EnvVarSourceArgs
            {
                SecretKeyRef = new SecretKeySelectorArgs { Name = secretName, Key = $"Cratis__Prompter__{key}" }
            }
        })
    ];

    /// <summary>Assembles the actual workload pod without reading or provisioning the external Secret.</summary>
    /// <param name="args">The explicit installation identity.</param>
    /// <param name="container">The workload container with its explicit runtime environment.</param>
    /// <returns>The pod specification; no App configuration leaves its security and volumes unchanged.</returns>
    /// <exception cref="InvalidGitHubApp">The configuration is incomplete or invalid.</exception>
    public static PodSpecArgs Create(GitHubAppArgs args, ContainerArgs container)
    {
        if (!args.IsValid) throw new InvalidGitHubApp();
        var pod = new PodSpecArgs { Containers = [container] };
        if (!args.IsConfigured) return pod;
        pod.SecurityContext = new PodSecurityContextArgs
        {
            RunAsNonRoot = true,
            RunAsUser = 1654,
            RunAsGroup = 1654,
            FsGroup = 1654
        };
        pod.Volumes =
        [
            new VolumeArgs
            {
                Name = "github-app-key",
                Projected = new ProjectedVolumeSourceArgs
                {
                    DefaultMode = 288,
                    Sources =
                    [
                        new VolumeProjectionArgs
                        {
                            Secret = new SecretProjectionArgs
                            {
                                Name = args.ExternalSecretName,
                                Optional = false,
                                Items = [new KeyToPathArgs { Key = args.KeyReference, Path = "private-key.pem", Mode = 288 }]
                            }
                        }
                    ]
                }
            }
        ];
        container.VolumeMounts =
        [
            new VolumeMountArgs
            {
                Name = "github-app-key", MountPath = "/var/run/prompter/github-app", ReadOnly = true
            }
        ];
        return pod;
    }
}
