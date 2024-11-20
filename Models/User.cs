using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CareerAI.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; }

    [BsonIgnoreIfNull]
    public string? OTP { get; set; }

    [BsonIgnoreIfNull]
    public DateTime? OtpExpiration { get; set; }

    [BsonIgnoreIfNull]
    public bool? IsEmailVerified { get; set; }

    [BsonIgnoreIfNull]
    public string? EmailVerificationToken { get; set; }
}

public enum UserRole {
    Admin,
    User
}