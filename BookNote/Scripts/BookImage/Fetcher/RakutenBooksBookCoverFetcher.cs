namespace BookNote.Scripts.BookImage.Fetcher {
    public class RakutenBooksBookCoverFetcher : BookCoverFetcherBase {
        private const string RakutenBooksApiBaseUrl = "https://app.rakuten.co.jp/services/api/BooksBook/Search/20170404";
        private readonly string _applicationId;

        public RakutenBooksBookCoverFetcher(string applicationId) : base() {
            _applicationId = applicationId;
        }

        public RakutenBooksBookCoverFetcher(HttpClient httpClient, string applicationId) : base(httpClient) {
            _applicationId = applicationId;
        }

        /// <summary>
        /// ISBNから書影のURLを取得します
        /// </summary>
        public override async Task<string> GetCoverUrlAsync(string isbn) {
            var normalizedIsbn = NormalizeIsbn(isbn);
            var requestUrl = $"{RakutenBooksApiBaseUrl}?applicationId={_applicationId}&isbn={normalizedIsbn}";

            try {
                var response = await _httpClient.GetStringAsync(requestUrl);
                var json = System.Text.Json.JsonDocument.Parse(response);

                if (json.RootElement.TryGetProperty("Items", out var items) && items.GetArrayLength() > 0) {
                    var item = items[0].GetProperty("Item");
                    if (item.TryGetProperty("largeImageUrl", out var largeImageUrl)) {
                        return largeImageUrl.GetString() ?? string.Empty;
                    }
                    if (item.TryGetProperty("mediumImageUrl", out var mediumImageUrl)) {
                        return mediumImageUrl.GetString() ?? string.Empty;
                    }
                }
            } catch {
                // エラー時は空文字列を返す
            }

            return string.Empty;
        }
    }
}