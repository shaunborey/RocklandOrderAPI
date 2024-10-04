using RocklandOrderAPI.Data;
using RocklandOrderAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace RocklandOrderAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _dbContext;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = configuration;
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null && (await _signInManager.CheckPasswordSignInAsync(user, model.Password, false)).Succeeded)
                {
                    // Login successful: Generate and return JWT
                    var token = GenerateJwtToken(user);
                    return Ok(new { token });
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during the login process for user {username}", model.Username);
                return StatusCode(500, "An unexpected error occurred.");
            }

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                // Validate all fields provided
                if (String.IsNullOrEmpty(model.Username) ||
                    String.IsNullOrEmpty(model.Password) ||
                    String.IsNullOrEmpty(model.FirstName) ||
                    String.IsNullOrEmpty(model.LastName) ||
                    String.IsNullOrEmpty(model.Email) ||
                    String.IsNullOrEmpty(model.TimeZoneId))
                {
                    return BadRequest("Registration Error: All fields are required.");
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return BadRequest("Registration Error: Email address is already in use.");
                }

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(model.TimeZoneId);
                if (timeZone == null)
                {
                    return BadRequest("Registration Error: Invalid time zone.");
                }

                string jwtToken = string.Empty;

                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    var newUser = new ApplicationUser()
                    {
                        UserName = model.Username,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        MiddleName = model.MiddleName,
                        LastName = model.LastName,
                        Suffix = model.Suffix,
                        Address1 = model.Address1,
                        Address2 = model.Address2,
                        City = model.City,
                        State = model.State,
                        PostalCode = model.PostalCode,
                        TimeZoneId = model.TimeZoneId,
                        OptInAccountNotices = model.OptInAccountNotices,
                        OptInProductNotices = model.OptInProductNotices
                    };

                    var creationResult = await _userManager.CreateAsync(newUser, model.Password);
                    if (creationResult == null || !creationResult.Succeeded)
                    {
                        // Registration failed
                        await transaction.RollbackAsync();
                        return BadRequest("Registration Error: Registration failed.");
                    }

                    // Generate email confirmation token
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                    var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { token, email = newUser.Email }, Request.Scheme);

                    // Send verification email
                    await SendVerificationEmail(newUser.Email, confirmationLink);

                    // Generate and return a JWT for the newly created user
                    jwtToken = GenerateJwtToken(newUser);

                    await transaction.CommitAsync();
                    return Ok(new { token = jwtToken });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred during the user registration process: {@registerdata}", model);
                    return StatusCode(500, "Registration Error: An error occurred during user registration.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the user registration process: {@registerdata}", model);
                return StatusCode(500, "Registration Error: An error occurred during user registration.");
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    return Ok("Email confirmed successfully.");
                }

                return BadRequest("Email confirmation failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during email confirmation for {emailAddress}", email);
                return StatusCode(500, "Email Confirmation Error: An error occurred during email confirmation.");
            }
        }

        // Helper method to send verification email
        private async Task SendVerificationEmail(string email, string confirmationLink)
        {
            try
            {
                using var smtpClient = new SmtpClient();
                smtpClient.Host = _config["Email:SmtpHost"];
                smtpClient.Port = 587;
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(_config["Email:SmtpUsername"], _config["Email:SmtpPassword"]);

                using var message = new MailMessage();
                message.From = new MailAddress(_config["Email:FromEmail"], _config["Email:FromName"]);
                message.To.Add(new MailAddress(email));
                message.Subject = "Email Verification";
                message.Body = $"Please confirm your email by clicking on this link: {confirmationLink}";

                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending verification email for {emailAddress}", email);
                throw;
            }
        }

        // Helper method to generate JWT
        private string GenerateJwtToken(ApplicationUser user)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                    new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
                    new Claim("middle_name", user.MiddleName),
                    new Claim("suffix", user.Suffix),
                    new Claim("address1", user.Address1),
                    new Claim("address2", user.Address2),
                    new Claim("city", user.City),
                    new Claim("state", user.State),
                    new Claim("postal_code", user.PostalCode),
                    new Claim("time_zone", user.TimeZoneId),
                    new Claim("opt_in_account_notices", user.OptInAccountNotices.ToString()),
                    new Claim("opt_in_product_notices", user.OptInProductNotices.ToString())
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpirationInMinutes"]));
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires,
                    SigningCredentials = creds,
                    Issuer = _config["Jwt:Issuer"],
                    Audience = _config["Jwt:Audience"]
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating JWT for {@user}", user);
                throw;
            }
        }
    }
}
