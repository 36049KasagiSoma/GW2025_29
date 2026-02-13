namespace BookNote.Scripts.BooksAPI.Moderation {
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// OpenAI Moderation API クライアント
    /// </summary>
    public sealed class ModerationClient : IDisposable {
        private const string EndpointUrl = "https://api.openai.com/v1/moderations";

        /// <summary>
        /// Moderation API の 1 リクエストあたりの最大文字数
        /// </summary>
        private const int MaxChunkSize = 30000;

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private static readonly JsonSerializerOptions JsonOptions = new() {
            // CamelCase ポリシーはデシリアライズ時のキーマッチングに影響するため指定しない。
            // PropertyNameCaseInsensitive = true により大文字小文字を無視して照合する。
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="apiKey">OpenAI API キー</param>
        /// <param name="httpClient">
        /// 任意の HttpClient インスタンス。
        /// null の場合は内部で新規生成します（IHttpClientFactory 経由での注入を推奨）。
        /// </param>
        public ModerationClient(string apiKey, HttpClient? httpClient = null) {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API キーは必須です。", nameof(apiKey));

            _apiKey = apiKey;
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// 単一テキストをモデレーション判定します。
        /// テキストが上限（32,768 文字）を超える場合は自動的に分割して送信し、
        /// 全チャンクの結果をマージして返します。
        /// </summary>
        /// <param name="apiKey">OpenAI API キー</param>
        /// <param name="text">判定対象のテキスト</param>
        /// <returns>
        /// <see cref="ModerationResponse"/> — 判定結果全体。
        /// 分割送信時は全チャンクをマージした結果を返します。
        /// <c>Results[0].Flagged</c> が true の場合、有害コンテンツが検出されています。
        /// </returns>
        /// <exception cref="ArgumentException">text が空の場合</exception>
        /// <exception cref="HttpRequestException">API 呼び出しが失敗した場合</exception>
        /// <exception cref="InvalidOperationException">レスポンスのパースに失敗した場合</exception>
        public async Task<ModerationResponse> CheckAsync(string text) {
            if (string.IsNullOrWhiteSpace(text))
                return new ModerationResponse() {
                    Id = string.Empty,
                    Model = string.Empty,
                    Results = [],
                };

            // 上限以内ならそのまま送信
            if (text.Length <= MaxChunkSize)
                return await SendAsync(_apiKey, text).ConfigureAwait(false);

            // 上限を超える場合は分割して送信し、結果をマージ
            var chunks = SplitIntoChunks(text, MaxChunkSize);
            var responses = new List<ModerationResponse>();

            foreach (var chunk in chunks)
                responses.Add(await SendAsync(_apiKey, chunk).ConfigureAwait(false));

            return MergeResponses(responses);
        }

        /// <summary>
        /// 複数テキストをまとめてモデレーション判定します。
        /// </summary>
        /// <param name="apiKey">OpenAI API キー</param>
        /// <param name="texts">判定対象のテキスト配列</param>
        /// <returns>
        /// <see cref="ModerationResponse"/> — 判定結果全体。
        /// <c>Results[i]</c> が <c>texts[i]</c> に対応します。
        /// </returns>
        /// <exception cref="ArgumentException">texts が空または null の場合</exception>
        /// <exception cref="HttpRequestException">API 呼び出しが失敗した場合</exception>
        /// <exception cref="InvalidOperationException">レスポンスのパースに失敗した場合</exception>
        public async Task<ModerationResponse> CheckAsync(string[] texts) {
            if (texts == null || texts.Length == 0)
                return new ModerationResponse() {
                    Id = string.Empty,
                    Model = string.Empty,
                    Results = [],
                };

            var requestBody = JsonSerializer.Serialize(new { input = texts }, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl) {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Moderation API エラー: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseBody}"
                );

            return JsonSerializer.Deserialize<ModerationResponse>(responseBody, JsonOptions)
                ?? throw new InvalidOperationException("レスポンスのパースに失敗しました。");
        }

        // ─────────────────────────────────────────────
        // 内部メソッド
        // ─────────────────────────────────────────────

        /// <summary>
        /// 実際に API へリクエストを送信する内部メソッド
        /// </summary>
        private async Task<ModerationResponse> SendAsync(string apiKey, string text) {
            var requestBody = JsonSerializer.Serialize(new { input = text }, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl) {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Moderation API エラー: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseBody}"
                );

            return JsonSerializer.Deserialize<ModerationResponse>(responseBody, JsonOptions)
                ?? throw new InvalidOperationException("レスポンスのパースに失敗しました。");
        }

        /// <summary>
        /// テキストを指定サイズで分割します。
        /// 単語・文の途中での分割を避けるため、空白・改行・句読点を区切りとして優先します。
        /// </summary>
        private static IEnumerable<string> SplitIntoChunks(string text, int chunkSize) {
            int start = 0;
            while (start < text.Length) {
                int length = Math.Min(chunkSize, text.Length - start);

                // 末尾が上限ちょうどで文の途中の場合、直前の区切り文字で分割する
                if (start + length < text.Length) {
                    int lastBreak = text.LastIndexOfAny(
                        new[] { ' ', '\n', '\r', '\u3002', '\uff0c', '.', ',' }, // 。、
                        start + length - 1,
                        length
                    );
                    if (lastBreak > start)
                        length = lastBreak - start + 1;
                }

                yield return text.Substring(start, length);
                start += length;
            }
        }

        /// <summary>
        /// 複数チャンクのレスポンスを 1 件にマージします。
        /// - Flagged : いずれかのチャンクが true なら true
        /// - フラグ  : いずれかのチャンクが true なら true
        /// - スコア  : 各チャンクの最大値を採用（最もリスクが高い箇所を反映）
        /// </summary>
        private static ModerationResponse MergeResponses(List<ModerationResponse> responses) {
            var mergedResult = new ModerationResult {
                Flagged = responses.Any(r => r.Results[0].Flagged),

                Categories = new ModerationCategories {
                    Hate = responses.Any(r => r.Results[0].Categories.Hate),
                    HateThreatening = responses.Any(r => r.Results[0].Categories.HateThreatening),
                    Harassment = responses.Any(r => r.Results[0].Categories.Harassment),
                    HarassmentThreatening = responses.Any(r => r.Results[0].Categories.HarassmentThreatening),
                    SelfHarm = responses.Any(r => r.Results[0].Categories.SelfHarm),
                    SelfHarmIntent = responses.Any(r => r.Results[0].Categories.SelfHarmIntent),
                    SelfHarmInstructions = responses.Any(r => r.Results[0].Categories.SelfHarmInstructions),
                    Sexual = responses.Any(r => r.Results[0].Categories.Sexual),
                    SexualMinors = responses.Any(r => r.Results[0].Categories.SexualMinors),
                    Violence = responses.Any(r => r.Results[0].Categories.Violence),
                    ViolenceGraphic = responses.Any(r => r.Results[0].Categories.ViolenceGraphic),
                },

                CategoryScores = new ModerationCategoryScores {
                    Hate = responses.Max(r => r.Results[0].CategoryScores.Hate),
                    HateThreatening = responses.Max(r => r.Results[0].CategoryScores.HateThreatening),
                    Harassment = responses.Max(r => r.Results[0].CategoryScores.Harassment),
                    HarassmentThreatening = responses.Max(r => r.Results[0].CategoryScores.HarassmentThreatening),
                    SelfHarm = responses.Max(r => r.Results[0].CategoryScores.SelfHarm),
                    SelfHarmIntent = responses.Max(r => r.Results[0].CategoryScores.SelfHarmIntent),
                    SelfHarmInstructions = responses.Max(r => r.Results[0].CategoryScores.SelfHarmInstructions),
                    Sexual = responses.Max(r => r.Results[0].CategoryScores.Sexual),
                    SexualMinors = responses.Max(r => r.Results[0].CategoryScores.SexualMinors),
                    Violence = responses.Max(r => r.Results[0].CategoryScores.Violence),
                    ViolenceGraphic = responses.Max(r => r.Results[0].CategoryScores.ViolenceGraphic),
                },
            };

            return new ModerationResponse {
                Id = responses[0].Id,
                Model = responses[0].Model,
                Results = new[] { mergedResult },
            };
        }

        /// <inheritdoc/>
        public void Dispose() => _httpClient.Dispose();
    }
}
