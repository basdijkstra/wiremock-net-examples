using NUnit.Framework;
using RestSharp;
using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WireMockNetExamples
{
    [TestFixture]
    public class ResponseTemplatingExamples
    {
        private WireMockServer server;

        private RestClient client;

        private const string BASE_URL = "http://localhost:9876";

        [OneTimeSetUp]
        public void SetupRestSharpClient()
        {
            client = new RestClient(BASE_URL);
        }

        [SetUp]
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

        [TestCase(Method.Get, "GET", TestName = "Check that GET method is echoed successfully")]
        [TestCase(Method.Post, "POST", TestName = "Check that POST method is echoed successfully")]
        [TestCase(Method.Delete, "DELETE", TestName = "Check that DELETE method is echoed successfully")]
        public async Task TestStubEchoHttpMethod(Method method, string expectedResponseMethod)
        {
            CreateStubEchoHttpMethod();

            RestRequest request = new RestRequest("/echo-http-method", method);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.EqualTo($"HTTP method used was {expectedResponseMethod}"));
        }

        [TestCase("Pillars of the Earth", "Ken Follett", TestName = "Check for Pillars of the Earth")]
        [TestCase("The Secret History", "Donna Tartt", TestName = "Check for The Secret History")]
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

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.EqualTo($"The specified book title is {title}"));
        }

        [TearDown]
        public void StopServer()
        {
            server.Stop();
        }

    }
}