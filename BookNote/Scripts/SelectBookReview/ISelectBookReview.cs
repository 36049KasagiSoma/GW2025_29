using BookNote.Scripts.Models;

namespace BookNote.Scripts.SelectBook {
    public interface ISelectBookReview {
        public List<BookReview> GetReview();
    }
}
