using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Sylvanas.Web;
using HttpHeaders = Sylvanas.Web.HttpHeaders;

namespace Sylvanas.Extensions
{
    public static class HttpExtensions
    {
        public static WebHeaderCollection ToWebHeaderCollection(this HttpResponseHeaders headers)
        {
            var to = new WebHeaderCollection();
            foreach (var header in headers)
            {
                to[header.Key] = string.Join(", ", header.Value);
            }
            return to;
        }

        public static string GetContentType(this HttpResponseMessage httpRes)
        {
            IEnumerable<string> values;
            return httpRes.Headers.TryGetValues(HttpHeaders.ContentType, out values) ? values.FirstOrDefault() : null;
        }

        public static async Task UploadFileAsync(this WebRequest webRequest, Stream fileStream, string fileName, string mimeType,
            string accept = null)
        {
            var httpRequest = (HttpWebRequest) webRequest;
            httpRequest.Method = HttpMethods.Post;

            if (accept != null)
            {
                httpRequest.Accept = accept;
            }

            var boundary = "----------------------------" + Guid.NewGuid().ToString("N");

            httpRequest.ContentType = "multipart/form-data; boundary=" + boundary;

            var boundarybytes = ("\r\n--" + boundary + "\r\n").ToAsciiBytes();

            var headerTemplate = "\r\n--" + boundary +
                                 "\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\nContent-Type: {1}\r\n\r\n";

            var header = string.Format(headerTemplate, fileName, mimeType);

            var headerbytes = header.ToAsciiBytes();

            using (var outputStream = await httpRequest.GetRequestStreamAsync())
            {
                outputStream.Write(headerbytes, 0, headerbytes.Length);

                fileStream.CopyTo(outputStream, 4096);

                outputStream.Write(boundarybytes, 0, boundarybytes.Length);

                outputStream.Flush();
            }
        }

        public static async Task UploadFileAsync(this WebRequest webRequest, Stream fileStream, string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            var mimeType = MimeTypes.GetMimeType(fileName);
            if (mimeType == null)
            {
                throw new ArgumentException("Mime-type not found for file: " + fileName);
            }

            await UploadFileAsync(webRequest, fileStream, fileName, mimeType);
        }
    }
}