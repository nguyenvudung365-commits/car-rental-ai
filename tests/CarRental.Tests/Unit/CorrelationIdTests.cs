using System.Net;
using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Infrastructure.Ai;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CarRental.Tests.Unit;

/// <summary>Correlation id: PredictPriceRequest.CorrelationId duoc uu tien, khong co thi lay header X-Correlation-Id cua request hien tai.</summary>
public sealed class CorrelationIdTests
{
    private const string Header = "X-Correlation-Id";

    private static PredictPriceRequest Req(string? cid = null) =>
        new(1, 2, 5, 3, 700_000, new DateTime(2030, 1, 1), cid);

    private static (AiPricingClient client, CapturingHandler handler) NewClient(string? httpContextHeader, int retry = 0)
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002/") };
        var accessor = new HttpContextAccessor();
        if (httpContextHeader != null)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[Header] = httpContextHeader;
            accessor.HttpContext = ctx;
        }
        var opts = Options.Create(new AiSettings { RetryCount = retry, TimeoutSeconds = 5 });
        return (new AiPricingClient(http, opts, accessor, NullLogger<AiPricingClient>.Instance), handler);
    }

    [Fact]
    public async Task AiClient_RequestCorrelationId_IsSentAsHeader()
    {
        var (client, handler) = NewClient(httpContextHeader: null);
        await client.PredictPriceAsync(Req("from-request"));
        Assert.Equal(new[] { "from-request" }, handler.Last!.Headers.GetValues(Header).ToArray());
    }

    [Fact]
    public async Task AiClient_RequestCorrelationId_WinsOverHttpContextHeader()
    {
        var (client, handler) = NewClient(httpContextHeader: "from-http");
        await client.PredictPriceAsync(Req("from-request"));
        Assert.Equal(new[] { "from-request" }, handler.Last!.Headers.GetValues(Header).ToArray());
    }

    [Fact]
    public async Task AiClient_NoRequestId_FallsBackToHttpContextHeader()
    {
        var (client, handler) = NewClient(httpContextHeader: "from-http");
        await client.PredictPriceAsync(Req(null));
        Assert.Equal(new[] { "from-http" }, handler.Last!.Headers.GetValues(Header).ToArray());
    }

    [Fact]
    public async Task AiClient_EmptyRequestId_FallsBackToHttpContextHeader()
    {
        var (client, handler) = NewClient(httpContextHeader: "from-http");
        await client.PredictPriceAsync(Req(""));
        Assert.Equal(new[] { "from-http" }, handler.Last!.Headers.GetValues(Header).ToArray());
    }

    [Fact]
    public async Task AiClient_NoIdAnywhere_SendsNoHeader_AndStillReturnsPrice()
    {
        var (client, handler) = NewClient(httpContextHeader: null);
        var result = await client.PredictPriceAsync(Req(null));
        Assert.False(handler.Last!.Headers.Contains(Header));
        Assert.Equal(850_000, result.PredictedPrice);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public async Task AiClient_Retry_SendsSameCorrelationIdEachAttempt()
    {
        var (client, handler) = NewClient(httpContextHeader: null, retry: 1);
        handler.FailFirst = 1;
        var result = await client.PredictPriceAsync(Req("cid-retry"));
        Assert.Equal(2, handler.Seen.Count);
        Assert.All(handler.Seen, v => Assert.Equal("cid-retry", v));
        Assert.False(result.IsFallback);
    }

    [Fact]
    public async Task BookingService_CreateAsync_PassesBookingCorrelationIdToAi()
    {
        var car = new Car
        {
            Id = 7, CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-1",
            Seats = 5, CarAgeYears = 2, BasePricePerDay = 700_000, Status = CarStatus.SanSang
        };
        var bookings = new FakeBookingRepo();
        var ai = new CapturingAi();
        var svc = new BookingService(bookings, new FakeCarRepo(car), ai, new FakeUow());

        var dto = await svc.CreateAsync(1, new CreateBookingRequest(
            7, DateTime.UtcNow.Date.AddDays(5), DateTime.UtcNow.Date.AddDays(7), null));

        Assert.False(string.IsNullOrEmpty(ai.Last!.CorrelationId));
        Assert.Equal(bookings.Added!.CorrelationId, ai.Last.CorrelationId);
        Assert.Equal(ai.Last.CorrelationId, dto.CorrelationId);
    }

    [Fact]
    public async Task BookingService_CreateAsync_UsesDifferentCorrelationIdPerBooking()
    {
        var car = new Car { Id = 7, CarType = CarType.Sedan, Seats = 5, BasePricePerDay = 700_000, Status = CarStatus.SanSang };
        var ai = new CapturingAi();
        var svc = new BookingService(new FakeBookingRepo(), new FakeCarRepo(car), ai, new FakeUow());
        var r = new CreateBookingRequest(7, DateTime.UtcNow.Date.AddDays(5), DateTime.UtcNow.Date.AddDays(7), null);

        await svc.CreateAsync(1, r);
        var first = ai.Last!.CorrelationId;
        await svc.CreateAsync(1, r);

        Assert.NotEqual(first, ai.Last!.CorrelationId);
    }

    [Fact]
    public async Task BookingService_GetByIdAsync_OtherCustomer_IsForbidden_Owner_Allowed()
    {
        // Controller Cancel dua vao GetByIdAsync(id, customerId) de chan khach huy don nguoi khac.
        var car = new Car { Id = 7, CarType = CarType.Sedan, Seats = 5, BasePricePerDay = 700_000, Status = CarStatus.SanSang };
        var svc = new BookingService(new FakeBookingRepo(), new FakeCarRepo(car), new CapturingAi(), new FakeUow());
        await svc.CreateAsync(1, new CreateBookingRequest(7, DateTime.UtcNow.Date.AddDays(5), DateTime.UtcNow.Date.AddDays(7), null));

        await Assert.ThrowsAsync<ForbiddenException>(() => svc.GetByIdAsync(1, 999));
        var dto = await svc.GetByIdAsync(1, 1);
        Assert.Equal(1, dto.CustomerId);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }
        public List<string?> Seen { get; } = new();
        public int FailFirst { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            Seen.Add(request.Headers.TryGetValues(Header, out var v) ? v.FirstOrDefault() : null);
            if (Seen.Count <= FailFirst)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"predicted_price_per_day\":850000}")
            });
        }
    }

    private sealed class CapturingAi : IAiPricingClient
    {
        public PredictPriceRequest? Last { get; private set; }
        public Task<PredictPriceResult> PredictPriceAsync(PredictPriceRequest request, CancellationToken cancellationToken = default)
        {
            Last = request;
            return Task.FromResult(new PredictPriceResult(900_000, false));
        }
    }

    private sealed class FakeUow : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default) => action();
    }

    private sealed class FakeCarRepo(Car car) : ICarRepository
    {
        public Task<Car?> GetByIdAsync(int id, bool includeImages = false) => Task.FromResult<Car?>(id == car.Id ? car : null);
        public Task<Car?> GetByLicensePlateAsync(string licensePlate) => throw new NotImplementedException();
        public Task<(List<Car> Items, int TotalCount)> GetFilteredAsync(CarFilterRequest filter) => throw new NotImplementedException();
        public Task AddAsync(Car c) => throw new NotImplementedException();
        public void Update(Car c) => throw new NotImplementedException();
        public void Delete(Car c) => throw new NotImplementedException();
        public Task LockForBookingAsync(int carId) => Task.CompletedTask;
    }

    private sealed class FakeBookingRepo : IBookingRepository
    {
        public Booking? Added { get; private set; }
        public Task<Booking?> GetByIdAsync(int id, bool includeDetails = false) => Task.FromResult(Added);
        public Task<(List<Booking> Items, int TotalCount)> GetFilteredAsync(BookingFilterRequest filter, int? customerId = null) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(int carId, DateTime startDate, DateTime endDate, int? excludeBookingId = null) => Task.FromResult(false);
        public Task<List<(DateTime Start, DateTime End)>> GetActiveRangesAsync(int carId, DateTime from) => throw new NotImplementedException();
        public Task AddAsync(Booking booking) { booking.Id = 1; Added = booking; return Task.CompletedTask; }
        public void Update(Booking booking) { }
        public Task<Payment?> GetPaymentByIdAsync(int id) => throw new NotImplementedException();
        public Task AddPaymentAsync(Payment payment) => throw new NotImplementedException();
        public Task AddReturnRecordAsync(ReturnRecord record) => throw new NotImplementedException();
    }
}
