using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Sylvanas
{
    public interface IServiceGatewayAsync
    {
        Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request,
            CancellationToken token = default(CancellationToken));

        Task<WebResponse> PostFileAsync(string relativeOrAbsoluteUrl, FileInfo uploadFileInfo);
    }
}