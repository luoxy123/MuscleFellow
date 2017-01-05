using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json.Linq;
using Remotion.Linq;
using Sylvanas;
using Sylvanas.Exceptions;
using Sylvanas.Net.Http;
using Sylvanas.Utility;

namespace MuscleFellow.API.Controllers
{

    [Route("api/test")]
    public class OpenCityController : Controller
    {
        private readonly IOptions<WebApiSettings> _settings;
        private RestHttpClient _client;
        public OpenCityController(IOptions<WebApiSettings> settings)
        {
            _settings = settings;
            _client = new RestHttpClient();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
                _client = null;
            }
            base.Dispose(disposing);
        }


        [HttpGet]
        [Route("open")]
        public async Task<IActionResult> Get()
        {
                try
                {
                    _client.BaseUri = $"{_settings.Value.ServiceUrl}/basics";
                    var opencitys =
                        await
                            _client.GetAsync<string>("/api/common/OpenCity",
                                new Dictionary<string, object> {});

                    var jobj = JObject.Parse(opencitys);
                    return Ok(jobj);
                }
                catch (WebServiceException ex)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, ex.ResponseBody);
                }

                //_client.GetAsync<object>("/api/common/OpenCity",
                //    new Dictionary<string, object>
                //    {
                //        {"auto_query", new QueryModel {Pager = false}.ToString()}
                //    });
          
        }


        [HttpGet]
        [Route("ip")]
        public  IActionResult  GetIp()
        {
            var Ip= ClientHostAddressUtility.GetClientAddress(HttpContext);
            return Ok(new {Ip = Ip});
        }


    }
}