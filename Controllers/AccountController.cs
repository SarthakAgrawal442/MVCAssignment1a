using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVCSampleApp.Models;
using MVCSampleApp.Services;

namespace MVCSampleApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISmsSender _smsSender;

        public AccountController(SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager, ISmsSender smsSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _smsSender = smsSender;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string phoneNumber)
        {
            var user = new ApplicationUser { UserName = email, Email = email, PhoneNumber = phoneNumber };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Employee");
                // Turn 2FA on for this account so it gets asked for an SMS code on login
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

            if (result.RequiresTwoFactor)
            {
                var user = await _userManager.FindByEmailAsync(email);
                var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Phone");
                await _smsSender.SendSmsAsync(user.PhoneNumber, $"Your login code is: {code}");

                TempData["2fa-email"] = email;
                return RedirectToAction("LoginWith2fa");
            }

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View();
        }

        [HttpGet]
        public IActionResult LoginWith2fa() => View();

        [HttpPost]
        public async Task<IActionResult> LoginWith2fa(string code)
        {
            var result = await _signInManager.TwoFactorSignInAsync("Phone", code, isPersistent: false, rememberClient: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Invalid code.");
            return View();
        }

        // SSO - kicks off the Google login flow
        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> ExternalLoginCallback()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction("Login");

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            // First time logging in with Google - create a local account for them
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            await _userManager.CreateAsync(user);
            await _userManager.AddLoginAsync(user, info);
            await _userManager.AddToRoleAsync(user, "Employee");
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
