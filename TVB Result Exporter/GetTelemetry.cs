using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                // Connect to telemetry
                // Added a "ConfigureAwait(false)" to avoid deadlock.
                //HttpResponseMessage response = await _client.GetAsync(uri).ConfigureAwait(false);
                //response.EnsureSuccessStatusCode();
                //return response;

                // If we don't need headers, just this one line.
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
