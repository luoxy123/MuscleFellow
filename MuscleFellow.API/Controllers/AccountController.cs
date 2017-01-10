using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MuscleFellow.API.JWT;
using MuscleFellow.API.Models;
using MuscleFellow.Data.Interfaces;
using MuscleFellow.Models;

namespace MuscleFellow.API.Controllers
{
    [Route("api/v1/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly ILogger _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<WebApiSettings> _settings;
        
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ICartItemRepository cartItemRepository,
            ILoggerFactory loggerFactory,
            IOptions<WebApiSettings> settings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cartItemRepository = cartItemRepository;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _settings = settings;
        }


        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Ok();
            var user = await _userManager.FindByEmailAsync(id);
            var result = new JsonResult(user);
            return result;
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] LoginAPIModel registerModel)
        {
            var user = new ApplicationUser {UserName = registerModel.UserID, Email = registerModel.UserID};
            try
            {
                var result = await _userManager.CreateAsync(user, registerModel.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    _logger.LogInformation(3, "User created a new account with password.");
                    return Ok();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Ok();

            // If we got this far, something failed.
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginAPIModel loginModel)
        {
            var result = await _signInManager.PasswordSignInAsync(
                loginModel.UserID, loginModel.Password, true, false);
            if (result.Succeeded)
            {
                //await HttpContext.Authentication.SignInAsync("Cookie", User);
                //_logger.LogInformation(1, "User logged in.");
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Value.SecretKey));
                var options = new TokenProviderOptions
                {
                    Audience = "MuscleFellowAudience",
                    Issuer = "MuscleFellow",
                    SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                };
                var tpm = new TokenProvider(options);
                var token = await tpm.GenerateToken(HttpContext, loginModel.UserID, loginModel.Password);
                if (null != token)
                    return new JsonResult(token);
                return NotFound();
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning(2, "User account locked out.");
                return Ok("Lockout");
            }
            _logger.LogWarning(2, "Invalid login attempt.");
            return Ok("Invalid login attempt.");
        }

        // POST: /Account/LogOff
        [HttpPost]
        [Authorize(ActiveAuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            await HttpContext.Authentication.SignOutAsync("Cookie");
            _logger.LogInformation(4, "User logged out.");
            return Ok();
        }
    }
}