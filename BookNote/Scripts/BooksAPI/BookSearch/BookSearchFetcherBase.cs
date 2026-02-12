using BookNote.Scripts.BooksAPI.BookSearch.Fetcher;
namespace BookNote.Scripts.BooksAPI.BookSearch {
    /// <summary>
    /// 書籍検索の基底クラス
    /// </summary>
    public abstract class BookSearchFetcherBase : IBookSearchFetcher {
        protected readonly HttpClient _httpClient;
        public float Progress { get; protected set; } = 0f;

        /// <summary>
        /// 進捗が更新された時に呼び出されるイベント
        /// </summary>
        public event Action<float>? OnProgressChanged;

        protected BookSearchFetcherBase() {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BookCoverFetcher/1.0");
        }
        protected BookSearchFetcherBase(HttpClient httpClient) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public abstract Task<List<BookSearchResult>> GetSearchBooksAsync(string keyword);

        /// <summary>
        /// 進捗を更新し、イベントを発火します
        /// </summary>
        protected void UpdateProgress(float progress) {
            Progress = Math.Clamp(progress, 0f, 1f);
            OnProgressChanged?.Invoke(Progress);
        }

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