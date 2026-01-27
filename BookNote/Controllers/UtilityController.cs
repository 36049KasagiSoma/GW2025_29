using BookNote.Scripts;
using Microsoft.AspNetCore.Mvc;

namespace BookNote.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class UtilityController : ControllerBase {
        [HttpGet("format-date")]
        public IActionResult FormatDate([FromQuery] DateTime date) {
            return Ok(StaticEvent.FormatPostingTime(date));
        }
    }
}
