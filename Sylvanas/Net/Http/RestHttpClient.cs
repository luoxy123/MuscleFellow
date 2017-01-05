using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sylvanas.Exceptions;
using Sylvanas.Extensions;

using Sylvanas.Web;
using HttpHeaders = Sylvanas.Web.HttpHeaders;

namespace Sylvanas.Net.Http
{
    public class RestHttpClient : IRestClientAsync, IHasCookieContainer
    {
        public const string DefaultHttpMethod = HttpMethods.Post;
        public static string DefaultUserAgent = "Sylvanas .NET HttpClient " + Environment.SylvanasVersion;
        private int _activeAsyncRequests;

        public RestHttpClient()
        {
            Format = "json";
            Headers = new NameValueCollectionWrapper(new NameValueCollection());
            CookieContainer = new CookieContainer();
            ContentType = MimeTypes.Json;
            Accept = MimeTypes.Json;
            EnableLogger = true;

        }

        public RestHttpClient(string baseUri) : this()
        {
            BaseUri = baseUri;
        }

        public bool EnableLogger { get; set; }
        public HttpMessageHandler HttpMessageHandler { get; set; }
        public static Func<HttpMessageHandler> GlobalHttpMessageHandlerFactory { get; set; }

        public HttpClient HttpClient { get; set; }

        public string BaseUri { get; set; }
        public string Format { get; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public CancellationTokenSource CancelTokenSource { get; set; }
        public INameValueCollection Headers { get; set; }

        public ICredentials Credentials { get; set; }
        public string BearerToken { get; set; }

        public CookieContainer CookieContainer { get; set; }

        internal Action<HttpRequestMessage> RequestFilter { get; set; }
        internal Func<Type, string, string, object, object> ResultsFilter { get; set; }
        internal Action<HttpResponseMessage, object, string, string, object> ResultsFilterResponse { get; set; }
        internal Func<HttpResponseMessage, string, Type, object> ExceptionFilter { get; set; }

        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request,
            CancellationToken token = default(CancellationToken))
        {
            if (!httpMethod.HasRequestBody() && (request != null))
            {
                if (!request.GetType().IsAssignableFrom(typeof(IDictionary)) &&
                    !request.GetType().HasInterface(typeof(IDictionary)))
                    throw new NotSupportedException("在Uri中传递参数时，参数类型必须从IDictionary继承");

                var map = (IDictionary) request;
                var queryString = QueryStringSerializer.SerializeToString(map);
                if (!string.IsNullOrEmpty(queryString))
                    absoluteUrl += "?" + queryString;
            }

            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, absoluteUrl, request);
                if (response is TResponse)
                {
                    var tcs = new TaskCompletionSource<TResponse>();
                    tcs.SetResult((TResponse) response);
                    return tcs.Task;
                }
            }

            var client = GetHttpClient();

            var httpRequest = new HttpRequestMessage(new HttpMethod(httpMethod), absoluteUrl);

            foreach (var name in Headers.AllKeys)
                httpRequest.Headers.Add(name, Headers[name]);
            httpRequest.Headers.Add(HttpHeaders.Accept, Accept);
            httpRequest.Headers.Add(HttpHeaders.UserAgent, DefaultUserAgent);

