using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Sylvanas.Caching;

namespace Sylvanas
{
    public interface ICachedRestClientAsync : IRestClientAsync
    {
        int CacheCount { get; }
        long CacheHits { get; }
        long NotModifiedHits { get; }
        long ErrorFallbackHits { get; }
        long CachesAdded { get; }
        long CachesRemoved { get; }

        void SetCache(ConcurrentDictionary<string, HttpCacheEntry> cache);
        int RemoveCachesOlderThan(TimeSpan age);
        int RemoveExpiredCachesOlderThan(TimeSpan age);
    }

    public static class CachedRestClientAsyncExtensions
    {
        public static void ClearCache(this ICachedRestClientAsync client)
        {
            client.SetCache(new ConcurrentDictionary<string, HttpCacheEntry>());
        }

        public static Dictionary<string, string> GetStats(this ICachedRestClientAsync client)
        {
            return new Dictionary<string, string>
            {
                {"CacheCount", client.CacheCount + ""},
                {"CacheHits", client.CacheHits + ""},
                {"NotModifiedHits", client.NotModifiedHits + ""},
                {"ErrorFallbackHits", client.ErrorFallbackHits + ""},
                {"CachesAdded", client.CachesAdded + ""},
                {"CachesRemoved", client.CachesRemoved + ""}
            };
        }
    }
}