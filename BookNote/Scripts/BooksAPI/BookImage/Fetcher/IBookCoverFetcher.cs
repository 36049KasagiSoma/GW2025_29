namespace BookNote.Scripts.BooksAPI.BookImage.Fetcher {
    /// <summary>
    /// 書影を取得するためのインターフェイス
    /// </summary>
    public interface IBookCoverFetcher {
        /// <summary>
        /// ISBNから書影のURLを取得します
        /// </summary>
        /// <param name="isbn">ISBN（ハイフンあり・なし両対応）</param>
        /// <returns>書影のURL、取得できない場合はnull</returns>
        Task<string> GetCoverUrlAsync(string isbn);

        /// <summary>
        /// ISBNから書影の画像データを取得します
        /// </summary>
        /// <param name="isbn">ISBN（ハイフンあり・なし両対応）</param>
        /// <returns>画像データのバイト配列、取得できない場合はnull</returns>
        Task<byte[]> GetCoverImageAsync(string isbn);
    }
}
