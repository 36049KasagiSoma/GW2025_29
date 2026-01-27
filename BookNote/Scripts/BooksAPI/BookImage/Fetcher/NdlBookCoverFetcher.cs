using BookNote.Scripts.BooksAPI.BookImage;

namespace BookNote.Scripts.BooksAPI.BookImage.Fetcher {
    public class NdlBookCoverFetcher : BookCoverFetcherBase {
        private const string NdlCoverBaseUrl = "https://ndlsearch.ndl.go.jp/thumbnail/";

        public NdlBookCoverFetcher() : base() { }

        public NdlBookCoverFetcher(HttpClient httpClient) : base(httpClient) { }

        /// <summary>
        /// ISBNから書影のURLを取得します
        /// </summary>
        public override Task<string> GetCoverUrlAsync(string isbn) {
            var normalizedIsbn = NormalizeIsbn(isbn);
            return Task.FromResult($"{NdlCoverBaseUrl}{normalizedIsbn}.jpg");
        }
    }
}
