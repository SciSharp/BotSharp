using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class WeChatUserDocument : MongoBase
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User unique identifier (unique under the current application)
    /// </summary>
    public string OpenId { get; set; } = string.Empty;

    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// User unique identifier (cross application unique, requiring open platform binding)
    /// </summary>
    public string UnionId { get; set; } = string.Empty;

    /// <summary>
    /// User's gender: 1- Male, 2- Female, 0- Unknown
    /// </summary>
    public int Sex { get; set; }

    /// <summary>
    /// The province where the user's personal information is filled in
    /// </summary>
    public string Province { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;

    /// <summary>
    /// User avatar URL (46/64/96/132/0 pixels)
    /// </summary>
    public string Headimgurl { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The country where the user is located, such as China CN
    /// </summary>
    public string Country { get; set; } = "CN";

    /// <summary>
    /// User privilege information (such as WeChat membership, etc.)
    /// </summary>
    public string[] Privilege { get; set; } = Array.Empty<string>();

    //public string AppId { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public WeChatUser ToWeChatUser()
    {
        return new WeChatUser
        {
            Id = Id,
            OpenId = OpenId,
            SessionKey = SessionKey,
            UnionId = UnionId,
            Sex = Sex,
            Province = Province,
            City = City,
            NickName = NickName,
            Headimgurl = Headimgurl,
            PhoneNumber = PhoneNumber,
            Country = Country,
            Privilege = Privilege,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}
