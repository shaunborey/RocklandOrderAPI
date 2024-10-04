using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RocklandOrderAPI.Data;
using RocklandOrderAPI.Models;
using System.Security.Claims;

namespace RocklandOrderAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly long _fileSizeLimit;

        public OrderController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _logger = logger;
            _dbContext = dbContext;
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit", 2097152);
        }

        [HttpGet("shipping-options")]
        public async Task<IActionResult> GetShippingOptions()
        {
            try
            {
                var options = await _dbContext.ShippingOptions.ToListAsync();
                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving shipping options.");
                return StatusCode(500, "Error: An error occurred while retrieving shipping options.");
            }
        }

        [HttpPost("create-order")]
        [Authorize]
        public async Task<IActionResult> CreateOrder(OrderModel model)
        {
            try
            {
                // Validate the model data
                if (model == null)
                {
                    return BadRequest("Invalid order data.");
                }

                if (string.IsNullOrEmpty(model.ShippingAddress1) || string.IsNullOrEmpty(model.ShippingCity) || string.IsNullOrEmpty(model.ShippingState) || string.IsNullOrEmpty(model.ShippingPostalCode))
                {
                    return BadRequest("Shipping address is required.");
                }

                if (model.PurchaseOrderPDF == null || !(_validatePDF(model.PurchaseOrderPDF)))
                {
                    return BadRequest("Invalid purchase order file.");
                }

                var shippingOption = await _dbContext.ShippingOptions.FindAsync(model.ShippingOptionId);
                if (shippingOption == null)
                {
                    return BadRequest("A valid shipping option is required.");
                }

                if (model.Details.Sum(x => x.TotalPrice) + shippingOption.Amount != model.OrderTotal)
                {
                    return BadRequest("Order total does not match the expected amount.");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                var userOrder = new UserOrder
                {
                    User = user,
                    Details = model.Details,
                    PurchaseOrderPDF = model.PurchaseOrderPDF,
                    OrderTotal = model.OrderTotal,
                    ShippingAddress1 = model.ShippingAddress1,
                    ShippingAddress2 = model.ShippingAddress2,
                    ShippingCity = model.ShippingCity,
                    ShippingState = model.ShippingState,
                    ShippingPostalCode = model.ShippingPostalCode,
                    ShippingOption = shippingOption,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.New
                };

                _dbContext.UserOrders.Add(userOrder);
                await _dbContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating an order.");
                return StatusCode(500, "Order Error: An unexpected error occurred while creating an order.");
            }
        }

        // Helper method to validate that the file represents a pdf file and is below the allowed size limit
        private bool _validatePDF(byte[] fileContents)
        {
            byte[] pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            using (var fileStream = new MemoryStream(fileContents))
            {
                long fileSize = fileStream.Length;
                using (var reader = new BinaryReader(fileStream))
                {
                    var headerBytes = reader.ReadBytes(pdfSignature.Length);
                    return headerBytes.SequenceEqual(pdfSignature) && fileSize < _fileSizeLimit;
                }
            }            
        }
    }
}
