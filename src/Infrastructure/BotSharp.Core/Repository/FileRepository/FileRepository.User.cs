using BotSharp.Abstraction.Users.Models;
using System.IO;

namespace BotSharp.Core.Repository
{
    public partial class FileRepository
    {
        public User? GetUserByEmail(string email)
        {
            return Users.FirstOrDefault(x => x.Email == email);
        }

        public User? GetUserById(string id = null)
        {
            return Users.FirstOrDefault(x => x.ExternalId == id || x.Id == id);
        }

        public void CreateUser(User user)
        {
            var userId = Guid.NewGuid().ToString();
            user.Id = userId;
            var dir = Path.Combine(_dbSettings.FileRepository, "users", userId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var path = Path.Combine(dir, "user.json");
            File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
        }
    }
}
