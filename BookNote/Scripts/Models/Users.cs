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
        [StringLength(10)]
        [Column("USER_PUBLICID", TypeName = "CHAR")]
        public string UserPublicId { get; set; }

        [Required]
        [StringLength(30)]
        [Column("USER_NAME")]
        public string UserName { get; set; }

        [Required]
        [Column("USER_PROFILE", TypeName = "CLOB")]
        public string UserProfile { get; set; }

        /* Navigation */
        public virtual ICollection<BookReview> BookReviews { get; set; }
    }
}
