using System.Net.Http.Json;
using System.Text.Json;
using Recycler.Models.UpBank;

namespace Recycler.Services;

public class UpBankService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<HttpResponseMessage> PingAsync()
    {
        return await httpClient.GetAsync("util/ping");
    }

    public async Task<List<UpAccount>> GetAccountsAsync()
    {
        var response = await httpClient.GetFromJsonAsync<UpApiResponse<UpAccount>>("accounts", JsonOptions);
        return response?.Data ?? [];
    }

    public async Task<List<UpTransaction>> GetTransactionsAsync(
        string accountId,
        DateTimeOffset? since = null,
        DateTimeOffset? until = null,
        int pageSize = 100)
    {
        var query = new List<string>
        {
            $"page[size]={pageSize}"
        };

        if (since.HasValue)
            query.Add($"filter[since]={since.Value:o}");
        if (until.HasValue)
            query.Add($"filter[until]={until.Value:o}");

        var url = $"accounts/{accountId}/transactions?" + string.Join("&", query);

        var all = new List<UpTransaction>();

        while (url is not null)
        {
            var response = await httpClient.GetFromJsonAsync<UpApiResponse<UpTransaction>>(url, JsonOptions);
            if (response is null) break;

            all.AddRange(response.Data);
            url = response.Links?.Next;
        }

        return all;
    }
}
