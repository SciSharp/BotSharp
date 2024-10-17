using BotSharp.Abstraction.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class UserMappers
{
    public static User ToModel(this Entities.User entity)
    {
        return new User
        {
            Id = entity.Id,
            UserName = entity.UserName,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Salt = entity.Salt,
            Password = entity.Password,
            Email = entity.Email,
            Phone = entity.Phone,
            Source = entity.Source,
            ExternalId = entity.ExternalId,
            Role = entity.Role,
            VerificationCode = entity.VerificationCode,
            Verified = entity.Verified,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime
        };
    }
}
