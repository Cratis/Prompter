// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Apps.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using K8sDeployment = Pulumi.Kubernetes.Apps.V1.Deployment;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>
/// The Prompter bot itself: a single-replica Deployment plus the ClusterIP Service the ingress routes to.
/// </summary>
/// <remarks>
/// Single replica is a requirement, not a sizing choice — the Discord gateway wants exactly one connection
/// per bot, so a second pod would double every answer. <c>Recreate</c> makes the rollout hand the gateway
/// over cleanly instead of briefly running two.
/// </remarks>
public sealed class PrompterDeployment
{
    /// <summary>
    /// The name of the workload and its Service.
    /// </summary>
    public const string Name = "prompter";

    /// <summary>
    /// The port Kestrel serves <c>/healthz</c> and <c>/reindex</c> on (the Dockerfile's EXPOSE).
    /// </summary>
    public const int Port = 8080;

    const string SecretName = "prompter-secrets";

    /// <summary>
    /// Initializes a new instance of the <see cref="PrompterDeployment"/> class.
    /// </summary>
    /// <param name="args">The arguments describing the deployment.</param>
    public PrompterDeployment(PrompterDeploymentArgs args)
    {
        var labels = new InputMap<string>
        {
            ["app"] = Name,
            ["environment"] = args.Environment,
        };

        // Every secret the bot needs, in one Secret. The keys are the configuration paths themselves, so the
        // mapping from `Cratis:Prompter:…` to environment variable is readable at a glance in `kubectl`.
        var secret = new Secret(
            $"{SecretName}-{args.Environment}",
            new SecretArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = SecretName,
                    Namespace = args.Namespace,
                    Labels = labels,
                },
                StringData = new InputMap<string>
                {
                    ["Cratis__Prompter__ConnectionString"] = args.ConnectionString,
                    ["Cratis__Prompter__Discord__Token"] = args.DiscordToken,
                    ["Cratis__Prompter__Anthropic__ApiKey"] = args.AnthropicApiKey,
                    ["Cratis__Prompter__Voyage__ApiKey"] = args.VoyageApiKey,
                    ["Cratis__Prompter__ReindexSecret"] = args.ReindexSecret,
                },
            },
            new CustomResourceOptions { Provider = args.Provider, DependsOn = [args.NamespaceResource] });

        var env = new List<EnvVarArgs>
        {
            new() { Name = "DOTNET_ENVIRONMENT", Value = "Production" },
        };

        if (!string.IsNullOrWhiteSpace(args.AskChannelId))
        {
            env.Add(new EnvVarArgs { Name = "Cratis__Prompter__Discord__AskChannelId", Value = args.AskChannelId });
        }

        if (!string.IsNullOrWhiteSpace(args.HelpForumChannelId))
        {
            env.Add(new EnvVarArgs { Name = "Cratis__Prompter__Discord__HelpForumChannelId", Value = args.HelpForumChannelId });
        }

        var container = new ContainerArgs
        {
            Name = Name,
            Image = args.Image,
            Ports = [new ContainerPortArgs { ContainerPortValue = Port, Name = "http" }],
            Env = env,
            EnvFrom = [new EnvFromSourceArgs { SecretRef = new SecretEnvSourceArgs { Name = SecretName } }],

            // Readiness uses /healthz, which checks the database *and* the gateway connection — exactly the
            // question "should traffic reach this pod". Liveness deliberately does not: /healthz reports
            // unhealthy during a Discord outage, and restarting the pod in a loop would neither fix Discord
            // nor let the re-index endpoint keep working. A TCP check asks the only question liveness
            // should ask — is the process still there.
            ReadinessProbe = new ProbeArgs
            {
                HttpGet = new HTTPGetActionArgs { Path = "/healthz", Port = Port },
                InitialDelaySeconds = 10,
                PeriodSeconds = 15,
                FailureThreshold = 3,
            },
            LivenessProbe = new ProbeArgs
            {
                TcpSocket = new TCPSocketActionArgs { Port = Port },
                InitialDelaySeconds = 30,
                PeriodSeconds = 30,
                FailureThreshold = 3,
            },
            Resources = new ResourceRequirementsArgs
            {
                Requests = { ["cpu"] = "100m", ["memory"] = "256Mi" },
                Limits = { ["cpu"] = "1", ["memory"] = "512Mi" },
            },
        };

        var deployment = new K8sDeployment(
            $"{Name}-{args.Environment}",
            new DeploymentArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = Name,
                    Namespace = args.Namespace,
                    Labels = labels,
                },
                Spec = new DeploymentSpecArgs
                {
                    Replicas = 1,
                    Selector = new LabelSelectorArgs { MatchLabels = labels },
                    Strategy = new DeploymentStrategyArgs { Type = "Recreate" },
                    Template = new PodTemplateSpecArgs
                    {
                        Metadata = new ObjectMetaArgs { Labels = labels },
                        Spec = new PodSpecArgs { Containers = [container] },
                    },
                },
            },
            new CustomResourceOptions
            {
                Provider = args.Provider,
                DependsOn = args.DependsOn.Concat([secret, args.NamespaceResource]),
            });

        Service = new Service(
            $"{Name}-service-{args.Environment}",
            new ServiceArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = Name,
                    Namespace = args.Namespace,
                    Labels = labels,
                },
                Spec = new ServiceSpecArgs
                {
                    Selector = labels,
                    Ports = [new ServicePortArgs { Port = Port, TargetPort = Port, Name = "http" }],
                },
            },
            new CustomResourceOptions { Provider = args.Provider, DependsOn = [deployment] });
    }

    /// <summary>
    /// Gets the Service the ingress routes to.
    /// </summary>
    public Service Service { get; }
}
