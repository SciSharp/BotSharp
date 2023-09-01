namespace BotSharp.Plugin.Mongo.Collections;

public class UserCollection : MongoBase
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Salt { get; set; }
    public string Password { get; set; }
    public string? ExternalId { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}