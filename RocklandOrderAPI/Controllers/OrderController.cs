using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RocklandOrderAPI.Data;
using RocklandOrderAPI.Models;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

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

        [HttpGet("product-list")]
        public async Task<IActionResult> GetProductList()
        {
            try
            {
                var productList = await _dbContext.Products.ToListAsync();
                List<ProductModel> products = new List<ProductModel>();
                productList.ForEach(item => products.Add(new ProductModel() { Id = item.Id, Name = item.Name, Description = item.Description, Price = item.Price, Image = Convert.ToBase64String(item.Image) }));
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving the product list.");
                return StatusCode(500, "Error: An error occurred while retrieving the product list.");
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

                byte[] pdfFile = Convert.FromBase64String(model.PurchaseOrderPDF);

                if (model.PurchaseOrderPDF == null || !(_validatePDF(pdfFile)))
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

                List<OrderDetail> detailList = new List<OrderDetail>();
                model.Details.ForEach(d => detailList.Add(new OrderDetail() { Id = d.Id, UserOrderId = d.UserOrderId, Product = _dbContext.Products.First(p => p.Id == d.Product.Id), Quantity = d.Quantity, TotalPrice = d.TotalPrice }));

                var userOrder = new UserOrder
                {
                    User = user,
                    Details = detailList,
                    PurchaseOrderPDF = pdfFile,
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

                return Ok("The order was successfully submitted.");
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
