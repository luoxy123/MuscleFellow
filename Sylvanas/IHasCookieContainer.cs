using System.Net;

namespace Sylvanas
{
    public interface IHasCookieContainer
    {
        CookieContainer CookieContainer { get; }
    }
}