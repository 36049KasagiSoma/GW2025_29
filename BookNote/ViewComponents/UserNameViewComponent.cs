using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.ViewComponents {
    public class UserNameViewComponent : ViewComponent {
        private readonly OracleConnection _conn;

        public UserNameViewComponent(OracleConnection conn) {
            _conn = conn;
        }
        public async Task<IViewComponentResult> InvokeAsync() {
            var userName = await AccountDataGetter.GetDbUserNameAsync(_conn);
            return Content(userName ?? "ユーザー");
        }
    }
}