using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArchRender.Revit.Models;

namespace ArchRender.Revit.Services;

public class ApiClient
{
    private const string EdgeFunctionUrl =
        "https://klttztjdhaqtlgmctipe.supabase.co/functions/v1/render-from-plugin";

    private static readonly HttpClient Http = new();

    private readonly string _apiKey;

    public ApiClient(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<RenderResult> GenerateRenderAsync(
        byte[] imageBytes,
        RenderOptions options,
        CancellationToken ct = default)
    {
        var payload = new
        {
            imageBase64 = Convert.ToBase64String(imageBytes),
            renderType = options.RenderType,
            season = options.Season,
            timeOfDay = options.TimeOfDay,
            environment = options.Environment,
            aspectRatio = options.AspectRatio,
            materialDetails = options.MaterialDetails,
            useUltraModel = options.UseUltraModel,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, EdgeFunctionUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(payload);

        using var response = await Http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new ApiException((int)response.StatusCode, body);
        }

        var result = await response.Content.ReadFromJsonAsync<RenderResult>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        return result ?? throw new ApiException(0, "Empty response from server.");
    }
}

public class ApiException(int statusCode, string body) : Exception($"API error {statusCode}: {body}")
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
}
