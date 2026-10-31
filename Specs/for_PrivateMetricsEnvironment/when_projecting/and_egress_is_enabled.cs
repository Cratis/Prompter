// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Networking;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Networking.V1;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_PrivateMetricsEnvironment.when_projecting;

public class and_egress_is_enabled : given.an_approved_collector
{
    NetworkPolicyPeerArgs _peer = null!;
    NetworkPolicyPortArgs _port = null!;

    static Task<T> Value<T>(Input<T> input) => OutputUtilities.GetValueAsync((Output<T>)input);

    async Task Because()
    {
        var rule = PrivateMetricsEgress.Create(Collector with { BackendPort = 14318 })!;
        var peers = await Value(rule.To);
        var ports = await Value(rule.Ports);
        peers.Length.ShouldEqual(1);
        ports.Length.ShouldEqual(1);
        _peer = peers[0];
        _port = ports[0];
    }

    [Fact]
    async Task should_match_namespace_and_pod_identity_in_the_same_peer()
    {
        var namespaceSelector = await Value(_peer.NamespaceSelector!);
        var podSelector = await Value(_peer.PodSelector!);
        var namespaceLabels = await Value(namespaceSelector.MatchLabels);
        var podLabels = await Value(podSelector.MatchLabels);
        namespaceLabels.Count.ShouldEqual(1);
        namespaceLabels["kubernetes.io/metadata.name"].ShouldEqual(Collector.Namespace);
        podLabels.Count.ShouldEqual(2);
        podLabels["app"].ShouldEqual(Collector.App);
        podLabels["environment"].ShouldEqual(Collector.Environment);
        _peer.IpBlock.ShouldBeNull();
    }

    [Fact]
    async Task should_grant_only_the_verified_tcp_backend_port()
    {
        (await Value(_port.Protocol!)).ShouldEqual("TCP");
        var port = await Value(_port.Port!);
        port.AsT0.ShouldEqual(14318);
        _port.EndPort.ShouldBeNull();
    }
}
