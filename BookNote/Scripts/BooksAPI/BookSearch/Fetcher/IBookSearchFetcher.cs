namespace BookNote.Scripts.BooksAPI.BookSearch.Fetcher {
    /// <summary>
    /// 書影を取得するためのインターフェイス
    /// </summary>
    public interface IBookSearchFetcher {
        /// <summary>
        /// ISBNから書影のURLを取得します
        /// </summary>
        /// <param name="isbn">ISBN（ハイフンあり・なし両対応）</param>
        /// <returns>書影のURL、取得できない場合はnull</returns>
        Task<List<BookSearchResult>> GetSearchBooksAsync(string keyword);
    }
}
