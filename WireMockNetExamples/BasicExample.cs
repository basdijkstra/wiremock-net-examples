using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WireMockNetExamples
{
    [TestClass]
    public class BasicExample
    {
        private WireMockServer server;

        private RestClient client;

        private const string BASE_URL = "http://localhost:9876";

        [ClassInitialize]
        public void SetupRestSharpClient()
        {
            client = new RestClient(BASE_URL);
        }

        [TestInitialize]
        public void StartServer()
        {
            server = WireMockServer.Start(9876);
        }

        private void CreateHelloWorldStub()
        {
            server.Given(
                Request.Create().WithPath("/hello-world").UsingGet()
            )
            .RespondWith(
                Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain")
                .WithBody("Hello, world!")
            );
        }

        [TestMethod]
        public async Task TestHelloWorldStub()
        {
            CreateHelloWorldStub();

            RestRequest request = new RestRequest("/hello-world", Method.Get);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("text/plain", response.ContentType);
            Assert.AreEqual("Hello, world!", response.Content);
        }

        [TestCleanup]
        public void StopServer()
        {
            server.Stop();
        }

    }
}