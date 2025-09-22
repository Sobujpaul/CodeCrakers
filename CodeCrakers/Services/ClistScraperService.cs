using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeCrakers.Services
{
    public class ClistContest
    {
        public string Name { get; set; }
        public string Platform { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public TimeSpan Duration { get; set; }
        public string Url { get; set; }
    }

    public class ClistScraperService : IDisposable
    {
        private readonly HttpClient _httpClient;

        public ClistScraperService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CodeCrakers/1.0");
        }

        public async Task<List<ClistContest>> GetUpcomingContestsAsync()
        {
            var contests = new List<ClistContest>();
            try
            {
                var html = await _httpClient.GetStringAsync("https://clist.by/?view=list");

                // Find rows with name links and surrounding time/duration data.
                var rowRegex = new Regex(
                    "<a[^>]+href=\"(?<url>https?:\\/\\/[^\\\"]*clist\\.by[^\\\"]*)\"[^>]*>(?<name>[^<]{3,})<\\/a>",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

                var timeRegex = new Regex("fixedtime\\.html\\?iso=(?<iso>\\d{8}T\\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var platformRegex = new Regex("resource/([a-z0-9_.-]+)/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var durationRegex = new Regex(">\\s*(?<dur>(\\d+\\s*days?\\s*)?(\\d{1,2}:\\d{2}))\\s*<", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                foreach (Match m in rowRegex.Matches(html))
                {
                    var name = System.Net.WebUtility.HtmlDecode(m.Groups["name"].Value.Trim());
                    var url = m.Groups["url"].Value;

                    int idx = m.Index;
                    int start = Math.Max(0, idx - 600);
                    int len = Math.Min(html.Length - start, 1800);
                    var window = html.Substring(start, len);

                    DateTime startUtc = DateTime.MinValue;
                    var timeMatch = timeRegex.Match(window);
                    if (timeMatch.Success)
                    {
                        var iso = timeMatch.Groups["iso"].Value; // yyyyMMddTHHmm
                        if (DateTime.TryParseExact(iso, "yyyyMMdd'T'HHmm", null,
                            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                            out var dt))
                        {
                            startUtc = dt;
                        }
                    }

                    TimeSpan duration = TimeSpan.Zero;
                    var dmatch = durationRegex.Match(window);
                    if (dmatch.Success)
                    {
                        var ds = dmatch.Groups["dur"].Value.Trim();
                        int days = 0;
                        var dayMatch = Regex.Match(ds, @"^(?<days>\d+)\s*days?\s*(?<rest>\d{1,2}:\d{2})$", RegexOptions.IgnoreCase);
                        if (dayMatch.Success)
                        {
                            days = int.Parse(dayMatch.Groups["days"].Value);
                            ds = dayMatch.Groups["rest"].Value;
                        }
                        if (TimeSpan.TryParse(ds, out var hhmm))
                        {
                            duration = TimeSpan.FromDays(days) + hhmm;
                        }
                    }

                    string platform = null;
                    var pmatch = platformRegex.Match(window);
                    if (pmatch.Success)
                    {
                        platform = pmatch.Groups[1].Value;
                    }

                    if (startUtc > DateTime.UtcNow.AddMinutes(-5))
                    {
                        contests.Add(new ClistContest
                        {
                            Name = name,
                            Platform = platform,
                            StartTimeUtc = startUtc,
                            Duration = duration,
                            Url = url
                        });
                    }
                }

                contests.Sort((a, b) => a.StartTimeUtc.CompareTo(b.StartTimeUtc));
            }
            catch
            {
                // ignore, return partial/empty list
            }

            return contests;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}


