using BookNote.Scripts.BooksAPI.BookImage.Fetcher;

namespace BookNote.Scripts.BooksAPI.BookImage {
    public class BookCoverFetcherFactory {
        /// <summary>
        /// 指定されたソースの書影取得クラスを作成します
        /// </summary>
        public static IBookCoverFetcher Create(BookCoverSource source) {
            return source switch {
                BookCoverSource.Ndl => new NdlBookCoverFetcher(),
                BookCoverSource.Racuten => new RakutenBooksBookCoverFetcher("1034198535573888291"),
                BookCoverSource.GoogleBooks => new GoogleBooksBookCoverFetcher("AIzaSyC8iV4KdNYgWNdisRz3LRO5z_jtcj9GHuk"),
                _ => throw new ArgumentException($"未対応のソース: {source}", nameof(source))
            };
        }

        public static List<IBookCoverFetcher> CreateList() {
            return Enum.GetValues<BookCoverSource>()
               .Select(Create)
               .ToList();
        }
        /// <summary>
        /// 書影の取得元
        /// </summary>
        public enum BookCoverSource {
            Ndl,
            GoogleBooks,
            Racuten,
            // Amazon
        }
    }
}
