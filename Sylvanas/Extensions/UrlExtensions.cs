using Sylvanas.Web;

namespace Sylvanas.Extensions
{
    public static class UrlExtensions
    {
        public static bool HasRequestBody(this string httpMethod)
        {
            switch (httpMethod)
            {
                case HttpMethods.Get:
                case HttpMethods.Delete:
                case HttpMethods.Head:
                case HttpMethods.Options:
                    return false;
            }

            return true;
        }
    }
}