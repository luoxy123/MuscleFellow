using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Sylvanas.Caching;
using Sylvanas.Web;

namespace Sylvanas.Net.Http
{
    public class CachedHttpClient : ICachedRestClientAsync
    {
        private readonly RestHttpClient _client;
        private ConcurrentDictionary<string, HttpCacheEntry> _cache = new ConcurrentDictionary<string, HttpCacheEntry>();

        private long _cacheHit;

        private long _cachesAdded;

        private long _cachesRemove;

        private long _errorFallbackHits;

        private long _notModifiedHits;

        public CachedHttpClient(RestHttpClient client)
        {
            _client = client;
            _client.RequestFilter = OnRequestFilter;
            _client.ResultsFilter = OnResultsFilter;
            _client.ResultsFilterResponse = OnResultsFilterResponse;
            _client.ExceptionFilter = OnExceptionFilter;
        }

        public CachedHttpClient(RestHttpClient client, ConcurrentDictionary<string, HttpCacheEntry> cache)
            : this(client)
        {
            if (_cache != null)
                _cache = cache;
        }

        public TimeSpan? ClearCachesOlderThan { get; set; }
        public TimeSpan? ClearExpiredCachesOlderThan { get; set; }

        public int CleanCachesWhenCountExceeds { get; set; }

        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request,
            CancellationToken token = new CancellationToken())
        {
            return _client.SendAsync<TResponse>(httpMethod, absoluteUrl, request, token);
        }

        public Task<WebResponse> PostFileAsync(string relativeOrAbsoluteUrl, FileInfo uploadFileInfo)
        {
            return _client.PostFileAsync(relativeOrAbsoluteUrl, uploadFileInfo);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request)
        {
            return _client.GetAsync<TResponse>(relativeOrAbsoluteUrl, request);
        }

        public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return _client.PostAsync<TResponse>(relativeOrAbsoluteUrl, request);
        }

        public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request)
        {
            return _client.DeleteAsync<TResponse>(relativeOrAbsoluteUrl, request);
        }

        public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return _client.PutAsync<TResponse>(relativeOrAbsoluteUrl, request);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl,
            object request)
        {
            return _client.CustomMethodAsync<TResponse>(httpVerb, relativeOrAbsoluteUrl, request);
        }

        public int CacheCount => _cache.Count;
        public long CacheHits => _cacheHit;
        public long NotModifiedHits => _notModifiedHits;
        public long ErrorFallbackHits => _errorFallbackHits;
        public long CachesAdded => _cachesAdded;
        public long CachesRemoved => _cachesRemove;

        public void SetCache(ConcurrentDictionary<string, HttpCacheEntry> cache)
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));
            _cache = cache;
        }

        public int RemoveCachesOlderThan(TimeSpan age)
        {
            var keysToRemove = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var entry in _cache)
                if (now - entry.Value.Created > age)
                    keysToRemove.Add(entry.Key);

            foreach (var key in keysToRemove)
            {
                HttpCacheEntry ignore;
                if (_cache.TryRemove(key, out ignore))
                    Interlocked.Increment(ref _cachesRemove);
            }

            return keysToRemove.Count;
        }

        public int RemoveExpiredCachesOlderThan(TimeSpan age)
        {
            var keysToRemove = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var entry in _cache)
                if (now - entry.Value.Expires > age)
                    keysToRemove.Add(entry.Key);

            foreach (var key in keysToRemove)
            {
                HttpCacheEntry ignore;
                if (_cache.TryRemove(key, out ignore))
                    Interlocked.Increment(ref _cachesRemove);
            }

            return keysToRemove.Count;
        }

        private void OnRequestFilter(HttpRequestMessage request)
        {
            HttpCacheEntry entry;
            if ((request.Method.Method == HttpMethods.Get) &&
                _cache.TryGetValue(request.RequestUri.ToString(), out entry))
            {
                if (entry.ETag != null)
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(entry.ETag.StripWeakRef(),
                        entry.ETag.StartsWith("W/")));

                if (entry.LastModified != null)
                    request.Headers.IfModifiedSince = entry.LastModified.Value;
            }
        }

        private object OnResultsFilter(Type responseType, string httpMethod, string requestUri, object request)
        {
            HttpCacheEntry entry;
            if ((httpMethod == HttpMethods.Get) && _cache.TryGetValue(requestUri, out entry))
                if (!entry.ShouldRevalidate())
                {
                    Interlocked.Increment(ref _cacheHit);
                    return entry.Response;
                }

            return null;
        }

        private void OnResultsFilterResponse(HttpResponseMessage webRes, object response, string httpMethod,
            string requestUri, object request)
        {
            if ((httpMethod != HttpMethods.Get) || (response == null) || (webRes == null))
                return;

            var eTag = webRes.Headers.ETag?.Tag;
            if ((eTag == null) && (webRes.Content.Headers.LastModified == null))
                return;

            var entry = new HttpCacheEntry(response)
            {
                ETag = eTag,
                ContentLength = webRes.Content.Headers.ContentLength
            };

            if (webRes.Content.Headers.LastModified != null)
                entry.LastModified = webRes.Content.Headers.LastModified.Value.UtcDateTime;

            entry.Age = webRes.Headers.Age;

            var cacheControl = webRes.Headers.CacheControl;
            if (cacheControl != null)
            {
                if (cacheControl.NoCache)
                    return;

                if (cacheControl.MaxAge != null)
                    entry.MaxAge = cacheControl.MaxAge.Value;

                entry.MustRevalidate = cacheControl.MustRevalidate;
                entry.NoCache = cacheControl.NoCache;

                entry.SetMaxAge(entry.MaxAge);
                _cache[requestUri] = entry;
                Interlocked.Increment(ref _cachesAdded);

                var runCleanupAfterEvery = CleanCachesWhenCountExceeds;
                if ((_cachesAdded%runCleanupAfterEvery == 0) && (_cache.Count > CleanCachesWhenCountExceeds))
                {
                    if (ClearExpiredCachesOlderThan != null)
                        RemoveExpiredCachesOlderThan(ClearExpiredCachesOlderThan.Value);
                    if (ClearCachesOlderThan != null)
                        RemoveCachesOlderThan(ClearCachesOlderThan.Value);
                }
            }
        }

        private object OnExceptionFilter(HttpResponseMessage webRes, string requestUri, Type responseType)
        {
            HttpCacheEntry entry;
            if (_cache.TryGetValue(requestUri, out entry))
            {
                if (webRes.StatusCode == HttpStatusCode.NotModified)
                {
                    Interlocked.Increment(ref _notModifiedHits);
                    return entry.Response;
                }
                if (entry.CanUseCacheOnError())
                {
                    Interlocked.Increment(ref _errorFallbackHits);
                    return entry.Response;
                }
            }

            return null;
        }
    }

    public static class CachedHttpClientExtensions
    {
        public static ICachedRestClientAsync WithCache(this RestHttpClient client)
        {
            return new CachedHttpClient(client);
        }

        public static ICachedRestClientAsync WithCache(this RestHttpClient client,
            ConcurrentDictionary<string, HttpCacheEntry> cache)
        {
            return new CachedHttpClient(client, cache);
        }

        internal static string StripWeakRef(this string etag)
        {
            return (etag != null) && etag.StartsWith("W/") ? etag.Substring(2) : etag;
        }
    }
}