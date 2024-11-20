using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CareerAI.Data;
using CareerAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareerAI.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly ILogger<AssessmentController> _logger;
        private readonly ChatHistoryRepository _chatHistoryRepository;

        public AssessmentController(ILogger<AssessmentController> logger, ChatHistoryRepository chatHistoryRepository)
        {
            _logger = logger;
            _chatHistoryRepository = chatHistoryRepository;
        }

        public async Task<IActionResult> AssessmentAI()
        {
            var userId = HttpContext.Session.GetString("UserId");
            List<ChatHistory> chatHistories = new List<ChatHistory>();

            if (!string.IsNullOrEmpty(userId))
            {
                chatHistories = await _chatHistoryRepository.GetChatHistoriesByUserIdAsync(userId);
            }

            return View(chatHistories);
        }

        public async Task<IActionResult> AssessmentResult()
        {
            var userId = HttpContext.Session.GetString("UserId");
            List<ChatHistory> chatHistories = new List<ChatHistory>();

            if (!string.IsNullOrEmpty(userId))
            {
                chatHistories = await _chatHistoryRepository.GetChatHistoriesByUserIdAsync(userId);
            }

            return View(chatHistories);
        }

        public IActionResult Assessment()
        {
            return View();
        }
    }
}