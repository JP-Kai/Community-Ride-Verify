using System.Net.Http.Json;

namespace RideSafeSA.Bot;

public class RideSafeApiClient(HttpClient httpClient)
{
    public async Task<CheckDriverResponse> CheckDriverAsync(string name, string licensePlate, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/drivers/check", new CheckDriverRequest(name, licensePlate), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckDriverResponse>(ct))!;
    }

    public async Task<SubmitReportResponse> SubmitReportAsync(SubmitReportRequest request, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync("/api/reports", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubmitReportResponse>(ct))!;
    }
}
