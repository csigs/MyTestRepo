

namespace Microsoft.Localization.Lego.FunctionTestHelper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Juno.NewtonsoftHelp;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Primitives;
    using Moq;

    public abstract class FunctionTest
    {
        public HttpRequest HttpRequestSetup(Dictionary<string, StringValues> query, string body, string contentType = null)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            reqMock.Setup(req => req.ContentType).Returns(contentType);
            reqMock.Setup(req => req.HttpContext.Request.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            reqMock.Setup(req => req.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();            
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);
            return reqMock.Object;
        }

        public HttpRequest HttpGetRequestSetup(Dictionary<string, StringValues> query)
        {
            var httpResMock = new Mock<HttpResponse>(MockBehavior.Strict);
            httpResMock
                .Setup(r => r.Headers)
                .Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));

            var httpCtxMock = new Mock<HttpContext>(MockBehavior.Strict);
            httpCtxMock
                .Setup(c => c.Response)
                .Returns(httpResMock.Object);

            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            reqMock.Setup(req => req.Method).Returns("GET");
            reqMock.Setup(req => req.Body).Returns(new MemoryStream());
            reqMock
                .Setup(req => req.HttpContext)
                .Returns(httpCtxMock.Object);
            reqMock.Setup(req => req.HttpContext.Request.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            reqMock.Setup(req => req.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            return reqMock.Object;
        }

        public HttpRequest HttpPostRequestSetup(Dictionary<string, StringValues> query)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            reqMock.Setup(req => req.Method).Returns("POST");
            reqMock.Setup(req => req.Body).Returns(new MemoryStream());
            reqMock.Setup(req => req.HttpContext.Request.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            reqMock.Setup(req => req.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            return reqMock.Object;
        }

        public HttpRequest HttpPostRequestSetup(Dictionary<string, StringValues> query, string body, string contentType = "application/json", Dictionary<string, StringValues> httpHeaders = null)
        {
            var httpResMock = new Mock<HttpResponse>(MockBehavior.Strict);
            httpResMock
                .Setup(r => r.Headers)
                .Returns(new HeaderDictionary(httpHeaders ?? new Dictionary<string, StringValues>()));
                       

            var reqMock = new Mock<HttpRequest>();
            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            reqMock.Setup(req => req.Method).Returns("POST");
            reqMock.Setup(req => req.ContentType).Returns(contentType);
            reqMock
                .Setup(req => req.Headers)
                .Returns(() =>
                    new HeaderDictionary(httpHeaders ?? new Dictionary<string, StringValues>()));

            var httpCtxMock = new Mock<HttpContext>(MockBehavior.Strict);
            httpCtxMock
                .Setup(c => c.Response)
                .Returns(httpResMock.Object);
            httpCtxMock
                .Setup(c => c.Request)
                .Returns(reqMock.Object);

            reqMock.Setup(req => req.HttpContext)
                .Returns(httpCtxMock.Object);

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);
            return reqMock.Object;
        }

        public HttpRequest HttpDeleteRequestSetup(Dictionary<string, StringValues> query)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            reqMock.Setup(req => req.Method).Returns("DELETE");
            reqMock.Setup(req => req.Body).Returns(new MemoryStream());
            reqMock.Setup(req => req.HttpContext.Request.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            reqMock.Setup(req => req.Headers).Returns(new HeaderDictionary(new Dictionary<string, StringValues>()));
            return reqMock.Object;
        }
    }

    public class AsyncCollector<T> : IAsyncCollector<T>
    {
        public readonly List<T> Items = new List<T>();

        public Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {

            Items.Add(item);

            return Task.FromResult(true);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true);
        }
    }
}
