using System.Net;
using CarRental.Tests.Support;

namespace CarRental.Tests.Integration;

public sealed class PublicMediaApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [LocalDbFact]
    public async Task Get_HandlesPublicImagesAndKeepsPrivateImagesOutOfPublicRoute()
    {
        using var client = fixture.Factory!.CreateClient();

        foreach (var key in new[] { "cars/car.webp", "avatars/1/avatar.webp" })
        {
            using var response = await client.GetAsync($"/api/media/{key}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/webp", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(WebpBytes.Tiny, await response.Content.ReadAsByteArrayAsync());
        }

        using var privateImage = await client.GetAsync("/api/media/private/documents/1/document.webp");
        Assert.Equal(HttpStatusCode.NotFound, privateImage.StatusCode);
    }
}
