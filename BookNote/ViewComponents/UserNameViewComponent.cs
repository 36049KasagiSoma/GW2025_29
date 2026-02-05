using Microsoft.AspNetCore.Mvc;
using BookNote.Scripts.Login;

namespace BookNote.ViewComponents {
    public class UserNameViewComponent : ViewComponent {
        public async Task<IViewComponentResult> InvokeAsync() {
            var userName = await AccountController.GetDbUserNameAsync();
            return Content(userName ?? "ユーザー");
        }
    }
}