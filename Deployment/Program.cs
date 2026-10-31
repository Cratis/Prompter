// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Networking;
using Cratis.Prompter.Deployment.Services;
using Cratis.Prompter.Deployment.Storage;
using Pulumi;
using Pulumi.Random;
using K8sProviderArgs = Pulumi.Kubernetes.ProviderArgs;
using KubernetesProvider = Pulumi.Kubernetes.Provider;

// Prompter's slice of the shared UpCloud UKS cluster. The cluster, the `prompter` namespace, the baseline
// network policy and the namespace-scoped deploy identity this stack runs as all belong to
// Cratis/Infrastructure's Core stack (AppAccess). This stack holds only resources inside that namespace:
// it never creates the namespace, and the `upcloud-maxiops` StorageClass is referenced, never declared.
// There is no public route; re-indexing runs as an on-demand or nightly Job.
return await Deployment.RunAsync(() =>
{
    var config = new Config("prompter-deployment");
    var readiness = AnsweringReadinessArgs.From(config);
    var metrics = PrivateMetricsArgs.From(config);
    if (!metrics.IsValid) throw new InvalidPrivateMetrics();

    var environment = config.Require("environment");
    var image = config.Require("prompterImage");
    var storageClassName = config.Get("storageClassName") ?? "upcloud-maxiops";
    var storageSizeGb = config.GetInt32("postgresStorageSizeGb") ?? 10;

    // Secrets arrive as environment variables from GitHub secrets, never from stack config, so nobody has to
    // run `pulumi config set` locally. GitHub secret names cannot start with GITHUB_, hence the PROMPTER_ prefix.
    var discordToken = Output.CreateSecret(RequireEnv("PROMPTER_DISCORD_TOKEN"));
    var anthropicApiKey = Output.CreateSecret(RequireEnv("PROMPTER_ANTHROPIC_API_KEY"));
    var anthropicSecondaryApiKey = OptionalSecretEnv("PROMPTER_ANTHROPIC_SECONDARY_API_KEY");
    var voyageApiKey = Output.CreateSecret(RequireEnv("PROMPTER_VOYAGE_API_KEY"));
    var reindexSecret = Output.CreateSecret(RequireEnv("PROMPTER_REINDEX_SECRET"));

    // The GitHub credentials are optional: an empty token leaves issue filing off and an empty webhook
    // secret makes the webhook endpoint refuse everything, so a deployment that sets neither behaves
    // exactly as it did before the tracker bridge existed (decision D-16).
    var gitHubToken = OptionalSecretEnv("PROMPTER_GITHUB_TOKEN");
    var gitHubWebhookSecret = OptionalSecretEnv("PROMPTER_GITHUB_WEBHOOK_SECRET");
    var answeringRepositories = config.Get("answeringRepositories");
    var issueNotifyChannelId = config.Get("issueNotifyChannelId");

    var askChannelId = config.Get("askChannelId");
    var helpForumChannelId = config.Get("helpForumChannelId");

    // The namespace is owned by Core. The name is a constant, and the Core output is read as a guard that
    // fails the deployment when Infrastructure has not onboarded Prompter yet.
    const string namespaceName = "prompter";
    var core = new StackReference($"organization/core-deployment/{environment}");
    var coreNamespace = core.RequireOutput("prompterNamespace").Apply(value =>
        $"{value}" == namespaceName
            ? namespaceName
            : throw new InvalidOperationException($"Core reports the Prompter namespace as '{value}', expected '{namespaceName}'."));

    var provider = new KubernetesProvider(
        "k8s",
        new K8sProviderArgs { KubeConfig = Output.CreateSecret(RequireEnv("PROMPTER_KUBECONFIG")) });

    // Generated once and then stable in state, so no person ever has to choose or hand over this password.
    var postgresPassword = new RandomPassword(
        $"postgres-password-{environment}",
        new RandomPasswordArgs { Length = 32, Special = false });

    var postgres = new PostgresDeployment(new PostgresDeploymentArgs
    {
        Provider = provider,
        Namespace = namespaceName,
        Environment = environment,
        Password = postgresPassword.Result,
        StorageClassName = storageClassName,
        StorageSizeGb = storageSizeGb,
    });

    _ = new PrompterDeployment(new PrompterDeploymentArgs
    {
        Provider = provider,
        Namespace = namespaceName,
        Environment = environment,
        Image = image,
        MetricsEnabled = metrics.Enabled,
        Metrics = metrics,
        ConnectionString = postgres.ConnectionString,
        DiscordToken = discordToken,
        AnthropicApiKey = anthropicApiKey,
        AnthropicSecondaryApiKey = anthropicSecondaryApiKey,
        Readiness = readiness,
        VoyageApiKey = voyageApiKey,
        ReindexSecret = reindexSecret,
        AskChannelId = askChannelId,
        HelpForumChannelId = helpForumChannelId,
        GitHubToken = gitHubToken,
        GitHubWebhookSecret = gitHubWebhookSecret,
        AnsweringRepositories = answeringRepositories,
        IssueNotifyChannelId = issueNotifyChannelId,
        DependsOn = { postgres.Resource },
    });

    _ = new PrompterEgress(new PrompterEgressArgs
    {
        Metrics = metrics,
        Provider = provider,
        Namespace = namespaceName,
        Environment = environment,
    });

    // No schedule, credentials, image or network grant exists unless deliberately configured.
    if (config.GetBoolean("postgresBackupsEnabled") == true)
    {
        var backupArgs = new PostgresBackupsArgs
        {
            Namespace = namespaceName,
            Environment = environment,
            Image = config.Require("postgresBackupImage"),
            Endpoint = config.Require("postgresBackupEndpoint"),
            StoreCidr = config.Require("postgresBackupStoreCidr"),
            Bucket = config.Require("postgresBackupBucket"),
            Recipient = config.Require("postgresBackupRecipient"),
        };
        _ = new PostgresBackups(backupArgs, provider);
    }

    return new Dictionary<string, object?>
    {
        ["namespace"] = coreNamespace,
        ["image"] = image,
    };
});

static string RequireEnv(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Required environment variable '{name}' is not set. See Deployment/README.md.");

static Output<string> OptionalSecretEnv(string name) =>
    Output.CreateSecret(Environment.GetEnvironmentVariable(name) ?? string.Empty);
