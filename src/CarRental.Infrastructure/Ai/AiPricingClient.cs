using System.Net.Http.Json;
using System.Text.Json;
using CarRental.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarRental.Infrastructure.Ai;

public sealed class AiPricingClient : IAiPricingClient
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AiPricingClient> _logger;

    public AiPricingClient(
        HttpClient httpClient,
        IOptions<AiSettings> options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AiPricingClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<PredictPriceResult> PredictPriceAsync(
        PredictPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            car_type = request.CarType,
            car_age_years = request.CarAgeYears,
            seats = request.Seats,
            rental_days = request.RentalDays,
            base_price_per_day = request.BasePricePerDay,
            start_date = request.StartDate.ToString("yyyy-MM-dd")
        };

        for (var attempt = 0; attempt <= _settings.RetryCount; attempt++)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "predict-price")
                {
                    Content = JsonContent.Create(payload)
                };

                var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].ToString();
                if (!string.IsNullOrEmpty(correlationId))
                    httpRequest.Headers.Add("X-Correlation-Id", correlationId);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI pricing returned {Status} attempt {Attempt}", response.StatusCode, attempt + 1);
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("predicted_price", out var priceEl) && priceEl.TryGetDecimal(out var predicted))
                {
                    return new PredictPriceResult(predicted, IsFallback: false);
                }

                if (doc.RootElement.TryGetProperty("predictedPrice", out var priceEl2) && priceEl2.TryGetDecimal(out var predicted2))
                {
                    return new PredictPriceResult(predicted2, IsFallback: false);
                }

                _logger.LogWarning("AI pricing response missing predicted_price field");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "AI pricing timeout attempt {Attempt}", attempt + 1);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "AI pricing request failed attempt {Attempt}", attempt + 1);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "AI pricing invalid JSON");
                break;
            }

            if (attempt < _settings.RetryCount)
                await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)), cancellationToken);
        }

        return new PredictPriceResult(request.BasePricePerDay, IsFallback: true);
    }
}
