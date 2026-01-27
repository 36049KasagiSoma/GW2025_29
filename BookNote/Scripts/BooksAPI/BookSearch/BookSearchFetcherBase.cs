using BookNote.Scripts.BooksAPI.BookSearch.Fetcher;

namespace BookNote.Scripts.BooksAPI.BookSearch {
    /// <summary>
    /// 書籍検索の基底クラス
    /// </summary>
    public abstract class BookSearchFetcherBase : IBookSearchFetcher {
        protected readonly HttpClient _httpClient;

        protected BookSearchFetcherBase() {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BookCoverFetcher/1.0");
        }

        protected BookSearchFetcherBase(HttpClient httpClient) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public abstract Task<List<BookSearchResult>> GetSearchBooksAsync(string keyword);

        /// <summary>
        /// ISBNを正規化します（ハイフンを除去）
        /// </summary>
        protected string NormalizeIsbn(string isbn) {
            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException("ISBNが指定されていません", nameof(isbn));

            return isbn.Replace("-", "").Replace(" ", "").Trim();
        }

        
    }
}
