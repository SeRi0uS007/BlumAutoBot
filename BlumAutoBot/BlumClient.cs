using System.Text;
using System.Text.Json;
using System.Net;
using System.Text.Json.Nodes;

namespace BlumBot
{
    public class BlumClient: IDisposable
    {
        Settings _settings;
        HttpClient _httpClient;
        private static string GetUserAgent(BlumPlatform platform) =>
            platform switch
            {
                BlumPlatform.iOS15 => "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1",
                BlumPlatform.iOS16 => "Mozilla/5.0 (iPhone; CPU iPhone OS 16_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.4 Mobile/15E148 Safari/604.1",
                BlumPlatform.Android => "Mozilla/5.0 (Linux; Android 13; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Mobile Safari/537.36",
                BlumPlatform.Windows => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0",
                BlumPlatform.MacOS => "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_4) AppleWebKit/537.36 (KHTML, like Gecko) Version/16.0 Safari/537.36",
                _ => throw new ArgumentException($"Invalid platform {platform.ToString()}")
            };

        public BlumClient(Settings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(GetUserAgent(_settings.Platform));
            _httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json, text/plain, */*");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.TryParseAdd("en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("origin", "https://telegram.blum.codes");
            _httpClient.DefaultRequestHeaders.Add("priority", "u=1, i");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "same-site");

            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _settings.AuthorizationToken);

            if (_settings.Platform == BlumPlatform.Windows)
            {
                _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Microsoft Edge\";v=\"126\", \"Chromium\";v=\"126\", \"Not.A/Brand\";v=\"8\", \"Microsoft Edge WebView2\";v=\"126\"");
                _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
                _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
            }
        }

        private async Task<(bool, string?)> MakeRequestAsync(HttpMethod method, Uri uri, StringContent? payload = null)
        {
            HttpRequestMessage request = new()
            {
                Method = method,
                RequestUri = uri
            };

            if (payload != null)
                request.Content = payload;

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Invalid token. Please recheck");
                return (false, null);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Invalid HTTP Status Code: {response.StatusCode}");
                return (false, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            return (true, content);
        }

        private async Task<(bool, uint)> GetBalanceAsync()
        {
            Uri uri = new("https://game-domain.blum.codes/api/v1/user/balance");
            var (success, response) = await MakeRequestAsync(HttpMethod.Get, uri);

            if (!success)
                return (false, 0);

            if (response == null)
            {
                Console.WriteLine("Balance content is null");
                return (false, 0);
            }

            using (JsonDocument document = JsonDocument.Parse(response))
            {
                var playPasses = document.RootElement.GetProperty("playPasses").GetUInt32();
                return (true, playPasses);
            }
        }

        private async Task<(bool, string?)> GetGameIdAsync()
        {
            Uri uri = new("https://game-domain.blum.codes/api/v1/game/play");
            var (success, response) = await MakeRequestAsync(HttpMethod.Post, uri);

            if (!success)
                return (false, null);

            if (response == null)
            {
                Console.WriteLine("GameId content is null");
                return (false, null);
            }

            using (JsonDocument document = JsonDocument.Parse(response))
            {
                var gameId = document.RootElement.GetProperty("gameId").GetString();
                return (true, gameId);
            }
        }

        private async Task<bool> ClaimPointsAsync(string gameId, int points)
        {
            Uri uri = new("https://game-domain.blum.codes/api/v1/game/claim");
            var payloadJson = new JsonObject
            {
                ["gameId"] = gameId,
                ["points"] = points
            };
            StringContent payload = new(payloadJson.ToJsonString(), Encoding.UTF8, "application/json");

            var (success, _) = await MakeRequestAsync(HttpMethod.Post, uri, payload);

            return success;
        }

        private async Task<bool> PlayGameAsync()
        {
            Console.WriteLine("Starting game");
            var (success, gameId) = await GetGameIdAsync();
            if (!success || string.IsNullOrEmpty(gameId))
            {
                Console.WriteLine("Unable to start game");
                return false;
            }

            Console.WriteLine("Waiting 32 seconds");
            await Task.Delay(TimeSpan.FromSeconds(32));

            Random pointsRnd = new();
            var points = pointsRnd.Next((int)_settings.MinScore, (int)_settings.MaxScore + 1);

            Console.WriteLine($"Claiming {points} points");
            success = await ClaimPointsAsync(gameId, points);
            if (!success)
                Console.WriteLine("Unable to claim points");
            return success;
        }

        public async Task<bool> StartBotAsync()
        {
            var (success, passes) = await GetBalanceAsync();
            if (!success)
                return false;

            if (passes == 0)
            {
                Console.WriteLine("There are no passes");
                return true;
            }

            Console.WriteLine($"Found {passes} passes");
            for (int i = 0;  i < passes; i++)
            {
                success = await PlayGameAsync();
                if (!success)
                    return false;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return true;
        }

        public void Dispose() => _httpClient.Dispose();
    }
}