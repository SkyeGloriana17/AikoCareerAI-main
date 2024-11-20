using Microsoft.AspNetCore.Mvc;
using CareerAI.Models;
using CareerAI.Models.ViewModel;
using CareerAI.Services;

namespace CareerAI.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly UserRepository _userRepository;

    public AccountController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public IActionResult Index()
    {
        return View("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            if (user != null && BCrypt.Net.BCrypt.EnhancedVerify(model.Password, user.Password))
            {
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);
                

                // Handle successful login
                return RedirectToAction("Index", "Home");
            }

            // If authentication fails, add an error to the ModelState
            ModelState.AddModelError("", "Invalid login attempt.");
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }
}