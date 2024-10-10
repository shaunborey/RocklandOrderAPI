using Microsoft.AspNetCore.Mvc;
using RocklandOrderAPI.Data;
using Microsoft.EntityFrameworkCore;
using RocklandOrderAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace RocklandOrderAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public DataController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, ILogger<DataController> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet("timezones")]
        public IActionResult GetTimeZones()
        {
            try
            {
                List<TimeZoneData> timezones = TimeZoneInfo.GetSystemTimeZones().AsEnumerable().Select<TimeZoneInfo, TimeZoneData>(tz =>
                {
                    bool isConverted = TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string? ianaId);
                    return new TimeZoneData()
                    {
                        IanaName = !isConverted ? "" : ianaId,
                        TimeZone = tz
                    };
                }).ToList();
                return Ok(timezones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving time zones.");
                return StatusCode(500, "Error: An error occurred while retrieving time zones.");
            }
        }
    }
}
