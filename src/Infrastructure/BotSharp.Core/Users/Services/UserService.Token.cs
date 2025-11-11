using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using BotSharp.OpenAPI.ViewModels.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BotSharp.Core.Users.Services;

public partial class UserService
{
    public async Task<Token?> GetToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password, regionCode) = base64.SplitAsTuple(":");

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = id.Contains("@") ? db.GetUserByEmail(id) : db.GetUserByUserName(id);
        if (record == null)
        {
            record = db.GetUserByPhone(id, regionCode: regionCode);
        }

        if (record != null && record.Type == UserType.Affiliate)
        {
            return default;
        }

        var hooks = _services.GetServices<IAuthenticationHook>();
        //verify password is correct or not.
        if (record != null && !hooks.Any())
        {
            var hashPassword = Utilities.HashTextMd5($"{password}{record.Salt}");
            if (hashPassword != record.Password)
            {
                return default;
            }
        }

        User? user = record;
        var isAuthenticatedByHook = false;
        if (record == null || record.Source != UserSource.Internal)
        {
            // check 3rd party user
            foreach (var hook in hooks)
            {
                user = await hook.Authenticate(id, password);
                if (user == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(user.Source) || user.Source == UserSource.Internal)
                {
                    _logger.LogError($"Please set source name in the Authenticate hook.");
                    return null;
                }

                if (record == null)
                {
                    // create a local user record
                    record = new User
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Source = user.Source,
                        ExternalId = user.ExternalId,
                        Password = user.Password,
                        Type = user.Type,
                        Role = user.Role,
                        RegionCode = user.RegionCode
                    };
                    await CreateUser(record);
                }

                isAuthenticatedByHook = true;
                break;
            }
        }

        if ((hooks.Any() && user == null) || record == null)
        {
            return default;
        }

        if (!isAuthenticatedByHook && _setting.NewUserVerification && !record.Verified)
        {
            return default;
        }

        if (!isAuthenticatedByHook && Utilities.HashTextMd5($"{password}{record.Salt}") != record.Password)
        {
            return default;
        }

        var (token, jwt) = BuildToken(record);
        foreach (var hook in hooks)
        {
            hook.UserAuthenticated(record, token);
        }

        return token;
    }

    public async Task<Token?> RenewToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            var (newToken, _) = BuildToken(await GetMyProfile());
            return newToken;
        }

        // Allow "Bearer {token}" input
        if (token.Contains(' '))
        {
            token = token.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
        }

        try
        {
            User? user = null;

            var hooks = _services.GetServices<IAuthenticationHook>();
            foreach (var hook in hooks)
            {
                user = await hook.RenewAuthentication(token);
                if (user != null)
                {
                    break;
                }
            }

            if (user == null)
            {
                // Validate the incoming JWT (signature, issuer, audience, lifetime)
                var config = _services.GetRequiredService<IConfiguration>();
                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config["Jwt:Key"])),
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var userId = principal?.Claims?
                    .FirstOrDefault(x => x.Type.IsEqualTo(JwtRegisteredClaimNames.NameId)
                                       || x.Type.IsEqualTo(ClaimTypes.NameIdentifier)
                                       || x.Type.IsEqualTo("uid")
                                       || x.Type.IsEqualTo("user_id")
                                       || x.Type.IsEqualTo("userId"))?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                user = await GetUser(userId);
                if (user == null
                    || user.IsDisabled
                    || (user.Id != userId && user.ExternalId != userId))
                {
                    return null;
                }
            }

            // Issue a new access token
            var (newToken, _) = BuildToken(user);

            // Notify hooks for token issuance
            foreach (var hook in hooks)
            {
                hook.UserAuthenticated(user, newToken);
            }

            return newToken;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid token presented for refresh.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token.");
            return null;
        }
    }

    public async Task<Token> ActiveUser(UserActivationModel model)
    {
        var id = model.UserName;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = id.Contains("@") ? db.GetUserByEmail(id) : db.GetUserByUserName(id);

        if (record == null)
        {
            record = db.GetUserByPhone(id, regionCode: (string.IsNullOrWhiteSpace(model.RegionCode) ? "CN" : model.RegionCode));
        }

        //if (record == null)
        //{
        //    record = db.GetUserByPhoneV2(id, regionCode: (string.IsNullOrWhiteSpace(model.RegionCode) ? "CN" : model.RegionCode));
        //}

        if (record == null)
        {
            return default;
        }

        if (record.VerificationCode != model.VerificationCode || (record.VerificationCodeExpireAt != null && DateTime.UtcNow > record.VerificationCodeExpireAt))
        {
            return default;
        }

        if (record.Verified)
        {
            return default;
        }

        db.UpdateUserVerified(record.Id);

        var accessToken = GenerateJwtToken(record);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var token = new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
        return token;
    }

    public async Task<Token> CreateTokenByUser(User user)
    {
        var accessToken = GenerateJwtToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var token = new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
        return token;
    }

    public async Task<Token?> GetAffiliateToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password, regionCode) = base64.SplitAsTuple(":");
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetAffiliateUserByPhone(id);
        var isCanLogin = record != null && !record.IsDisabled && record.Type == UserType.Affiliate;
        if (!isCanLogin)
        {
            return default;
        }

        if (Utilities.HashTextMd5($"{password}{record.Salt}") != record.Password)
        {
            return default;
        }

        var (token, jwt) = BuildToken(record);

        return await Task.FromResult(token);
    }

    public async Task<Token?> GetAdminToken(string authorization)
    {
        var base64 = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
        var (id, password, regionCode) = base64.SplitAsTuple(":");
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var record = db.GetUserByPhone(id, type: UserType.Internal);
        var isCanLogin = record != null && !record.IsDisabled
            && record.Type == UserType.Internal && new List<string>
            {
                UserRole.Root,UserRole.Admin
            }.Contains(record.Role);
        if (!isCanLogin)
        {
            return default;
        }

        if (Utilities.HashTextMd5($"{password}{record.Salt}") != record.Password)
        {
            return default;
        }

        var (token, jwt) = BuildToken(record);

        return await Task.FromResult(token);
    }

    public async Task<DateTime> GetUserTokenExpires()
    {
        var _cacheService = _services.GetRequiredService<ICacheService>();
        return await _cacheService.GetAsync<DateTime>(GetUserTokenExpiresCacheKey(_user.Id));
    }

    #region Private methods
    private (Token, JwtSecurityToken) BuildToken(User record)
    {
        var accessToken = GenerateJwtToken(record);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var token = new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
        return (token, jwt);
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user?.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.GivenName, user?.FirstName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.FamilyName, user?.LastName ?? string.Empty),
            new Claim("source", user.Source),
            new Claim("external_id", user.ExternalId ?? string.Empty),
            new Claim("type", user.Type ?? UserType.Client),
            new Claim("role", user.Role ?? UserRole.User),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("phone", user.Phone ?? string.Empty),
            new Claim("affiliate_id", user.AffiliateId ?? string.Empty),
            new Claim("employee_id", user.EmployeeId ?? string.Empty),
            new Claim("regionCode", user.RegionCode ?? "CN")
        };

        var validators = _services.GetServices<IAuthenticationHook>();
        foreach (var validator in validators)
        {
            validator.AddClaims(claims);
        }

        var config = _services.GetRequiredService<IConfiguration>();
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var expireInMinutes = int.Parse(config["Jwt:ExpireInMinutes"] ?? "120");
        var key = Encoding.ASCII.GetBytes(config["Jwt:Key"]);
        var expires = DateTime.UtcNow.AddMinutes(expireInMinutes);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        SaveUserTokenExpiresCache(user.Id, expires, expireInMinutes).GetAwaiter().GetResult();
        return tokenHandler.WriteToken(token);
    }

    private async Task SaveUserTokenExpiresCache(string userId, DateTime expires, int expireInMinutes)
    {
        var config = _services.GetService<IConfiguration>();
        var enableSingleLogin = bool.Parse(config["Jwt:EnableSingleLogin"] ?? "false");
        if (enableSingleLogin)
        {
            var _cacheService = _services.GetRequiredService<ICacheService>();
            await _cacheService.SetAsync(GetUserTokenExpiresCacheKey(userId), expires, TimeSpan.FromMinutes(expireInMinutes));
        }
    }

    private string GetUserTokenExpiresCacheKey(string userId)
    {
        return $"user:{userId}_token_expires";
    }
    #endregion
}
