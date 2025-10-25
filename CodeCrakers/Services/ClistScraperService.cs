using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeCrakers.Services
{
    public class ClistContest
    {
        public string Name { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public DateTime StartTimeUtc { get; set; }
        public TimeSpan Duration { get; set; }
        public string Url { get; set; } = string.Empty;
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
                // Match both absolute and relative links to clist.by contest pages only
                // Example matches:
                //  - https://clist.by/contest/12345
                //  - /contest/12345
                var rowRegex = new Regex(
                    "<a[^>]+href=\"(?<url>(?:https?:\\/\\/[^\\\"]*clist\\.by)?\\/contest\\/[^\\\"]+)\"[^>]*>(?<name>[^<]{3,})<\\/a>",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

                // Time patterns (prefer stable attributes, then legacy fixedtime links, then data- attributes)
                var timeFixedRegex = new Regex("fixedtime\\.html\\?iso=(?<iso>\\d{8}T\\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var timeDatetimeAttr = new Regex("datetime=\"(?<dt>\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}(?::\\d{2})?Z?)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var timeDataAttr = new Regex("data-(?:start(?:time)?|begin|time)=\"(?<dt>[^\">]{8,})\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var timeEpochAttr = new Regex("data-(?:start|time|epoch|timestamp)=\"?(?<sec>\\d{10,13})\"?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                var platformRegex = new Regex("/resource/([a-z0-9_.-]+)/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var durationRegex = new Regex(">\\s*(?<dur>(\\d+\\s*days?\\s*)?(\\d{1,2}:\\d{2}(?::\\d{2})?))\\s*<", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                foreach (Match m in rowRegex.Matches(html))
                {
                    var name = System.Net.WebUtility.HtmlDecode(m.Groups["name"].Value.Trim());
                    var url = m.Groups["url"].Value;
                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        url = "https://clist.by" + url;
                    }

                    int idx = m.Index;
                    int start = Math.Max(0, idx - 600);
                    int len = Math.Min(html.Length - start, 1800);
                    var window = html.Substring(start, len);

                    DateTime startUtc = DateTime.MinValue;
                    // Try datetime attribute first
                    var timeMatch = timeDatetimeAttr.Match(window);
                    if (timeMatch.Success)
                    {
                        var dtStr = timeMatch.Groups["dt"].Value;
                        if (DateTime.TryParse(dtStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                        {
                            startUtc = dt.ToUniversalTime();
                        }
                    }
                    // Fallback to fixedtime iso=yyyyMMddTHHmm
                    if (startUtc == DateTime.MinValue)
                    {
                        var ft = timeFixedRegex.Match(window);
                        if (ft.Success)
                        {
                            var iso = ft.Groups["iso"].Value; // yyyyMMddTHHmm
                            if (DateTime.TryParseExact(iso, "yyyyMMdd'T'HHmm", null,
                                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                                out var dt2))
                            {
                                startUtc = dt2;
                            }
                        }
                    }
                    // Fallback to data-* attr with ISO-like content
                    if (startUtc == DateTime.MinValue)
                    {
                        var da = timeDataAttr.Match(window);
                        if (da.Success)
                        {
                            var ds = da.Groups["dt"].Value.Trim();
                            if (DateTime.TryParse(ds, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt3))
                            {
                                startUtc = dt3.ToUniversalTime();
                            }
                        }
                    }
                    // Fallback to epoch seconds/millis
                    if (startUtc == DateTime.MinValue)
                    {
                        var ea = timeEpochAttr.Match(window);
                        if (ea.Success)
                        {
                            if (long.TryParse(ea.Groups["sec"].Value, out var secs))
                            {
                                if (secs > 1000000000000) // milliseconds
                                    startUtc = DateTimeOffset.FromUnixTimeMilliseconds(secs).UtcDateTime;
                                else
                                    startUtc = DateTimeOffset.FromUnixTimeSeconds(secs).UtcDateTime;
                            }
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

                    string platform = string.Empty;
                    var pmatch = platformRegex.Match(window);
                    if (pmatch.Success)
                    {
                        platform = pmatch.Groups[1].Value;
                    }

                    // Only include truly upcoming contests. Previously we allowed a -5 minute tolerance
                    // which could show just-started (running) contests in the upcoming list.
                    // Respect the user's preference: upcoming schedule only.
                    if (startUtc > DateTime.UtcNow)
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


