using BookNote.Scripts.BooksAPI.BookImage;

namespace BookNote.Scripts.BooksAPI.BookImage.Fetcher {
    public class GoogleBooksBookCoverFetcher : BookCoverFetcherBase {
        private const string GoogleBooksApiBaseUrl = "https://www.googleapis.com/books/v1/volumes";

        public GoogleBooksBookCoverFetcher() : base() { }
        public GoogleBooksBookCoverFetcher(HttpClient httpClient) : base(httpClient) { }

        /// <summary>
        /// ISBNから書影のURLを取得します
        /// </summary>
        public override async Task<string> GetCoverUrlAsync(string isbn) {
            var normalizedIsbn = NormalizeIsbn(isbn);
            var requestUrl = $"{GoogleBooksApiBaseUrl}?q=isbn:{normalizedIsbn}";

            try {
                var response = await _httpClient.GetStringAsync(requestUrl);
                var json = System.Text.Json.JsonDocument.Parse(response);

                if (json.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0) {
                    var volumeInfo = items[0].GetProperty("volumeInfo");
                    if (volumeInfo.TryGetProperty("imageLinks", out var imageLinks)) {
                        // thumbnail or smallThumbnailを取得
                        if (imageLinks.TryGetProperty("thumbnail", out var thumbnail)) {
                            return thumbnail.GetString()?.Replace("http://", "https://") ?? string.Empty;
                        }
                        if (imageLinks.TryGetProperty("smallThumbnail", out var smallThumbnail)) {
                            return smallThumbnail.GetString()?.Replace("http://", "https://") ?? string.Empty;
                        }
                    }
                }
            } catch {
                // エラー時は空文字列を返す
            }

            return string.Empty;
        }
    }
}