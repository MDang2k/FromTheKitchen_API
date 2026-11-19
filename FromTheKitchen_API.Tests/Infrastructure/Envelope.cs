using System.Net.Http.Json;

namespace FromTheKitchen_API.Tests.Infrastructure;

/// <summary>Typed view of the API's ApiResponse wrapper, so tests can read Result without casting.</summary>
public sealed record Envelope<T>(int StatusCode, bool IsSuccess, List<string> ErrorMessages, T? Result);

public static class HttpResponseExtensions
{
    public static async Task<Envelope<T>> ReadEnvelopeAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Envelope<T>>())
        ?? throw new InvalidOperationException($"Empty body from {response.RequestMessage?.RequestUri}");
}
