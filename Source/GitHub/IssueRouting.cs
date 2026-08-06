// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Decides which repository an issue belongs in, from the product a draft names.
/// </summary>
/// <remarks>
/// Q-7 settled on the owning product repository rather than one shared inbox, which means a wrong guess puts
/// a report in front of the wrong maintainers. Routing therefore never invents a repository: an unknown or
/// missing product falls back to the configured default, and the reporter sees the target in the preview and
/// can change it before anything is filed.
/// </remarks>
public static class IssueRouting
{
    /// <summary>
    /// Resolves the repository for a product.
    /// </summary>
    /// <param name="product">The product named by the draft, which may be empty or unrecognized.</param>
    /// <param name="options">The GitHub options carrying the routing table and the default.</param>
    /// <returns>The repository name, without the owner.</returns>
    public static string RepositoryFor(string? product, GitHubOptions options)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            return options.DefaultRepository;
        }

        return options.Repositories.TryGetValue(product.Trim(), out var repository)
            ? repository
            : options.DefaultRepository;
    }

    /// <summary>
    /// Determines whether a product routes to a repository of its own, as opposed to falling back to the
    /// default. The preview says so explicitly when it does not, because "we could not tell which product
    /// this is" is something the reporter can fix in one click and nobody else can.
    /// </summary>
    /// <param name="product">The product named by the draft.</param>
    /// <param name="options">The GitHub options carrying the routing table.</param>
    /// <returns><see langword="true"/> when the product is recognized; otherwise <see langword="false"/>.</returns>
    public static bool IsRouted(string? product, GitHubOptions options) =>
        !string.IsNullOrWhiteSpace(product) && options.Repositories.ContainsKey(product.Trim());

    /// <summary>
    /// Lists the repositories an issue may be filed in, for the reporter to choose from when the routing is
    /// wrong.
    /// </summary>
    /// <param name="options">The GitHub options carrying the routing table and the default.</param>
    /// <returns>The distinct repository names, ordered, with the default first.</returns>
    public static IReadOnlyList<string> Choices(GitHubOptions options) =>
        [
            options.DefaultRepository,
            .. options.Repositories.Values
                .Where(repository => !string.Equals(repository, options.DefaultRepository, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
        ];
}
