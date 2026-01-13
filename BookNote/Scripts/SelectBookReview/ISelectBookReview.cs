using BookNote.Scripts.Models;

namespace BookNote.Scripts.SelectBook {
    public interface ISelectBookReview {
        public Task<List<BookReview>> GetReview();
    }
}
