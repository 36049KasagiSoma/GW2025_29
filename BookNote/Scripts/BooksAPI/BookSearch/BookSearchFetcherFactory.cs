using BookNote.Scripts.BooksAPI.BookImage.Fetcher;
using BookNote.Scripts.BooksAPI.BookSearch.Fetcher;

namespace BookNote.Scripts.BooksAPI.BookSearch {
    public class BookSearchFetcherFactory {
        /// <summary>
        /// 指定されたソースの書籍検索クラスを作成します
        /// </summary>
        public static IBookSearchFetcher Create(BookSearchSource source) {
            return source switch {
                BookSearchSource.GoogleBooks => new GoogleBooksBookSearchrFetcher("AIzaSyC8iV4KdNYgWNdisRz3LRO5z_jtcj9GHuk"),
                BookSearchSource.Racuten => new RakutenBooksBookSearchFetcher("1034198535573888291"),
                _ => throw new ArgumentException($"未対応のソース: {source}", nameof(source))
            };
        }

        public static List<IBookSearchFetcher> CreateList() {
            return Enum.GetValues<BookSearchSource>()
               .Select(Create)
               .ToList();
        }
        /// <summary>
        /// 取得元
        /// </summary>
        public enum BookSearchSource {
            GoogleBooks,
            Racuten,
            // Amazon
        }
    }
}
