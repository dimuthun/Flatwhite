﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Flatwhite.WebApi;
using Flatwhite.WebApi.CacheControl;
using NSubstitute;
using NUnit.Framework;

namespace Flatwhite.Tests.WebApi.CacheControl
{
    [TestFixture]
    public class EtagHeaderHandlerTests
    {

        [SetUp]
        public void SetUp()
        {
            Global.Init();
        }

        [Test]
        public void Should_throw_if_ICacheResponseBuilder_is_null()
        {
            // Arrange
            Assert.Throws<ArgumentNullException>(() => new EtagHeaderHandler(null));
        }

        [Test]
        public async Task Should_return_null_if_no_etag_in_request()
        {
            // Arrange
            var cacheControl = new CacheControlHeaderValue();
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://localhost")
            };
            var builder = Substitute.For<ICacheResponseBuilder>();
            var handler = new EtagHeaderHandler(builder);
            
            // Action
            var response = await handler.HandleAsync(cacheControl, request, CancellationToken.None);

            Assert.IsNull(response);
        }

        [Test]
        public async Task Should_return_null_if_cache_item_not_found()
        {
            // Arrange
            var cacheControl = new CacheControlHeaderValue();
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri("http://localhost")
            };
            request.Headers.Add("If-None-Match", "\"fw-1000-HASHEDKEY-CHECKSUM\"");
            var builder = Substitute.For<ICacheResponseBuilder>();
            var handler = new EtagHeaderHandler(builder);

            // Action
            var response = await handler.HandleAsync(cacheControl, request, CancellationToken.None);

            Assert.IsNull(response);
        }

        [TestCase("NEWCHECKSUM", HttpStatusCode.OK)]
        [TestCase("OLDCHECKSUM", HttpStatusCode.NotModified)]
        public async Task Should_return_new_etag_if_cache_item_found_but_doesnt_match_checksum(string cacheChecksum, HttpStatusCode resultCode)
        {
            // Arrange
            var cacheControl = new CacheControlHeaderValue
            {
                MaxStale = true,
                MaxStaleLimit = TimeSpan.FromSeconds(15),
                MinFresh = TimeSpan.FromSeconds(20)
            };

            var oldCacheItem = new WebApiCacheItem
            {
                CreatedTime = DateTime.UtcNow.AddSeconds(-11),
                MaxAge = 10,
                StaleWhileRevalidate = 5,
                IgnoreRevalidationRequest = true,
                ResponseCharSet = "UTF8",
                ResponseMediaType = "text/json",
                Content = new byte[0],
                Key = "fw-0-HASHEDKEY",
                Checksum = cacheChecksum
            };

            var request = UnitTestHelper.GetMessage();
            request.Headers.Add("If-None-Match", "\"fw-0-HASHEDKEY-OLDCHECKSUM\"");
            var builder = new CacheResponseBuilder();
            var handler = new EtagHeaderHandler(builder);
            await Global.CacheStoreProvider.GetAsyncCacheStore().SetAsync("fw-0-HASHEDKEY", oldCacheItem, DateTimeOffset.Now.AddDays(1)).ConfigureAwait(false);


            // Action
            Global.Cache.PhoenixFireCage["fw-0-HASHEDKEY"] = new WebApiPhoenix(oldCacheItem, request);
            var response = await handler.HandleAsync(cacheControl, request, CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(resultCode, response.StatusCode);
            if (resultCode == HttpStatusCode.OK)
            {
                Assert.AreEqual($"\"fw-0-HASHEDKEY-{cacheChecksum}\"", response.Headers.ETag.Tag);
            }
            else
            {
                Assert.IsNull(response.Headers.ETag);
            }
        }
    }
}
