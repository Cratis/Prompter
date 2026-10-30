// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.given;

public class a_classifier : Specification
{
    HttpClient _client = null!;

    protected RoutingOptions options { get; } = new()
    {
        Endpoint = new("http://laya.internal:8080"),
        AllowedHost = "laya.internal"
    };
    protected LayaClassifier classifier { get; private set; } = null!;
    protected Classification result { get; set; } = null!;
    private protected recording_sink sink { get; } = new();
    private protected fake_decisions transport { get; } = new();

    protected Task Classify(string text = "Arc is broken") => ClassifyRequest(text);

    void Establish()
    {
        _client = new(transport);
        classifier = new(_client, options, ["arc", "chronicle"], sink);
    }

    void Destroy() => _client.Dispose();

    async Task ClassifyRequest(string text) => result = await classifier.Classify(text, []);

    internal sealed class recording_sink : IClassificationSink
    {
        public ICollection<ClassificationObservation> Observations { get; } = [];
        public void Record(ClassificationObservation observation) => Observations.Add(observation);
    }

    internal sealed class fake_decisions : HttpMessageHandler
    {
        public Func<string, string> Change { get; set; } = text => text;
        public Func<CancellationToken, Task>? Delay { get; set; }
        public Func<string, HttpContent> Content { get; set; } = body => new StringContent(body, Encoding.UTF8, "application/json");
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public int Calls { get; private set; }
        public string Body { get; private set; } = string.Empty;
        public Uri? Address { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Address = request.RequestUri;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            if (Delay is not null) await Delay(cancellationToken);
            using var document = JsonDocument.Parse(Body);
            var results = document.RootElement.GetProperty("decisions").EnumerateArray().Select(decision => new
            {
                requestId = decision.GetProperty("requestId").GetString(),
                choices = decision.GetProperty("choices").EnumerateArray().Select(choice => choice.GetString()!).ToDictionary(
                    choice => choice,
                    choice => choice == "arc" || choice == "bug" || choice == "offer-issue" ? 0.9 : 0.1 / (decision.GetProperty("choices").GetArrayLength() - 1)),
                model = "synthetic-fixture",
                engine = "laya-choice/v1",
                latencyMs = 1
            });
            return new(Status) { Content = Content(Change(JsonSerializer.Serialize(new { results }))) };
        }
    }
}
