using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookNote.Scripts.Models {
    [Table("USERS")]
    public class User {
        [Key]
        [StringLength(36)]
        [Column("USER_ID", TypeName = "CHAR")]
        public string UserId { get; set; }

        [Required]
        [StringLength(30)]
        [Column("USER_NAME")]
        public string UserName { get; set; }

        /* Navigation */
        public virtual ICollection<BookReview> BookReviews { get; set; }
    }
}
