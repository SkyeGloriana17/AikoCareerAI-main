using MongoDB.Driver;
using CareerAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareerAI.Data
{
    public class ChatHistoryRepository
    {
        private readonly IMongoCollection<ChatHistory> _chatHistories;

        public ChatHistoryRepository(IMongoDatabase database)
        {
            _chatHistories = database.GetCollection<ChatHistory>("ChatHistories");
        }

        public async Task<List<ChatHistory>> GetChatHistoriesByUserIdAsync(string userId)
        {
            return await _chatHistories.Find(ch => ch.UserId == userId).ToListAsync();
        }

        public async Task<ChatHistory> GetChatHistoryByIdAsync(string id)
        {
            return await _chatHistories.Find(ch => ch.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateChatHistoryAsync(ChatHistory chatHistory)
        {
            await _chatHistories.InsertOneAsync(chatHistory);
        }

        public async Task UpdateChatHistoryAsync(ChatHistory chatHistory)
        {
            await _chatHistories.ReplaceOneAsync(ch => ch.Id == chatHistory.Id, chatHistory);
        }

        public async Task DeleteChatHistoryAsync(string id)
        {
            await _chatHistories.DeleteOneAsync(ch => ch.Id == id);
        }
    }
}