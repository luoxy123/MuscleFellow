using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sylvanas
{
    public interface IRestClientAsync : IServiceGatewayAsync, IDisposable
    {
        Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request);

        Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request);

        Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, IDictionary<string, object> request);

        Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request);

        Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request);
    }
}