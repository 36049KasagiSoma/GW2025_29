using BookNote.Scripts;
using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Pages.user {
    public class WithdrawModel : PageModel {
        private readonly OracleConnection _conn;

        public WithdrawModel(OracleConnection conn) {
            _conn = conn;
        }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet() {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync() {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");

            var userId = AccountDataGetter.GetUserId();

            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            try {
                var sql = "UPDATE Users SET User_StatusId = 4 WHERE User_Id = :UserId";
                using var cmd = new OracleCommand(sql, _conn);
                cmd.Parameters.Add(new OracleParameter("UserId", userId));
                await cmd.ExecuteNonQueryAsync();

                return RedirectToPage("/Logout");

            } catch {
                ErrorMessage = "退会処理に失敗しました。しばらく時間をおいてから再度お試しください。";
                return Page();
            }
        }
    }
}
