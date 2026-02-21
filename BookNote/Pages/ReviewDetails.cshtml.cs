using BookNote.Scripts;
using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.BooksAPI.BookImage;
using BookNote.Scripts.Login;
using BookNote.Scripts.SelectBookReview;
using BookNote.Scripts.UserControl;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Diagnostics;

namespace BookNote.Pages {
    public class ReviewDetailsModel : PageModel {
        private readonly OracleConnection _conn;
        private readonly BookImageController _bookImageController;
        private readonly UserIconGetter _userIconGetter;
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
        private readonly HtmlSanitizer _sanitizer;

        private IConfiguration _configuration;


        public ReviewDetailsModel(OracleConnection conn, IConfiguration configuration) {
            _configuration = configuration;
            _bookImageController = new BookImageController(_configuration);
            _userIconGetter = new UserIconGetter(_configuration);
            _conn = conn;
            _sanitizer = new HtmlSanitizer();
            SetupSabutizer();
        }

        private void SetupSabutizer() {
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("s");
            _sanitizer.AllowedTags.Add("h1");
            _sanitizer.AllowedTags.Add("h2");
            _sanitizer.AllowedTags.Add("h3");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");
            _sanitizer.AllowedTags.Add("blockquote");
        }


        public async Task<IActionResult> OnGetAsync(int reviewId) {
            ReviewId = reviewId;

            if (_conn.State != ConnectionState.Open) {
                await _conn.OpenAsync();
            }
            var query = @"
                SELECT 
                    br.Review_Id,
                    br.Title,
                    br.Review,
                    br.Rating,
                    br.ISBN,
                    br.IsSpoilers,
                    br.PostingTime,
                    br.Status_Id,
                    b.Title AS BookTitle,
                    b.Author,
                    u.User_Name,
                    u.User_PublicId
                FROM BookReview br
                INNER JOIN Books b ON br.ISBN = b.ISBN
                INNER JOIN Users u ON br.User_Id = u.User_Id
                WHERE br.Review_Id = :ReviewId";

            using (var command = new OracleCommand(query, _conn)) {
                command.Parameters.Add(new OracleParameter("ReviewId", reviewId));

                using (var reader = await command.ExecuteReaderAsync()) {
                    if (await reader.ReadAsync()) {

                        if (Convert.ToInt32(reader["Status_Id"]) == 2) {
                            if (!reader.IsDBNull(reader.GetOrdinal("PostingTime"))) {
                                PostingTime = reader.GetDateTime(reader.GetOrdinal("PostingTime"));
                                PostingTimeDisplay = StaticEvent.FormatPostingTime(PostingTime.Value);
                            }
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
                            ReviewHtml = _sanitizer.Sanitize(reader.GetOracleClob(reader.GetOrdinal("Review")).Value);
                        }
                        Review = new ReviewData(); // レビューが存在することを示すフラグ
                    }
                }

            }
            if (AccountDataGetter.IsAuthenticated())
                ActivityTracer.LogActivity(ActivityType.VIEW, AccountDataGetter.GetUserId(), reviewId.ToString());

            return Page();
        }


        public class ReviewData { }

        public async Task<IActionResult> OnGetSimilarReviewsAsync(int reviewId) {
            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();
            var myId = AccountDataGetter.IsAuthenticated() ? AccountDataGetter.GetUserId() : null;
            var selector = new SelectSimilarReview(_conn, myId);
            var reviews = await selector.GetReview(6, reviewId);
            var result = reviews.Select(r => new {
                reviewId = r.ReviewId,
                reviewTitle = r.Title,
                bookTitle = r.Book?.Title,
                userName = r.User?.UserName,
                userPublicId = r.User?.UserPublicId,
                rating = r.Rating,
                isbn = r.Isbn,
                postingTime = r.PostingTime
            });
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetImageAsync(string isbn) {
            byte[]? imageData = await _bookImageController.GetBookImageData(isbn);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            byte[]? imageData = await _userIconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg");
            }

            return NotFound();
        }
    }
}
