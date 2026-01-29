using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookNote.Pages.error {
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class Error500Model : PageModel {
        public string? ErrorMessage { get; set; }
        public bool ShowDetails { get; set; }

        private readonly IWebHostEnvironment _environment;

        public Error500Model(IWebHostEnvironment environment) {
            _environment = environment;
        }

        public void OnGet() {
            ShowDetails = _environment.IsDevelopment();

            if (ShowDetails) {
                var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                if (exceptionFeature?.Error != null) {
                    ErrorMessage = $"Exception: {exceptionFeature.Error.Message}\n\n" +
                                   $"StackTrace:\n{exceptionFeature.Error.StackTrace}";
                }
            }
        }
    }
}
