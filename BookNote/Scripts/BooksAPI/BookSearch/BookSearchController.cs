using BookNote.Scripts.BooksAPI.BookImage;
using System.Collections.Generic;
namespace BookNote.Scripts.BooksAPI.BookSearch {
    public class BookSearchController {

        /// <summary>
        /// 全体進捗が更新された時のイベント
        /// </summary>
        public event Action<float>? OnProgressChanged;

        public async Task<List<BookSearchResult>> GetSearchBooks(string keyword) {
            var fecList = BookSearchFetcherFactory.CreateList();
            float processing = 0f;
            int fecCount = fecList.Count;

            var tasks = fecList.Select(async fec => {
                try {
                    if (fec is BookSearchFetcherBase b) {
                        b.OnProgressChanged += progress => {
                            processing = fecList
                                .OfType<BookSearchFetcherBase>()
                                .Sum(x => x.Progress / fecCount);
                            OnProgressChanged?.Invoke(processing);
                        };
                    }
                    var books = await fec.GetSearchBooksAsync(keyword);
                    return books ?? new List<BookSearchResult>();
                } catch {
                    return new List<BookSearchResult>();
                }
            });

            var results = await Task.WhenAll(tasks);
            var list = results
                .SelectMany(books => books)
                .GroupBy(b => b.book?.Isbn)
                .Select(g => g.First())
                .Where(b => b.book?.Isbn != null)
                .ToList();
            return list;
        }
    }
}