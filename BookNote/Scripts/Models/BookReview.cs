using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookNote.Scripts.Models {
    [Table("BOOKREVIEW")]
    public class BookReview {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("REVIEW_ID")]
        public int ReviewId { get; set; }

        [Required]
        [StringLength(36)]
        [Column("USER_ID", TypeName = "CHAR")]
        public string UserId { get; set; }

        [Required]
        [StringLength(13)]
        [Column("ISBN", TypeName = "CHAR")]
        public string Isbn { get; set; }

        [Range(0, 9)]
        [Column("RATING")]
        public int? Rating { get; set; }

        /// <summary>
        /// 0 = ネタバレなし, 1 = ネタバレあり
        /// </summary>
        [Column("ISSPOILERS")]
        public int? IsSpoilers { get; set; }

        [Column("POSTINGTIME")]
        public DateTime PostingTime { get; set; }

        [StringLength(100)]
        [Column("TITLE")]
        public string Title { get; set; }

        [Column("REVIEW")]
        public string Review { get; set; }

        /* --- Navigation Properties --- */

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [ForeignKey(nameof(Isbn))]
        public virtual Book Book { get; set; }
    }
}
