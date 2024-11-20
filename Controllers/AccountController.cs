using Microsoft.AspNetCore.Mvc;
using CareerAI.Models;
using CareerAI.Models.ViewModel;
using CareerAI.Services;

namespace CareerAI.Controllers;

public class AccountController : Controller
{
    private readonly UserRepository _userRepository;
    private readonly EmailService _emailService;

    public AccountController(UserRepository userRepository, EmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public IActionResult Error404()
    {
        return View();
    }
    
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Popups()
    {
        return View();
    }

    public IActionResult EmailMessage()
    {
        return View();
    }

    public IActionResult ForgotPassword()
    {
        return View();
    }

    public IActionResult VerifyOtp(string email)
    {
        var model = new VerifyOtpViewModel { Email = email };
        return View(model);
    }

    public IActionResult ResetPassword(string email, string otp)
    {
        var model = new ResetPasswordViewModel { Email = email, Otp = otp };
        return View(model);
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
                return Redirect("/");
            }

            // If authentication fails, add an error to the ModelState
            ModelState.AddModelError("", "Invalid login attempt.");
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model) {
        if (ModelState.IsValid)
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "An account with this email already exists.");
                return View(model);
            }

            var verificationToken = Guid.NewGuid().ToString();
            var newUser = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(model.NewPassword, 11),
                Role = UserRole.User,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken
            };

            await _userRepository.CreateUserAsync(newUser);

                // Send verification email
                var verificationLink = Url.Action("VerifyEmail", "Account", new { token = verificationToken }, Request.Scheme);
                _emailService.SendEmail(newUser.Email, "Email Verification", $"Please verify your email by clicking <a href='{verificationLink}'>here</a>.");

            ViewData["EmailSent"] = true;
        }

        return View(model);
    }
    [HttpGet("Account/VerifyEmail/{token}")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        var user = await _userRepository.GetUserByVerificationTokenAsync(token);
        if (user != null)
        {
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            await _userRepository.UpdateUserAsync(user.Id, user);

            _emailService.SendEmail(user.Email, "Email Verified", @"
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 600px; margin: 20px auto; border-radius: 8px; overflow: hidden;'>
        <tr>
            <td style='padding: 0; text-align: center; position: relative;'>
                <img src='/assets/email-banner/banner-verified.png' alt='Banner' style='width: 100%; max-width: 800px; display: block; border-bottom: 1px solid #ddd;'>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px 30px;color: #777;'>
                <p style='font-family:Helvetica; color:black; font-weight:500; margin:0; opacity:.8;'><b>Dear User,</b><br><br>
                Congratulations! Your email address has been successfully verified for your Aikareer account. You now have full access to all the features and resources we offer to guide you on your career journey.<br><br>
                Please take a moment to explore the platform and customize your profile to receive personalized career recommendations.</p>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px; text-align: center;'>
                <a href='http://career.aiko/Account/Login' style='font-family:Helvetica; font-weight:bold; border:3px solid black; background-color: #FFD662; color: #252E49; padding: 10px 75px; text-decoration: none; border-radius: 10px; display: inline-block;'>Login</a>
            </td>
        </tr>
        <tr>
            <td style='padding: 30px 30px;'>
                <p style='font-family:Helvetica; color:black; font-weight:500; margin:0; opacity:.8;'><b>Need Assistance?</b><br>
                If you encounter any issues or have questions, our support team is here to help. Contact us at <u>aiko.career@gmail.com</u> for assistance.<br><br>
                Thank you for choosing <b>Aikareer</b>. We are excited to be a part of your career planning journey.<br><br>
                <b>Best regards,</b><br>
                The Aikareer Team<br>
                [Career.Aiko]</p>
                <hr style='margin:4% 0;'>
                <p style='font-family:Arial; opacity:.5; text-align:center; font-size:.8rem;'>This website is developed to guide individuals in making informed career decisions and achieving their professional goals.</p>
            </td>
        </tr>
    </table>");
        }
        return View(); 
    }
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            if (user != null)
            {
                // Generate OTP
                var otp = new Random().Next(100000, 999999).ToString();
                user.OTP = otp;
                user.OtpExpiration = DateTime.UtcNow.AddMinutes(10); // OTP valid for 10 minutes
                await _userRepository.UpdateUserAsync(user.Id, user);

                // Send OTP to user's email
                _emailService.SendEmail(user.Email, "Password Reset OTP", $@"
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 600px; margin: 20px auto; border-radius: 8px; overflow: hidden;'>
        <tr>
            <td style='padding: 0; text-align: center; position: relative;'>
                <img src='/assets/email-banner/banner-forgotPass.png' alt='Banner' style='width: 100%; max-width: 800px; display: block; border-bottom: 1px solid #ddd;'>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px 30px;color: #777;'>
                <p style='font-family:Helvetica; color:black; font-weight:500; margin:0; opacity:.8;'><b>Dear User,</b><br><br>
                To ensure the security of your account, we have generated a One-Time Password (OTP) for your recent request on Aikareer. Please use the following OTP to proceed:<br><br>
                <b>Your OTP: {otp}</b><br><br>
                This code is valid for 10 minutes and can be used only once. For your security, do not share this OTP with anyone. If you are having trouble entering the OTP, ensure that it is typed correctly or try copying and pasting.</p>
            </td>
        </tr>
        <tr>
            <td style='padding: 20px; text-align: center;'>
                <a href='#' style='font-family:Helvetica; font-weight:bold;border:3px solid black; background-color: #FFD662; color: #252E49; padding: 10px 75px; text-decoration: none; border-radius: 10px; display: inline-block;'>Go to OTP</a>
            </td>
        </tr>
        <tr>
            <td style='padding: 30px 30px;'>
                <p style='font-family:Helvetica; color:black; font-weight:500; margin:0; opacity:.8;'><b>Protect Your Account:</b><br>
                We prioritize your safety. If you did not initiate this request or suspect any suspicious activity, please ignore this message and contact our support team at aiko.career@gmail.com for further assistance.<br><br>
                Thank you for choosing <b>Aikareer</b>. We are committed to keeping your information secure.<br><br>
                <b>Best regards,</b><br>
                The Aikareer Team<br>
                [Career.Aiko]</p>
                <hr style='margin:4% 0;'>
                <p style='font-family:Arial; opacity:.5; text-align:center; font-size:.8rem;'>This website is developed to help people make informed decisions about their future careers.</p>
            </td>
        </tr>
    </table>");
                // Redirect to OTP verification page
                return Redirect("/Account/VerifyOtp");
            }

            ModelState.AddModelError("", "Email not found.");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            if (user != null && user.OTP == model.Otp && user.OtpExpiration > DateTime.UtcNow)
            {
                // OTP is valid, redirect to reset password page
                return Redirect($"/Account/ResetPassword?email={user.Email}&otp={user.OTP}");
            }

            if(user != null && user.OTP != model.Otp) 
            {
                ModelState.AddModelError("", "Invalid OTP");
                Console.WriteLine("Invalid OTP");
            }

            if(user != null && user.OtpExpiration > DateTime.UtcNow) 
            {
                ModelState.AddModelError("", "OTP has expired");
                Console.WriteLine("OTP has expired");
            }

            if(user == null) 
            {
                ModelState.AddModelError("", "User Not Found");
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            if (user != null && user.OTP == model.Otp)
            {
                // Reset the password
                user.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(model.NewPassword);
                user.OTP = null;
                user.OtpExpiration = null;
                await _userRepository.UpdateUserAsync(user.Id, user);

                // Redirect to login page
                return Redirect("/Account/Login");
            }

            ModelState.AddModelError("", "Invalid OTP or OTP has expired.");
        }

        return View(model);
    }
}   