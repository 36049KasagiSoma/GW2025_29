using BookNote.Scripts.BooksAPI.BookImage;
using BookNote.Scripts.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Collections.Generic;
using System.Text.Json;

namespace BookNote.Scripts.BooksAPI.BookSearch.Fetcher {
    public class GoogleBooksBookSearchrFetcher : BookSearchFetcherBase {
        private const string GoogleBooksApiBaseUrl = "https://www.googleapis.com/books/v1/volumes";

        public GoogleBooksBookSearchrFetcher() : base() { }
        public GoogleBooksBookSearchrFetcher(HttpClient httpClient) : base(httpClient) { }

        public override async Task<List<BookSearchResult>> GetSearchBooksAsync(string keyword) {
            List<BookSearchResult> books = new List<BookSearchResult>();
            int maxResults = 40; // 1回あたりの最大取得数
            int totalToFetch = 200; // 合計取得数
            int startIndex = 0;

            try {
                while (books.Count < totalToFetch) {
                    string url = GoogleBooksApiBaseUrl +
                        $"?q={Uri.EscapeDataString(keyword)}" +
                        $"&startIndex={startIndex}" +
                        $"&maxResults={maxResults}";

                    var response = await _httpClient.GetStringAsync(url);
                    using JsonDocument doc = JsonDocument.Parse(response);
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("items", out JsonElement items)) {
                        break; // これ以上結果がない
                    }

                    int itemCount = 0;
                    foreach (JsonElement item in items.EnumerateArray()) {
                        string? titleText = null;
                        string? authorText = null;
                        string? publisherText = null;
                        string? isbn = null;
                        JsonElement volumeInfo = item.GetProperty("volumeInfo");

                        if (volumeInfo.TryGetProperty("industryIdentifiers", out JsonElement identifiers)) {
                            foreach (JsonElement id in identifiers.EnumerateArray()) {
                                string? type = id.GetProperty("type").GetString();
                                string? identifier = id.GetProperty("identifier").GetString();
                                if (type == "ISBN_13") {
                                    isbn = identifier;
                                    break;
                                }
                            }
                        }

                        if (isbn == null) continue;

                        titleText = volumeInfo.GetProperty("title").GetString();
                        if (volumeInfo.TryGetProperty("authors", out JsonElement authors)) {
                            authorText = String.Join('/', authors.EnumerateArray().Select(a => a.GetString()));
                        }
                        if (volumeInfo.TryGetProperty("publisher", out JsonElement publisher)) {
                            publisherText = publisher.GetString(); // 修正: EnumerateArray不要
                        }

                        var data = new Book {
                            Title = titleText ?? "不明",
                            Author = authorText ?? "不明",
                            Publisher = publisherText ?? "不明",
                            Isbn = isbn
                        };
                        books.Add(new BookSearchResult { book = data });
                        itemCount++;
                    }
                    startIndex += maxResults;

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