using System;
using System.Threading.Tasks;

namespace Tweetbook.Services
{
    public interface IResponseCacheService
    {
        Task cacheResponseAsync(string cacheKey, object response, TimeSpan timeToLive);
        Task<string> GetCacheResponseAsync(string cacheKey);
    }
}