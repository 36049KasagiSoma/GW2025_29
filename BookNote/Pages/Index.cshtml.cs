using BookNote.Scripts;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using System.Threading.Tasks;

namespace BookNote.Pages {
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public List<BookReview> RecommendedBooks { get; set; }


        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet() {
            using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                connection.Open();
                RecommendBook rb = new RecommendBook(connection);
                RecommendedBooks = rb.GetReview();
            }
        }
    }
}
