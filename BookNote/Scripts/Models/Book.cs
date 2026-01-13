using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookNote.Scripts.Models {
    [Table("BOOKS")]
    public class Book {
        [Key]
        [StringLength(13)]
        [Column("ISBN", TypeName = "CHAR")]
        public string Isbn { get; set; }

        [StringLength(100)]
        [Column("TITLE")]
        public string Title { get; set; }

        [StringLength(50)]
        [Column("AUTHOR")]
        public string Author { get; set; }

        [StringLength(30)]
        [Column("PUBLISHER")]
        public string Publisher { get; set; }

        public string? ImageUrl = null;

        /* Navigation */
        public virtual ICollection<BookReview> BookReviews { get; set; }
    }
}
