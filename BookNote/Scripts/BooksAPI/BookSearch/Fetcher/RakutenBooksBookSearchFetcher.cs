using BookNote.Scripts.Models;
using System.Text.Json;

namespace BookNote.Scripts.BooksAPI.BookSearch.Fetcher {
    public class RakutenBooksBookSearchFetcher : BookSearchFetcherBase {
        private const string RakutenBooksApiBaseUrl = "https://app.rakuten.co.jp/services/api/BooksBook/Search/20170404";
        private readonly string _applicationId;

        public RakutenBooksBookSearchFetcher(string applicationId) : base() {
            _applicationId = applicationId;
        }

        public RakutenBooksBookSearchFetcher(HttpClient httpClient, string applicationId) : base(httpClient) {
            _applicationId = applicationId;
        }

        public override async Task<List<BookSearchResult>> GetSearchBooksAsync(string keyword) {
            List<BookSearchResult> books = new List<BookSearchResult>();
            int maxResults = 30; // 1回あたりの最大取得数
            int totalToFetch = 200; // 合計取得数
            int page = 1;

            try {
                while (books.Count < totalToFetch) {
                    string url = RakutenBooksApiBaseUrl +
                        $"?applicationId={_applicationId}" +
                        $"&title={Uri.EscapeDataString(keyword)}" +
                        $"&format=json" +
                        $"&hits={maxResults}" +
                        $"&page={page}";

                    var response = await _httpClient.GetStringAsync(url);
                    using JsonDocument doc = JsonDocument.Parse(response);
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("Items", out JsonElement items)) {
                        break; // これ以上結果がない
                    }

                    int itemCount = 0;
                    foreach (JsonElement item in items.EnumerateArray()) {
                        if (!item.TryGetProperty("Item", out JsonElement bookInfo)) continue;

                        string? isbn = bookInfo.TryGetProperty("isbn", out JsonElement isbnElem)
                            ? isbnElem.GetString()
                            : null;

                        if (string.IsNullOrEmpty(isbn)) continue;

                        string? title = bookInfo.TryGetProperty("title", out JsonElement titleElem)
                            ? titleElem.GetString()
                            : null;

                        string? author = bookInfo.TryGetProperty("author", out JsonElement authorElem)
                            ? authorElem.GetString()
                            : null;

                        string? publisher = bookInfo.TryGetProperty("publisherName", out JsonElement publisherElem)
                            ? publisherElem.GetString()
                            : null;

                        string? imageUrl = bookInfo.TryGetProperty("largeImageUrl", out JsonElement imageElem)
                            ? imageElem.GetString()
                            : null;

                        var data = new Book {
                            Isbn = isbn,
                            Title = title ?? "不明",
                            Author = author ?? "不明",
                            Publisher = publisher ?? "不明",
                            ImageUrl = imageUrl
                        };

                        books.Add(new BookSearchResult { book = data });
                        itemCount++;
                    }

                    page++;

                    // API制限を考慮して少し待機
                    await Task.Delay(30);
                }
            } catch (Exception ex) {
                Console.WriteLine($"エラー: {ex.Message}");
            }

            return books;
        }
    }
}