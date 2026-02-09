using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.UserControl;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Oracle.ManagedDataAccess.Client;
using System.Security.Claims;
using static BookNote.Pages.ReviewDetailsModel;

namespace BookNote.Pages.user {
    public class MyPageModel : PageModel {
        IConfiguration _configuration;
        public MyPageModel(IConfiguration configuration) {
            _configuration = configuration;
        }

        public string UserPublicId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Profile { get; set; }
        public int ReviewCount { get; set; }
        public int TotalLikes { get; set; }
        public byte[]? IconImageData { get; set; }


        public async Task<IActionResult> OnGetAsync() {
            if (!AccountController.IsAuthenticated()) {
                return RedirectToPage("/Login");
            }

            var id = AccountController.GetUserId();

            using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        u.User_Id,
                        u.User_PublicId,
                        u.User_Name,
                        u.User_Profile,
                        NVL(r.Review_Count, 0) AS Review_Count,
                        NVL(r.Total_Good_Count, 0) AS Total_Good_Count
                    FROM Users u
                    LEFT JOIN (
                        SELECT 
                            br.User_Id,
                            COUNT(DISTINCT br.Review_Id) AS Review_Count,
                            COUNT(gr.Review_Id) AS Total_Good_Count
                        FROM BookReview br
                        LEFT JOIN GoodReview gr
                            ON br.Review_Id = gr.Review_Id
                        WHERE br.PostingTime IS NOT NULL
                        GROUP BY br.User_Id
                    ) r
                    ON u.User_Id = r.User_Id
                    WHERE u.User_Id = :UserId
                ";

                using (var command = new OracleCommand(query, connection)) {
                    command.Parameters.Add(new OracleParameter("UserId", id));

                    using (var reader = await command.ExecuteReaderAsync()) {
                        if (await reader.ReadAsync()) {
                            UserPublicId = reader["User_PublicId"] as string ?? "";
                            UserName = reader["User_Name"] as string ?? "";
                            Profile = reader["User_Profile"] as string ?? "";
                            ReviewCount = reader["Review_Count"] != DBNull.Value
                                ? Convert.ToInt32(reader["Review_Count"])
                                : 0;
                            TotalLikes = reader["Total_Good_Count"] != DBNull.Value
                                ? Convert.ToInt32(reader["Total_Good_Count"])
                                : 0;
                        }
                    }
                }

                IconImageData = await new UserIconGetter()
                    .GetIconImageData(UserPublicId, UserIconGetter.IconSize.LARGE);

                return Page();
            }
        }
    }
}
