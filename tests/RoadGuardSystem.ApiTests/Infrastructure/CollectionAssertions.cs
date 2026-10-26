using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace RoadGuardSystem.ApiTests.Infrastructure;

internal static class CollectionAssertions
{
    internal static async Task<JsonElement> Contains(HttpClient client, string route, Guid id, params string[] exactFields)
    {
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "items", "nextCursor" }, page.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
        var row = Assert.Single(page.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
        if (exactFields.Length > 0) Assert.Equal(exactFields.OrderBy(x => x), row.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
        return row;
    }
}
