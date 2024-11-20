using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using CareerAI.Services;
using CareerAI.Models;
using CareerAI.Extensions;
using CareerAI.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;

namespace CareerAI.Controllers
{
    public class ChatController : Controller
    {
        private readonly GoogleApiService _googleApiService;
        private readonly ChatHistoryRepository _chatHistoryRepository;
        private readonly ILogger<ChatController> _logger;

        public ChatController(GoogleApiService googleApiService, ChatHistoryRepository chatHistoryRepository, ILogger<ChatController> logger)
        {
            _googleApiService = googleApiService;
            _chatHistoryRepository = chatHistoryRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string chatHistoryId = null)
        {
            var userId = HttpContext.Session.GetString("UserId");
            List<ChatHistory> chatHistories = new List<ChatHistory>();

            if (!string.IsNullOrEmpty(userId))
            {
                chatHistories = await _chatHistoryRepository.GetChatHistoriesByUserIdAsync(userId);
                if (chatHistories.Count == 0)
                {
                    // Automatically create a new chat history if none exist
                    var newChatHistory = new ChatHistory
                    {
                        UserId = userId,
                        Title = "Untitled Chat",
                        Description = string.Empty,
                        Messages = new List<ChatMessage>(),
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    await _chatHistoryRepository.CreateChatHistoryAsync(newChatHistory);
                    chatHistories.Add(newChatHistory);
                }
                ViewBag.SelectedChatHistoryId = chatHistoryId;
                return View("Chat", chatHistories); // Return the Chat.cshtml view for logged-in users
            }
            else
            {
                // Load chat history from session for unauthenticated users
                var guestChatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                if (guestChatHistory == null)
                {
                    guestChatHistory = new ChatHistory
                    {
                        UserId = "guest",
                        Title = "Untitled Chat",
                        Description = string.Empty,
                        Messages = new List<ChatMessage>(),
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    HttpContext.Session.SetObjectAsJson("GuestChatHistory", guestChatHistory);
                }
                return View("GuestChat", guestChatHistory); // Return the GuestChat.cshtml view for guests
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChatHistory()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                var chatHistories = await _chatHistoryRepository.GetChatHistoriesByUserIdAsync(userId);
                if (chatHistories.Count >= 5)
                {
                    return BadRequest("You can only create up to 5 chat histories.");
                }

                var newChatHistory = new ChatHistory
                {
                    UserId = userId,
                    Title = "Untitled Chat",
                    Description = string.Empty,
                    Messages = new List<ChatMessage>(),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await _chatHistoryRepository.CreateChatHistoryAsync(newChatHistory);
                return Json(new { newChatHistoryId = newChatHistory.Id });
            }
            else
            {
                // Create a single chat history for guests if it doesn't exist
                var guestChatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                if (guestChatHistory == null)
                {
                    guestChatHistory = new ChatHistory
                    {
                        UserId = "guest",
                        Title = "Untitled Chat",
                        Description = string.Empty,
                        Messages = new List<ChatMessage>(),
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };

                    HttpContext.Session.SetObjectAsJson("GuestChatHistory", guestChatHistory);
                }

                return Ok();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string chatHistoryId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                ChatHistory chatHistory;

                if (!string.IsNullOrEmpty(userId))
                {
                    if (!ObjectId.TryParse(chatHistoryId, out var objectId))
                    {
                        return BadRequest("Invalid chat history ID.");
                    }

                    chatHistory = await _chatHistoryRepository.GetChatHistoryByIdAsync(chatHistoryId);
                    if (chatHistory == null || chatHistory.UserId != userId)
                    {
                        return NotFound();
                    }
                }
                else
                {
                    // Load chat history from session for unauthenticated users
                    chatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                    if (chatHistory == null)
                    {
                        return NotFound();
                    }
                }

                chatHistory.Messages.Add(new ChatMessage { Role = "user", Parts = new List<ChatPart> { new ChatPart { Text = message } } });

                var systemInstruction = new ChatMessage
                {
                    Role = "user",
                    Parts = new List<ChatPart>
                    {
                        new ChatPart
                        {
                            Text = "You are a career assistant or guide that helps students assess their skills and their capabilities to assist them in what career they might want to pursue. Remove the formatting of the message respond as if you are replying with just one paragraph. Please provide concise and accurate responses."
                        }
                    }
                };

                var responseText = await _googleApiService.GenerateContentAsync(chatHistory.Messages, systemInstruction);

                chatHistory.Messages.Add(new ChatMessage { Role = "model", Parts = new List<ChatPart> { new ChatPart { Text = responseText } } });

                // Update the title and description based on the latest messages
                var latestUserMessage = chatHistory.Messages.LastOrDefault(m => m.Role == "user")?.Parts.FirstOrDefault()?.Text;
                var latestBotResponse = chatHistory.Messages.LastOrDefault(m => m.Role == "model")?.Parts.FirstOrDefault()?.Text;

                if (!string.IsNullOrEmpty(latestUserMessage))
                {
                    chatHistory.Title = latestUserMessage;
                }

                if (!string.IsNullOrEmpty(latestBotResponse))
                {
                    chatHistory.Description = latestBotResponse;
                }

                chatHistory.UpdatedDate = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(userId))
                {
                    await _chatHistoryRepository.UpdateChatHistoryAsync(chatHistory);
                }
                else
                {
                    // Save updated chat history to session for unauthenticated users
                    HttpContext.Session.SetObjectAsJson("GuestChatHistory", chatHistory);
                }

                return Json(new { response = responseText });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error occurred while sending message.");
                return StatusCode(500, "HTTP request error");
            }
            catch (JsonReaderException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error occurred while sending message.");
                return StatusCode(500, "JSON parsing error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending message.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChatHistory(string chatHistoryId)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                ChatHistory chatHistory;

                if (!string.IsNullOrEmpty(userId))
                {
                    if (!ObjectId.TryParse(chatHistoryId, out var objectId))
                    {
                        return BadRequest("Invalid chat history ID.");
                    }

                    chatHistory = await _chatHistoryRepository.GetChatHistoryByIdAsync(chatHistoryId);
                    if (chatHistory == null || chatHistory.UserId != userId)
                    {
                        return NotFound();
                    }

                    await _chatHistoryRepository.DeleteChatHistoryAsync(chatHistoryId);
                }
                else
                {
                    // Load chat history from session for unauthenticated users
                    chatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                    if (chatHistory == null)
                    {
                        return NotFound();
                    }

                    HttpContext.Session.Remove("GuestChatHistory");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting chat history.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearChatHistory(string chatHistoryId)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                ChatHistory chatHistory;

                if (!string.IsNullOrEmpty(userId))
                {
                    if (!ObjectId.TryParse(chatHistoryId, out var objectId))
                    {
                        return BadRequest("Invalid chat history ID.");
                    }

                    chatHistory = await _chatHistoryRepository.GetChatHistoryByIdAsync(chatHistoryId);
                    if (chatHistory == null || chatHistory.UserId != userId)
                    {
                        return NotFound();
                    }

                    chatHistory.Messages.Clear();
                    chatHistory.Title = "Untitled Chat";
                    chatHistory.Description = null;
                    chatHistory.UpdatedDate = DateTime.UtcNow;

                    await _chatHistoryRepository.UpdateChatHistoryAsync(chatHistory);
                }
                else
                {
                    // Load chat history from session for unauthenticated users
                    chatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                    if (chatHistory == null)
                    {
                        return NotFound();
                    }

                    chatHistory.Messages.Clear();
                    chatHistory.Title = "Untitled Chat";
                    chatHistory.Description = null;
                    chatHistory.UpdatedDate = DateTime.UtcNow;

                    HttpContext.Session.SetObjectAsJson("GuestChatHistory", chatHistory);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while clearing chat history.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string chatHistoryId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            ChatHistory chatHistory;

            if (!string.IsNullOrEmpty(userId))
            {
                if (!ObjectId.TryParse(chatHistoryId, out var objectId))
                {
                    return BadRequest("Invalid chat history ID.");
                }

                chatHistory = await _chatHistoryRepository.GetChatHistoryByIdAsync(chatHistoryId);
                if (chatHistory == null || chatHistory.UserId != userId)
                {
                    return NotFound();
                }
            }
            else
            {
                // Load chat history from session for unauthenticated users
                chatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                if (chatHistory == null)
                {
                    return NotFound();
                }
            }

            return Json(new { messages = chatHistory.Messages });
        }

        [HttpGet]
        public IActionResult GetGuestChatHistory()
        {
            // Load chat history from session for unauthenticated users
            var chatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
            if (chatHistory == null)
            {
                return NotFound();
            }

            return Json(new { messages = chatHistory.Messages });
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistories()
        {
            var userId = HttpContext.Session.GetString("UserId");
            List<ChatHistory> chatHistories = new List<ChatHistory>();

            if (!string.IsNullOrEmpty(userId))
            {
                chatHistories = await _chatHistoryRepository.GetChatHistoriesByUserIdAsync(userId);
            }
            else
            {
                // Load chat history from session for unauthenticated users
                var sessionChatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                if (sessionChatHistory != null)
                {
                    chatHistories.Add(sessionChatHistory);
                }
            }

            var sortedChatHistories = chatHistories.OrderByDescending(ch => ch.UpdatedDate).Select(ch => new
            {
                id = ch.Id,
                title = ch.Title
            }).ToList();

            return Json(sortedChatHistories);
        }

        [HttpPost]
        public async Task<IActionResult> SendGuestMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
                // Load chat history from session for unauthenticated users
                var chatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                if (chatHistory == null)
                {
                    return NotFound();
                }

                chatHistory.Messages.Add(new ChatMessage { Role = "user", Parts = new List<ChatPart> { new ChatPart { Text = message } } });

                var systemInstruction = new ChatMessage
                {
                    Role = "user",
                    Parts = new List<ChatPart>
                    {
                        new ChatPart
                        {
                            Text = "You are a career assistant or guide that helps students assess their skills and their capabilities to assist them in what career they might want to pursue. Remove the formatting of the message respond as if you are replying with just one paragraph. Please provide concise and accurate responses."
                        }
                    }
                };

                var responseText = await _googleApiService.GenerateContentAsync(chatHistory.Messages, systemInstruction);

                chatHistory.Messages.Add(new ChatMessage { Role = "model", Parts = new List<ChatPart> { new ChatPart { Text = responseText } } });

                // Update the title and description based on the latest messages
                var latestUserMessage = chatHistory.Messages.LastOrDefault(m => m.Role == "user")?.Parts.FirstOrDefault()?.Text;
                var latestBotResponse = chatHistory.Messages.LastOrDefault(m => m.Role == "model")?.Parts.FirstOrDefault()?.Text;

                if (!string.IsNullOrEmpty(latestUserMessage))
                {
                    chatHistory.Title = latestUserMessage;
                }

                if (!string.IsNullOrEmpty(latestBotResponse))
                {
                    chatHistory.Description = latestBotResponse;
                }

                chatHistory.UpdatedDate = DateTime.UtcNow;

                // Save updated chat history to session for unauthenticated users
                HttpContext.Session.SetObjectAsJson("GuestChatHistory", chatHistory);

                return Json(new { response = responseText });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error occurred while sending message.");
                return StatusCode(500, "HTTP request error");
            }
            catch (JsonReaderException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error occurred while sending message.");
                return StatusCode(500, "JSON parsing error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending message.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public IActionResult ClearGuestChatHistory()
        {
            try
            {
                // Load chat history from session for unauthenticated users
                var chatHistory = HttpContext.Session.GetObjectFromJson<ChatHistory>("GuestChatHistory");
                if (chatHistory == null)
                {
                    return NotFound();
                }

                chatHistory.Messages.Clear();
                chatHistory.Title = "Untitled Chat";
                chatHistory.Description = null;
                chatHistory.UpdatedDate = DateTime.UtcNow;

                // Save updated chat history to session for unauthenticated users
                HttpContext.Session.SetObjectAsJson("GuestChatHistory", chatHistory);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while clearing chat history.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}