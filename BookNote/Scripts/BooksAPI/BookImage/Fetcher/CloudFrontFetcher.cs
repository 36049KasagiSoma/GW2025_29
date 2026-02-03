namespace BookNote.Scripts.BooksAPI.BookImage.Fetcher {
    public class CloudFrontFetcher: BookCoverFetcherBase {
        public CloudFrontFetcher() : base() { }

        public CloudFrontFetcher(HttpClient httpClient) : base(httpClient) { }

        /// <summary>
        /// ISBNから書影のURLを取得します
        /// </summary>
        public override Task<string> GetCoverUrlAsync(string isbn) {
            var normalizedIsbn = NormalizeIsbn(isbn);
            return Task.FromResult($"{Keywords.GetCloudFrontBaceUrl()}/covers/{StaticEvent.toHash(normalizedIsbn)}.jpg");
        }
    }
}
