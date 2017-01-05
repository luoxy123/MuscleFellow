using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Sylvanas.Utility
{
    public static class ClientHostAddressUtility
    {


        private const string ClientHostAddressHeaderKey = "X-Forwarded-For";

        public static string GetClientAddress(HttpContext context)
        {


            var request = context.Request;
            var customAddresses = request.Headers[ClientHostAddressHeaderKey];

            if (customAddresses.Any())
                return customAddresses.First();

            return request.Host.Host;
        }

    }
}