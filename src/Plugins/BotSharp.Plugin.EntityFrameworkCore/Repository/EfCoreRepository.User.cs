using BotSharp.Abstraction.Users.Models;
using BotSharp.Plugin.EntityFrameworkCore.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;
public partial class EfCoreRepository
{
    public User? GetUserByEmail(string email)
    {
        var user = _context.Users.FirstOrDefault(x => x.Email == email.ToLower());
        return user?.ToModel();
    }

    public User? GetUserById(string id)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id || (x.ExternalId != null && x.ExternalId == id));
        return user?.ToModel();
    }

    public User? GetUserByUserName(string userName)
    {
        var user = _context.Users.FirstOrDefault(x => x.UserName == userName.ToLower());
        return user?.ToModel();
    }

    public void CreateUser(User user)
    {
        if (user == null) return;

        var userEntity = new Entities.User
        {
            Id = user.Id ?? Guid.NewGuid().ToString(),
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Salt = user.Salt,
            Password = user.Password,
            Email = user.Email,
            Phone = user.Phone,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Role = user.Role,
            VerificationCode = user.VerificationCode,
            Verified = user.Verified,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _context.Users.Add(userEntity);
        _context.SaveChanges();
    }

    public void UpdateUserVerified(string userId)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user != null)
        {
            user.Verified = true;
            user.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    public void UpdateUserVerificationCode(string userId, string verficationCode)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user != null)
        {
            user.VerificationCode = verficationCode;
            user.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    public void UpdateUserPassword(string userId, string password)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user != null)
        {
            user.Password = password;
            user.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    public void UpdateUserEmail(string userId, string email)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user != null)
        {
            user.Email = email;
            user.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    public void UpdateUserPhone(string userId, string phone)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user != null)
        {
            user.Phone = phone;
            user.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }
    public void UpdateUserRole(string userId, string role)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user != null)
        {
            user.Role = role;
            user.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }
}