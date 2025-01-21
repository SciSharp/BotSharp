using BotSharp.Abstraction.Infrastructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Infrastructures
{
    public class HybridCacheService : ICacheService
    {
        private readonly MemoryCacheService _memoryCache;
        private readonly RedisCacheService _redisCache;

        public HybridCacheService(
            MemoryCacheService memoryCache,
            RedisCacheService redisCache)
        {
            _memoryCache = memoryCache;
            _redisCache = redisCache;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var result = await _memoryCache.GetAsync<T>(key);
            if (result != null)
            {
                return result;
            }
            result = await _redisCache.GetAsync<T>(key);
            if (result != null)
            {
                await SetAsync(key, result, TimeSpan.FromMinutes(5));
            }
            return result;
        }

        public async Task<object> GetAsync(string key, Type type)
        {
            var result = await _memoryCache.GetAsync(key, type);
            if (result != null)
            {
                return result;
            }
            result = await _redisCache.GetAsync(key, type);
            if (result != null)
            {
                await SetAsync(key, result, TimeSpan.FromMinutes(5));
            }

            return result;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry)
        {
            await _memoryCache.SetAsync(key, value, expiry);
            await _redisCache.SetAsync(key, value, expiry);
        }

        public async Task RemoveAsync(string key)
        {
            await _memoryCache.RemoveAsync(key);
            await _redisCache.RemoveAsync(key);
        }
    }
}
