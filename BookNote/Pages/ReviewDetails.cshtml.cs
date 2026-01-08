using BookNote.Scripts;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Pages {
    public class ReviewDetailsModel : PageModel {
        private readonly IConfiguration _configuration;

        public int ReviewId { get; set; }
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
                    br.IsSpoilers,
                    br.PostingTime,
                    b.Title AS BookTitle,
                    b.Author,
                    u.User_Name
                FROM BookReview br
                INNER JOIN Books b ON br.ISBN = b.ISBN
                INNER JOIN Users u ON br.User_Id = u.User_Id
                WHERE br.Review_Id = :ReviewId";

                using (var command = new OracleCommand(query, connection)) {
                    command.Parameters.Add(new OracleParameter("ReviewId", reviewId));

                    using (var reader = await command.ExecuteReaderAsync()) {
                        if (await reader.ReadAsync()) {
                            ReviewTitle = reader["Title"] as string;
                            BookTitle = reader["BookTitle"] as string;
                            UserName = reader["User_Name"] as string;

                            if (!reader.IsDBNull(reader.GetOrdinal("Rating"))) {
                                Evaluation = Convert.ToInt32(reader["Rating"]);
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("IsSpoilers"))) {
                                IsSpoilers = Convert.ToInt32(reader["IsSpoilers"]) == 1;
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("PostingTime"))) {
                                PostingTime = reader.GetDateTime(reader.GetOrdinal("PostingTime"));
                                PostingTimeDisplay = FormatPostingTime(PostingTime.Value);
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

        private string FormatPostingTime(DateTime postingTime) {
            var now = DateTime.Now;
            var diff = now - postingTime;

            if (diff.TotalMinutes < 60) {
                return $"{(int)diff.TotalMinutes}分前";
            } else if (diff.TotalHours < 24) {
                return $"{(int)diff.TotalHours}時間前";
            } else if (diff.TotalDays < 30) {
                return $"{(int)diff.TotalDays}日前";
            } else {
                return postingTime.ToString("yyyy/MM/dd");
            }
        }

        public class ReviewData { }

    }
}
