using System.Net.Http;
using System.Threading.Tasks;

namespace TVB_Result_Exporter
{
    class GetTelemetry
    {
        private static readonly HttpClient _client;

        static GetTelemetry()
        {
            _client = new HttpClient();
        }

        // Connect to Telemetry using HttpClient.
        public async Task<string> GetTelemetryAsync(string uri)
        {
            try
            {
                return await _client.GetStringAsync(uri).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                throw;
            }
        }
    }
}
