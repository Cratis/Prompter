// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Cluster;
using Cratis.Prompter.Deployment.Networking;
using Cratis.Prompter.Deployment.Services;
using Cratis.Prompter.Deployment.Storage;
using Pulumi;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;

// Prompter's slice of the UpCloud UKS cluster Studio's stack owns (decisions D-11 and D-15). Everything
// created here is namespaced: the cluster, its node group, the NGINX ingress controller, cert-manager and
// the `upcloud-maxiops` StorageClass all belong to Studio's stack and are referenced, never declared.
return await Deployment.RunAsync(() =>
{
    var config = new Config("prompter-deployment");

    var environment = config.Require("environment");
    var clusterId = config.Require("clusterId");
    var image = config.Require("prompterImage");
    var host = config.Require("ingressHost");
    var storageClassName = config.Get("storageClassName") ?? "upcloud-maxiops";
    var storageSizeGb = config.GetInt32("postgresStorageSizeGb") ?? 10;

    var postgresPassword = config.RequireSecret("postgresPassword");
    var discordToken = config.RequireSecret("discordToken");
    var anthropicApiKey = config.RequireSecret("anthropicApiKey");
    var voyageApiKey = config.RequireSecret("voyageApiKey");
    var reindexSecret = config.RequireSecret("reindexSecret");

    var askChannelId = config.Get("askChannelId");
    var helpForumChannelId = config.Get("helpForumChannelId");

    var namespaceName = $"prompter-{environment}";

    var cluster = new ExistingCluster(clusterId);
    var provider = cluster.CreateProvider(environment);

    var ns = new Namespace(
        $"namespace-{environment}",
        new NamespaceArgs
        {
            Metadata = new ObjectMetaArgs
            {
                Name = namespaceName,
                Labels = { ["environment"] = environment },
            },
        },
        new CustomResourceOptions { Provider = provider });

    var postgres = new PostgresDeployment(new PostgresDeploymentArgs
    {
        Provider = provider,
        Namespace = namespaceName,
        Environment = environment,
        NamespaceResource = ns,
        Password = postgresPassword,
        StorageClassName = storageClassName,
        StorageSizeGb = storageSizeGb,
    });

    var prompter = new PrompterDeployment(new PrompterDeploymentArgs
    {
        Provider = provider,
        Namespace = namespaceName,
        Environment = environment,
        NamespaceResource = ns,
        Image = image,
        ConnectionString = postgres.ConnectionString,
        DiscordToken = discordToken,
        AnthropicApiKey = anthropicApiKey,
        VoyageApiKey = voyageApiKey,
        ReindexSecret = reindexSecret,
        AskChannelId = askChannelId,
        HelpForumChannelId = helpForumChannelId,
        DependsOn = { postgres.Resource },
    });

    _ = new PrompterIngress(new PrompterIngressArgs
    {
        Provider = provider,
        Namespace = namespaceName,
        Environment = environment,
        Host = host,
        ServiceName = PrompterDeployment.Name,
        ServicePort = PrompterDeployment.Port,
        DependsOn = { prompter.Service },
    });

    return new Dictionary<string, object?>
    {
        ["namespace"] = namespaceName,
        ["image"] = image,
        ["reindexUrl"] = $"https://{host}/reindex",
    };
});