            if (httpMethod.HasRequestBody() && (request != null))
            {
                var httpContent = request as HttpContent;
                if (httpContent != null)
                {
                    httpRequest.Content = httpContent;
                }
                else
                {
                    var str = request as string;
                    var bytes = request as byte[];
                    var stream = request as Stream;

                    if (str != null)
                    {
                        httpRequest.Content = new StringContent(str);
                        httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
                    }
                    else if (bytes != null)
                    {
                        httpRequest.Content = new ByteArrayContent(bytes);
                    }
                    else if (stream != null)
                    {
                        httpRequest.Content = new StreamContent(stream);
                    }
                    else
                    {
                        if (ContentType == MimeTypes.Json)
                        {
                            httpRequest.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                                ContentType);
                        }
                        else if (ContentType == MimeTypes.FormUrlEncoded)
                        {
                            if (!request.GetType().IsAssignableFrom(typeof(IDictionary)) &&
                                !request.GetType().HasInterface(typeof(IDictionary)))
                                throw new NotSupportedException(
                                    "当请求体的数据格式为x-www-form-urlencoded时，传递的参数类型必须从IDictionary继承");

                            var map = (IDictionary) request;
                            httpRequest.Content = new StringContent(map.ToFormUrlEncoded(), Encoding.UTF8, ContentType);
                        }
                    }
                }
            }

            ApplyWebRequestFilters(httpRequest);

            Interlocked.Increment(ref _activeAsyncRequests);

            if (token == default(CancellationToken))
            {
                if (CancelTokenSource == null)
                    CancelTokenSource = new CancellationTokenSource();
                token = CancelTokenSource.Token;
            }

            if (EnableLogger)
            {
            }

            var sendAsyncTask = client.SendAsync(httpRequest, token);

            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (Task<TResponse>) (object) sendAsyncTask;

            return sendAsyncTask.ContinueWith(responseTask =>
            {
                var httpRes = responseTask.Result;

                if (!httpRes.IsSuccessStatusCode)
                {
                    var cachedResponse = ExceptionFilter?.Invoke(httpRes, absoluteUrl, typeof(TResponse));
                    if (cachedResponse is TResponse)
                    {
                        return Task.FromResult((TResponse)cachedResponse);
                    }

                    ThrowIfError(responseTask, httpRes);
                }

                if (typeof(TResponse) == typeof(string))
                    return httpRes.Content.ReadAsStringAsync().ContinueWith(task =>
                    {
                        var response = (TResponse) (object) task.Result;

                        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                        return response;
                    }, token);

                if (typeof(TResponse) == typeof(byte[]))
                    return httpRes.Content.ReadAsByteArrayAsync().ContinueWith(task =>
                    {
                        var response = (TResponse) (object) task.Result;

                        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                        return response;
                    }, token);

                if (typeof(TResponse) == typeof(Stream))
                    return httpRes.Content.ReadAsStreamAsync().ContinueWith(task =>
                    {
                        var response = (TResponse) (object) task.Result;

                        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                        return response;
                    }, token);

                return httpRes.Content.ReadAsStringAsync().ContinueWith(task =>
                {
                    var body = task.Result;
                    var response = JsonConvert.DeserializeObject<TResponse>(body);

                    ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                    return response;
                }, token);
            }, token).Unwrap();
        }

        public async Task<WebResponse> PostFileAsync(string relativeOrAbsoluteUrl, FileInfo uploadFileInfo)
        {
            var httpRequest = WebRequest.CreateHttp(ToAbsoluteUrl(relativeOrAbsoluteUrl));
            using (var fileStream = uploadFileInfo.OpenRead())
            {
                var fileName = uploadFileInfo.Name;
                await httpRequest.UploadFileAsync(fileStream, fileName);
            }

            return await httpRequest.GetResponseAsync();
        }

        public void Dispose()
        {
        }

        public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request)
        {
            return SendAsync<TResponse>(HttpMethods.Get, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(HttpMethods.Post, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request)
        {
            return SendAsync<TResponse>(HttpMethods.Delete, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(HttpMethods.Put, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl,
            object request)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            return SendAsync<TResponse>(httpVerb, ToAbsoluteUrl(relativeOrAbsoluteUrl), request);
        }

        public virtual string ToAbsoluteUrl(string relativeOrAbsoluteUrl)
        {
            return relativeOrAbsoluteUrl.StartsWith("http:") || relativeOrAbsoluteUrl.StartsWith("https:")
                ? relativeOrAbsoluteUrl
                : BaseUri.CombineWith(relativeOrAbsoluteUrl);
        }

        public HttpClient GetHttpClient()
        {
            //Should reuse same instance: http://social.msdn.microsoft.com/Forums/en-US/netfxnetcom/thread/4e12d8e2-e0bf-4654-ac85-3d49b07b50af/
            if (HttpClient != null)
                return HttpClient;

            if ((HttpMessageHandler == null) && (GlobalHttpMessageHandlerFactory != null))
                HttpMessageHandler = GlobalHttpMessageHandlerFactory();

            var handler = HttpMessageHandler ?? new HttpClientHandler
                          {
                              UseCookies = true,
                              CookieContainer = CookieContainer,
                              UseDefaultCredentials = Credentials == null,
                              Credentials = Credentials,
                              AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                          };

            var baseUri = BaseUri != null ? new Uri(BaseUri) : null;
            var client = new HttpClient(handler) {BaseAddress = baseUri};

            if (BearerToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);

            return HttpClient = client;
        }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }

        private void ApplyWebRequestFilters(HttpRequestMessage request)
        {
            RequestFilter?.Invoke(request);
        }

        private void ThrowIfError(Task task, HttpResponseMessage httpRes)
        {
            DisposeCancelToken();

            if (task.IsFaulted)
                throw new WebServiceException("请求任务失败", task.Exception);

            if (!httpRes.IsSuccessStatusCode)
            {
                var exception = new WebServiceException(httpRes.ReasonPhrase)
                {
                    StatusCode = (int) httpRes.StatusCode,
                    ResponseHeaders = httpRes.Headers.ToWebHeaderCollection(),
                    ResponseBody = httpRes.Content.ReadAsStringAsync().Result
                };
                throw exception;
            }
        }

        private void DisposeCancelToken()
        {
            if (Interlocked.Decrement(ref _activeAsyncRequests) > 0)
                return;
            if (CancelTokenSource == null)
                return;

            CancelTokenSource.Dispose();
            CancelTokenSource = null;
        }
    }
}