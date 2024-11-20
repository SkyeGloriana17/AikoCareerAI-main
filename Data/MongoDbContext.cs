using MongoDB.Driver;

namespace CareerAI.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoDatabase Database => _database;

        // public bool TestConnection()
        // {
        //     try
        //     {
        //         // Perform a simple operation to test the connection
        //         var filter = Builders<User>.Filter.Empty;
        //         var count = Users.CountDocuments(filter);
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         // Log the exception (optional)
        //         Console.WriteLine($"MongoDB connection test failed: {ex.Message}");
        //         return false;
        //     }
        // }
    }
}