// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using Pulumi.Kubernetes.Networking.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Pulumi.Kubernetes.Types.Inputs.Networking.V1;

namespace Cratis.Prompter.Deployment.Networking;

/// <summary>
/// Public routing for Prompter.
/// </summary>
/// <remarks>
/// The bot dials out to Discord, so nothing about answering needs to be reachable from the internet. The
/// one inbound caller is the Documentation build's re-index webhook, so the ingress exposes exactly that
/// path and nothing else — <c>/healthz</c> stays cluster-internal for the probes to use. The NGINX
/// controller and the <c>letsencrypt-prod</c> ClusterIssuer are cluster-scoped resources owned by Studio's
/// stack; this only references them by name.
/// </remarks>
public sealed class PrompterIngress
{
    /// <summary>
    /// The paths published to the internet. Adding the GitHub webhook (BACKLOG P-44) means adding its path
    /// here — everything else on the host stays unroutable.
    /// </summary>
    static readonly string[] _publicPaths = ["/reindex"];

    /// <summary>
    /// Initializes a new instance of the <see cref="PrompterIngress"/> class.
    /// </summary>
    /// <param name="args">The arguments describing the ingress.</param>
    public PrompterIngress(PrompterIngressArgs args)
    {
        var paths = _publicPaths.Select(path => new HTTPIngressPathArgs
        {
            Path = path,
            PathType = "Exact",
            Backend = new IngressBackendArgs
            {
                Service = new IngressServiceBackendArgs
                {
                    Name = args.ServiceName,
                    Port = new ServiceBackendPortArgs { Number = args.ServicePort },
                },
            },
        }).ToList();

        _ = new Ingress(
            $"prompter-ingress-{args.Environment}",
            new IngressArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = "prompter-ingress",
                    Namespace = args.Namespace,
                    Labels = { ["environment"] = args.Environment },
                    Annotations = new InputMap<string>
                    {
                        ["cert-manager.io/cluster-issuer"] = "letsencrypt-prod",

                        // The one caller posts an empty body a few times a day. A low ceiling costs
                        // nothing and caps how fast the shared secret can be guessed from outside.
                        ["nginx.ingress.kubernetes.io/limit-rps"] = "5",
                    },
                },
                Spec = new IngressSpecArgs
                {
                    IngressClassName = "nginx",
                    Tls =
                    [
                        new IngressTLSArgs
                        {
                            Hosts = [args.Host],
                            SecretName = $"prompter-tls-{args.Environment}",
                        },
                    ],
                    Rules =
                    [
                        new IngressRuleArgs
                        {
                            Host = args.Host,
                            Http = new HTTPIngressRuleValueArgs { Paths = paths },
                        },
                    ],
                },
            },
            new CustomResourceOptions { Provider = args.Provider, DependsOn = args.DependsOn });
    }
}
