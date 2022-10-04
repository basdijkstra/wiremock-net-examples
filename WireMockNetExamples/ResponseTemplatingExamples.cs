using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WireMockNetExamples
{
    [TestClass]
    public class ResponseTemplatingExamples
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

        private void CreateStubEchoHttpMethod()
        {
            server.Given(                
                Request.Create().WithPath("/echo-http-method").UsingAnyMethod()
            )
            .RespondWith(
                Response.Create()
                .WithStatusCode(200)
                // The {{request.method}} handlebar extracts the HTTP method from the request
                .WithBody("HTTP method used was {{request.method}}")
                // This enables response templating for this specific mock response
                .WithTransformer()
            );
        }

        private void CreateStubEchoJsonRequestElement()
        {
            server.Given(
                Request.Create().WithPath("/echo-json-request-element").UsingPost()
            )
            .RespondWith(
                Response.Create()
                .WithStatusCode(200)
                // This extracts the book.title element from the JSON request body
                // (using a JsonPath expression) and repeats it in the response body
                .WithBody("The specified book title is {{JsonPath.SelectToken request.body \"$.book.title\"}}")
                .WithTransformer()
            );
        }

        [TestMethod]
        [DataRow(Method.Get, "GET")]
        [DataRow(Method.Post, "POST")]
        [DataRow(Method.Delete, "DELETE")]
        public async Task TestStubEchoHttpMethod(Method method, string expectedResponseMethod)
        {
            CreateStubEchoHttpMethod();

            RestRequest request = new RestRequest("/echo-http-method", method);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual($"HTTP method used was {expectedResponseMethod}", response.Content);
        }

        [TestMethod]
        [DataRow("Pillars of the Earth", "Ken Follett")]
        [DataRow("The Secret History", "Donna Tartt")]
        public async Task TestStubEchoRequestJSONBodyElementValue(string title, string author)
        {
            CreateStubEchoJsonRequestElement();

            var requestBody = new
            {
                book = new
                {
                    title = title,
                    author = author
                }
            };

            RestRequest request = new RestRequest("/echo-json-request-element", Method.Post);

            request.AddJsonBody(requestBody);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual($"The specified book title is {title}", response.Content);
        }

        [TestCleanup]
        public void StopServer()
        {
            server.Stop();
        }

    }
}