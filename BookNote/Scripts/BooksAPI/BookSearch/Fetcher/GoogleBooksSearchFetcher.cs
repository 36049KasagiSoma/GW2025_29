using BookNote.Scripts.BooksAPI.BookImage;
using BookNote.Scripts.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Collections.Generic;
using System.Text.Json;

namespace BookNote.Scripts.BooksAPI.BookSearch.Fetcher {
    public class GoogleBooksBookSearchrFetcher : BookSearchFetcherBase {
        private const string GoogleBooksApiBaseUrl = "https://www.googleapis.com/books/v1/volumes";

        private string _applicationId;

        public GoogleBooksBookSearchrFetcher(string applicationId) : base() {
            _applicationId = applicationId;
        }
        public GoogleBooksBookSearchrFetcher(HttpClient httpClient, string applicationId) : base(httpClient) {
            _applicationId = applicationId;
        }

        public override async Task<List<BookSearchResult>> GetSearchBooksAsync(string keyword) {
            List<BookSearchResult> books = new List<BookSearchResult>();
            int maxResults = 40;
            int totalToFetch = 200;
            int startIndex = 0;
            int totalCount = 0;

            try {
                while (books.Count < totalToFetch) {
                    Console.WriteLine($"GoogleBooksApi =>{books.Count}取得済み:{startIndex} 件～");
                    string url = GoogleBooksApiBaseUrl +
                        $"?q={Uri.EscapeDataString(keyword)}" +
                        $"&startIndex={startIndex}" +
                        $"&maxResults={maxResults}" +
                        $"&key={_applicationId}";

                    var response = await _httpClient.GetStringAsync(url);
                    using JsonDocument doc = JsonDocument.Parse(response);
                    JsonElement root = doc.RootElement;

                    // 初回ループで総件数を取得
                    if (totalCount == 0 &&
                        root.TryGetProperty("totalItems", out JsonElement totalElem)) {
                        totalCount = totalElem.GetInt32();
                    }

                    if (!root.TryGetProperty("items", out JsonElement items) ||
                        items.GetArrayLength() == 0) {
                        Progress = 1f;
                        break;
                    }

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
                            publisherText = publisher.GetString();
                        }

                        var data = new Book {
                            Title = titleText ?? "不明",
                            Author = authorText ?? "不明",
                            Publisher = publisherText ?? "不明",
                            Isbn = isbn
                        };
                        Console.WriteLine(data.Title);
                        books.Add(new BookSearchResult { book = data });
                    }

                    // 進捗更新（totalToFetch と totalCount の小さい方を分母にする）
                    int denominator = totalCount > 0
                        ? Math.Min(totalToFetch, totalCount)
                        : totalToFetch;
                    UpdateProgress((float)books.Count / denominator);
                    startIndex += maxResults;
                    await Task.Delay(100);
                }

                UpdateProgress(1f);
            } catch (Exception ex) {
                Console.WriteLine($"エラー: {ex.Message}");
            }

            return books;
        }
    }
}