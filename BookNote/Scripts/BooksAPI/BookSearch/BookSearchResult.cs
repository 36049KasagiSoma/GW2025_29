using BookNote.Scripts.Models;

namespace BookNote.Scripts.BooksAPI.BookSearch {
    public record BookSearchResult {
        public Book? book { get; set; }
    }
}
