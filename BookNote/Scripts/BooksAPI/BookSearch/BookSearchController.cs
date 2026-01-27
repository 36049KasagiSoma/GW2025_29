using BookNote.Scripts.BooksAPI.BookImage;
using System.Collections.Generic;

namespace BookNote.Scripts.BooksAPI.BookSearch {
    public class BookSearchController {
        public async Task<List<BookSearchResult>> GetSearchBooks(string keyword) {
            var fecList = BookSearchFetcherFactory.CreateList();

            var tasks = fecList.Select(async fec => {
                try {
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
