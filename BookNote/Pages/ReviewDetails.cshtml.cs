using BookNote.Scripts;
using BookNote.Scripts.BooksAPI.BookImage;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Pages {
    public class ReviewDetailsModel : PageModel {
        private readonly IConfiguration _configuration;

        public int ReviewId { get; set; }
        public string? UserPublicId { get; set; }
        public string? Isbn { get; set; }
        public string? ReviewTitle { get; set; }
        public string? BookTitle { get; set; }
        public string? BookCoverUrl { get; set; }
        public int? Evaluation { get; set; }
        public bool IsSpoilers { get; set; }
        public string? UserName { get; set; }
        public DateTime? PostingTime { get; set; }
        public string? PostingTimeDisplay { get; set; }
        public string? ReviewHtml { get; set; }
        public ReviewData? Review { get; set; }
        public bool IsDraft { get; set; } = false;

        public ReviewDetailsModel(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync(int reviewId) {
            ReviewId = reviewId;

            using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                await connection.OpenAsync();

                var query = @"
                SELECT 
                    br.Review_Id,
                    br.Title,
                    br.Review,
                    br.Rating,
                    br.ISBN,
                    br.IsSpoilers,
                    br.PostingTime,
                    b.Title AS BookTitle,
                    b.Author,
                    u.User_Name,
                    u.User_PublicId
                FROM BookReview br
                INNER JOIN Books b ON br.ISBN = b.ISBN
                INNER JOIN Users u ON br.User_Id = u.User_Id
                WHERE br.Review_Id = :ReviewId";

                using (var command = new OracleCommand(query, connection)) {
                    command.Parameters.Add(new OracleParameter("ReviewId", reviewId));

                    using (var reader = await command.ExecuteReaderAsync()) {
                        if (await reader.ReadAsync()) {
                            if (!reader.IsDBNull(reader.GetOrdinal("PostingTime"))) {
                                PostingTime = reader.GetDateTime(reader.GetOrdinal("PostingTime"));
                                if (PostingTime == null) {
                                    IsDraft = true;
                                    return Page();
                                }
                                PostingTimeDisplay = StaticEvent.FormatPostingTime(PostingTime.Value);
                            } else {
                                IsDraft = true;
                                return Page();
                            }

                            ReviewTitle = reader["Title"] as string;
                            BookTitle = reader["BookTitle"] as string;
                            Isbn = reader["Isbn"] as string;
                            UserName = reader["User_Name"] as string;
                            UserPublicId = reader["User_PublicId"] as string;
                            if (!reader.IsDBNull(reader.GetOrdinal("Rating"))) {
                                Evaluation = Convert.ToInt32(reader["Rating"]);
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("IsSpoilers"))) {
                                IsSpoilers = Convert.ToInt32(reader["IsSpoilers"]) == 1;
                            }

                            // マークダウンをHTMLに変換
                            if (!reader.IsDBNull(reader.GetOrdinal("Review"))) {
                                var reviewText = reader.GetOracleClob(reader.GetOrdinal("Review")).Value;
                                var pipeline = new MarkdownPipelineBuilder()
                                    .UseAdvancedExtensions()
                                    .Build();
                                ReviewHtml = Markdown.ToHtml(reviewText, pipeline);
                            }

                            Review = new ReviewData(); // レビューが存在することを示すフラグ
                        }
                    }
                }
            }

            return Page();
        }


        public class ReviewData { }

        public async Task<IActionResult> OnGetImageAsync(string isbn) {
            byte[]? imageData = await new BookImageController().GetBookImageData(isbn);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }
    }
}
