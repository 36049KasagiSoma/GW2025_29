using BookNote.Scripts.Login;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.ViewComponents {
    public class UserIconViewComponent : ViewComponent {
        private readonly OracleConnection _conn;
        private readonly ILogger<UserIconViewComponent> _logger;
        private readonly IConfiguration _configuration;

        public UserIconViewComponent(OracleConnection conn, ILogger<UserIconViewComponent> logger, IConfiguration configuration) {
            _conn = conn;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync() {
            var publicId = await AccountDataGetter.GetDbUserPublicIdAsync(_conn);
            byte[]? iconData = null;
            if (publicId != null)
                iconData = await new UserIconGetter(_configuration).GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            
            if (iconData != null && iconData.Length > 0) {
                var base64 = Convert.ToBase64String(iconData);
                var html = $"<img src=\"data:image/jpeg;base64,{base64}\" alt=\"User Icon\" class=\"user-icon-img\">";
                return new HtmlContentViewComponentResult(
                    new Microsoft.AspNetCore.Html.HtmlString(html)
                );
            }

            return new HtmlContentViewComponentResult(
              new Microsoft.AspNetCore.Html.HtmlString(
                  "<div class=\"user-icon-placeholder\"><span>?</span></div>"
              )
          );
        }
    }
}
