using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CareerAI.Data;
using CareerAI.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareerAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ChatHistoryRepository _chatHistoryRepository; //ADD THIS SHIT TO EVERY VIEW THAT HAVE A CHAT HISTORY SIDEBAR

        public HomeController(ILogger<HomeController> logger, ChatHistoryRepository chatHistoryRepository) //HERE TOO FOR DEPENDENCY INJECTION
        {
            _logger = logger;
            _chatHistoryRepository = chatHistoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId"); //THISS UP TO - 
            List<ChatHistory> chatHistories = new List<ChatHistory>();

            if (!string.IsNullOrEmpty(userId))
            {
                chatHistories = await _chatHistoryRepository.GetChatHistoriesByUserIdAsync(userId); // - HERE 
            }

            return View(chatHistories); // THEN Pass the chat histories to the view
        }


        public IActionResult Assessment()
        {
            return View();
        }

        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Clear the session cookie
            if (Request.Cookies[".AspNetCore.Session"] != null)
            {
                Response.Cookies.Delete(".AspNetCore.Session");
            }

            // Redirect to the login page or home page
            return Redirect("/");
        }
    }
}