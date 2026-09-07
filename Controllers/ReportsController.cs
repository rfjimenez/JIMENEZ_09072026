using FileProcessingService.Models;
using FileProcessingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileProcessingService.Controllers
{
    //Reports on files processed since the service was started
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private const int MaxRecentCount = 100;

        private readonly IProcessingTracker _tracker;

        public ReportsController(IProcessingTracker tracker)
        {
            _tracker = tracker;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ProcessingReport), StatusCodes.Status200OK)]
        public ActionResult<ProcessingReport> Get([FromQuery] int recent = 20)
        {
            var count = Math.Clamp(recent, 0, MaxRecentCount);
            return Ok(_tracker.GetReport(count));
        }
    }
}
