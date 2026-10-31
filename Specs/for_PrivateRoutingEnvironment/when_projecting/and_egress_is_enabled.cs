// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Networking;
using Pulumi;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_PrivateRoutingEnvironment.when_projecting;

public class and_egress_is_enabled : given.an_approved_destination
{
    static Task<T> Value<T>(Input<T> input) => OutputUtilities.GetValueAsync((Output<T>)input);

    [Fact]
    async Task should_grant_only_the_bot_the_exact_destination_and_verified_backend_port()
    {
        var policy = PrivateRoutingEgress.Create(Destination, "source-test")!;
        var source = await Value(policy.PodSelector);
        var sourceLabels = await Value(source.MatchLabels);
        sourceLabels.Count.ShouldEqual(2);
        sourceLabels["app"].ShouldEqual("prompter");
        sourceLabels["environment"].ShouldEqual("source-test");
        var rules = await Value(policy.Egress);
        rules.Length.ShouldEqual(1);
        var peers = await Value(rules[0].To);
        peers.Length.ShouldEqual(1);
        var peer = peers[0];
        var namespaceLabels = await Value((await Value(peer.NamespaceSelector!)).MatchLabels);
        var podLabels = await Value((await Value(peer.PodSelector!)).MatchLabels);
        namespaceLabels.Count.ShouldEqual(1);
        namespaceLabels["kubernetes.io/metadata.name"].ShouldEqual(Destination.Namespace);
        podLabels.Count.ShouldEqual(2);
        podLabels["app"].ShouldEqual(Destination.App);
        podLabels["environment"].ShouldEqual(Destination.Environment);
        peer.IpBlock.ShouldBeNull();
        var ports = await Value(rules[0].Ports);
        ports.Length.ShouldEqual(1);
        (await Value(ports[0].Protocol!)).ShouldEqual("TCP");
        (await Value(ports[0].Port!)).AsT0.ShouldEqual(8080);
        ports[0].EndPort.ShouldBeNull();
        new Uri(Destination.Endpoint).Port.ShouldEqual(80);
    }
}
