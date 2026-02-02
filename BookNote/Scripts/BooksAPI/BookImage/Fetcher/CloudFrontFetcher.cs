namespace BookNote.Scripts.BooksAPI.BookImage.Fetcher {
    public class CloudFrontFetcher: BookCoverFetcherBase {
        private readonly string CloudFrontBaseUrl = "https://d2dayc6ex7a6gk.cloudfront.net";
        public CloudFrontFetcher() : base() { }

        public CloudFrontFetcher(HttpClient httpClient) : base(httpClient) { }

        /// <summary>
        /// ISBNから書影のURLを取得します
        /// </summary>
        public override Task<string> GetCoverUrlAsync(string isbn) {
            var normalizedIsbn = NormalizeIsbn(isbn);
            return Task.FromResult($"{CloudFrontBaseUrl}/covers/{normalizedIsbn}.jpg");
        }
    }
}
