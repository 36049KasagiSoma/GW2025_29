using BookNote.Scripts.BooksAPI.BookImage.Fetcher;

namespace BookNote.Scripts.BooksAPI.BookImage {
    /// <summary>
    /// 書影取得の基底クラス
    /// </summary>
    public abstract class BookCoverFetcherBase : IBookCoverFetcher {
        protected readonly HttpClient _httpClient;

        protected BookCoverFetcherBase() {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BookCoverFetcher/1.0");
        }

        protected BookCoverFetcherBase(HttpClient httpClient) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// ISBNを正規化します（ハイフンを除去）
        /// </summary>
        protected string NormalizeIsbn(string isbn) {
            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException("ISBNが指定されていません", nameof(isbn));

            return isbn.Replace("-", "").Replace(" ", "").Trim();
        }

        public abstract Task<string> GetCoverUrlAsync(string isbn);

        public virtual async Task<byte[]> GetCoverImageAsync(string isbn) {
            try {
                var url = await GetCoverUrlAsync(isbn);
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode) {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                return null;
            } catch (HttpRequestException) {
                return null;
            } catch (Exception) {
                throw;
            }
        }
    }
}
