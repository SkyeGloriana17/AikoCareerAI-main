using MongoDB.Driver;
using CareerAI.Models;

public class UserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _users.Find(user => true).ToListAsync();
    }

    public async Task<User> GetUserByIdAsync(string id)
    {
        return await _users.Find(user => user.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _users.Find(user => user.Email == email).FirstOrDefaultAsync();
    }

    public async Task CreateUserAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }

    public async Task UpdateUserAsync(string id, User user)
    {
        await _users.ReplaceOneAsync(u => u.Id == id, user);
    }

    public async Task DeleteUserAsync(string id)
    {
        await _users.DeleteOneAsync(u => u.Id == id);
    }
    
    public async Task<User> GetUserByVerificationTokenAsync(string token)
    {
        return await _users.Find(user => user.EmailVerificationToken == token).FirstOrDefaultAsync();
    }
}