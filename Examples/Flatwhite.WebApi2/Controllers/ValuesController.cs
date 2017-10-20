﻿using log4net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Flatwhite.WebApi.Owin.Controllers
{
    public class ValuesController : ApiController
    {
        private readonly ICoffeeService _coffeeService;
        private readonly ILog _logger;
        private static string _hint = "<br /><br /> <strong>Go to <a href='/_flatwhite/store/0' target='_blank'>/_flatwhite/store/{storeId}</a> to see cache statuses</strong>" +
                                      "<br /><br /> <strong>Go to <a href='/_flatwhite/phoenixes' target='_blank'>/_flatwhite/phoenixes</a> to see phoenix statuses</strong>";
        public ValuesController(ICoffeeService coffeeService, ILog logger)
        {
            _coffeeService = coffeeService;
            _logger = logger;
        }

        public ValuesController() : this(new FlatwhiteCoffeeService(), LogManager.GetLogger(typeof(ValuesController)))
        {
        }

        [WebApi.OutputCache(
            MaxAge = 3,
            StaleWhileRevalidate = 5,
            IgnoreRevalidationRequest = true)]
        public virtual async Task<IEnumerable<string>> Get()
        {
            await Task.Delay(2000);
            return new[] { "value1", "value2" };
        }


        [HttpGet]
        [Route("api/vary-by-param-async/{packageId}")]
        [WebApi.OutputCache(CacheProfile = "Profile1-Two-Seconds")]
        public async Task<HttpResponseMessage> VaryByParamAsync(string packageId)
        {
            var sw = Stopwatch.StartNew();

            await _coffeeService.OrderCoffeeAsync();
            sw.Stop();
            _logger.Info($"ActionMethod {nameof(VaryByParamAsync)} elapsed {sw.ElapsedMilliseconds} ms");
            return new HttpResponseMessage()
            {
                Content = new StringContent($"Download elapsed {sw.ElapsedMilliseconds} milliseconds.{_hint}", Encoding.UTF8, "text/html")
            };
        }


        [HttpGet]
        [Route("api/vary-by-param/{packageId}")]
        [WebApi.OutputCache(
            MaxAge = 2,
            StaleWhileRevalidate = 10,
            VaryByParam = "packageId",
            RevalidateKeyFormat = "VaryByParamMethod_{packageId}",
            IgnoreRevalidationRequest = true)]
        public HttpResponseMessage VaryByParam(string packageId)
        {
            var sw = Stopwatch.StartNew();
            _coffeeService.OrderCoffee();
            sw.Stop();
            _logger.Info($"ActionMethod {nameof(VaryByParam)} elapsed {sw.ElapsedMilliseconds} ms");
            return new HttpResponseMessage()
            {
                Content = new StringContent($"elapsed {sw.ElapsedMilliseconds} milliseconds.{_hint}", Encoding.UTF8, "text/html")
            };
        }


        [HttpGet]
        [Route("api/string/{packageId}")]
        [WebApi.OutputCache(
            MaxAge = 3,
            StaleWhileRevalidate = 5,
            VaryByParam = "packageId",
            RevalidateKeyFormat = "StringMethod_{packageId}",
            IgnoreRevalidationRequest = true)]
        public string String(string packageId)
        {
            return packageId;
        }


        [HttpGet]
        [Route("api/vary-by-header")]
        [WebApi.OutputCache(MaxAge = 3, StaleWhileRevalidate = 5, VaryByHeader = "UserAgent, CacheControl.Public", VaryByCustom = "query.src")]
        public virtual async Task<HttpResponseMessage> VaryByCustom()
        {
            var sw = Stopwatch.StartNew();
            await _coffeeService.OrderCoffeeAsync();
            sw.Stop();
            _logger.Info($"ActionMethod {nameof(VaryByCustom)} elapsed {sw.ElapsedMilliseconds} ms.{_hint}");
            return new HttpResponseMessage()
            {
                Content = new StringContent($"Elapsed {sw.ElapsedMilliseconds} milliseconds", Encoding.UTF8, "text/html")
            };
        }

        [HttpGet]
        [Route("api/reset/{packageId}")]
        [WebApi.Revalidate("VaryByParamMethod_{packageId}", "StringMethod_{packageId}")]
        public virtual HttpResponseMessage ResetCache(string packageId)
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
