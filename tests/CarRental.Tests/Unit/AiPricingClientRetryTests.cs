using System.Diagnostics;
using System.Net;
using System.Text;
using CarRental.Application.Common;
using CarRental.Infrastructure.Ai;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CarRental.Tests.Unit;

/// <summary>AiPricingClient: retry co backoff khi 5xx, cuoi cung fallback, khong cho sau lan thu cuoi.</summary>
public sealed class AiPricingClientRetryTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses;
        public int Calls;
        public List<DateTime> Times { get; } = new();
        public StubHandler(params Func<HttpResponseMessage>[] responses) => _responses = new(responses);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Times.Add(DateTime.UtcNow);
            var next = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return Task.FromResult(next());
        }
    }

    private static HttpResponseMessage Status(HttpStatusCode code) => new(code);

    private static HttpResponseMessage Ok(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static AiPricingClient NewClient(StubHandler handler, int retryCount) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002/") },
            Options.Create(new AiSettings { RetryCount = retryCount, TimeoutSeconds = 5 }),
            new HttpContextAccessor(),
            NullLogger<AiPricingClient>.Instance);

    private static PredictPriceRequest Req() => new(1, 2, 5, 2, 700_000m, new DateTime(2030, 1, 1));

    [Fact]
    public async Task Persistent5xx_CallsRetryCountPlusOne_ThenFallback_WithBackoff()
    {
        var handler = new StubHandler(() => Status(HttpStatusCode.InternalServerError));
        var sw = Stopwatch.StartNew();

        var result = await NewClient(handler, retryCount: 2).PredictPriceAsync(Req());
        sw.Stop();

        Assert.Equal(3, handler.Calls);
        Assert.True(result.IsFallback);
        Assert.Equal(700_000m, result.PredictedPrice);
        // backoff 200ms + 400ms giua cac lan thu (cho sai so dong ho)
        Assert.True(handler.Times[1] - handler.Times[0] >= TimeSpan.FromMilliseconds(150));
        Assert.True(handler.Times[2] - handler.Times[1] >= TimeSpan.FromMilliseconds(300));
    }

    [Fact]
    public async Task NoDelayAfterLastAttempt()
    {
        var handler = new StubHandler(() => Status(HttpStatusCode.ServiceUnavailable));
        var sw = Stopwatch.StartNew();

        var result = await NewClient(handler, retryCount: 0).PredictPriceAsync(Req());
        sw.Stop();

        Assert.Equal(1, handler.Calls);
        Assert.True(result.IsFallback);
        Assert.True(sw.ElapsedMilliseconds < 150, $"Cho thua sau lan cuoi: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Recovers_AfterTransient5xx()
    {
        var handler = new StubHandler(
            () => Status(HttpStatusCode.BadGateway),
            () => Ok("{\"predicted_price_per_day\": 850000}"));

        var result = await NewClient(handler, retryCount: 2).PredictPriceAsync(Req());

        Assert.Equal(2, handler.Calls);
        Assert.False(result.IsFallback);
        Assert.Equal(850_000m, result.PredictedPrice);
    }

    [Fact]
    public async Task SuccessOnFirstCall_NoRetry()
    {
        var handler = new StubHandler(() => Ok("{\"predicted_price_per_day\": 900000}"));

        var result = await NewClient(handler, retryCount: 2).PredictPriceAsync(Req());

        Assert.Equal(1, handler.Calls);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public async Task InvalidJson_FallsBackWithoutRetry()
    {
        var handler = new StubHandler(() => Ok("not json"));

        var result = await NewClient(handler, retryCount: 2).PredictPriceAsync(Req());

        Assert.Equal(1, handler.Calls);
        Assert.True(result.IsFallback);
    }
}
