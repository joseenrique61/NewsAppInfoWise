using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NewsAppInfoWise.Models;
using Microsoft.EntityFrameworkCore;

namespace NewsAppInfoWise.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        bool adminExists = (await _userManager.GetUsersInRoleAsync("Admin")).Any();
        if (adminExists)
        {
            return Unauthorized();
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Check if an Admin already exists.
        // We only allow registration if there are no Admin users yet,
        // or if the currently logged-in user is an Admin (which should not happen, but for safety)
        bool adminExists = (await _userManager.GetUsersInRoleAsync("Admin")).Any();
        if (adminExists)
        {
            // If an admin already exists, forbid further registrations.
            // This prevents regular users from registering new accounts after an admin is set up.
            // Admins themselves would not typically use the Register endpoint, but if they did,
            // they would still be prevented unless explicitly allowed (not done here for simplicity).
            ModelState.AddModelError(string.Empty, "Registration is currently disabled as an administrator already exists.");
            return Forbid(); 
        }


        if (ModelState.IsValid)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Create roles if they don't exist
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                }

                // Assign the first registered user to the Admin role (this logic now happens AFTER the adminExists check)
                if (!adminExists) // If no admin existed before this registration, this user becomes the admin
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}