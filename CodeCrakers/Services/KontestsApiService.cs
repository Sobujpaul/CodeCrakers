using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CodeCrakers.Services
{
    public class KontestsApiService : IDisposable
    {
        private readonly HttpClient _http;

        public KontestsApiService()
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(15);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CodeCrakers/1.0 (notifications)");
        }

        public async Task<List<ClistContest>> GetAllUpcomingAsync()
        {
            var list = new List<ClistContest>();
            try
            {
                var json = await _http.GetStringAsync("https://kontests.net/api/v1/all");
                var items = JsonConvert.DeserializeObject<List<KontestItem>>(json) ?? new List<KontestItem>();
                var nowUtc = DateTime.UtcNow;
                foreach (var it in items)
                {
                    // Parse start time
                    if (!DateTime.TryParse(it.start_time, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var start))
                        continue;
                    var startUtc = start.ToUniversalTime();
                    if (startUtc <= nowUtc) continue; // Only upcoming

                    // Duration may be seconds or hh:mm:ss; handle both
                    TimeSpan duration = TimeSpan.Zero;
                    if (!string.IsNullOrWhiteSpace(it.duration))
                    {
                        if (double.TryParse(it.duration, out var secs))
                        {
                            duration = TimeSpan.FromSeconds(Math.Max(0, secs));
                        }
                        else if (TimeSpan.TryParse(it.duration, out var ts))
                        {
                            duration = ts;
                        }
                    }

                    var platform = (it.site ?? string.Empty).Trim();

                    list.Add(new ClistContest
                    {
                        Name = it.name?.Trim() ?? "",
                        Platform = platform.ToLowerInvariant(),
                        StartTimeUtc = startUtc,
                        Duration = duration,
                        Url = it.url ?? ""
                    });
                }

                // Sort by start time
                list.Sort((a, b) => a.StartTimeUtc.CompareTo(b.StartTimeUtc));
            }
            catch
            {
                // swallow and return empty list
            }

            return list;
        }

        public void Dispose()
        {
            _http?.Dispose();
        }

        private class KontestItem
        {
            public string name { get; set; } = string.Empty;
            public string url { get; set; } = string.Empty;
            public string start_time { get; set; } = string.Empty;
            public string end_time { get; set; } = string.Empty;
            public string duration { get; set; } = string.Empty;
            public string site { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public string in_24_hours { get; set; } = string.Empty;
        }
    }
}
