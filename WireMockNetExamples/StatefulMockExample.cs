using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;
using System.Diagnostics;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WireMockNetExamples
{
    [TestFixture]
    public class StatefulMockExample
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

        private void CreateStatefulStub()
        {
            server.Given(
                Request.Create().WithPath("/todo/items").UsingGet()
            )
            // In this scenario, when the current state is 'TodoList State Started',
            // a call to an HTTP GET will only return 'Buy milk'
           .InScenario("To do list")
           .WillSetStateTo("TodoList State Started")
           .RespondWith(
                Response.Create().WithBody("Buy milk")
           );

            server.Given(
                Request.Create().WithPath("/todo/items").UsingPost()
            )
            // In this scenario, when the current state is 'TodoList State Started',
            // a call to an HTTP POST will trigger a state transition to new state
            // 'Cancel newspaper item added'
            .InScenario("To do list")
            .WhenStateIs("TodoList State Started")
            .WillSetStateTo("Cancel newspaper item added")
            .RespondWith(
                Response.Create().WithStatusCode(201)
            );

            server.Given(
                Request.Create().WithPath("/todo/items").UsingGet()
            )
            // In this scenario, when the current state is 'Cancel newspaper item added',
            // a call to an HTTP GET will return 'Buy milk;Cancel newspaper subscription'
            .InScenario("To do list")
            .WhenStateIs("Cancel newspaper item added")
            .RespondWith(
                Response.Create().WithBody("Buy milk;Cancel newspaper subscription")
            );
        }

        [Test]
        public async Task TestStatefulStub()
        {
            CreateStatefulStub();

            RestRequest request = new RestRequest("/todo/items", Method.Get);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.EqualTo("Buy milk"));

            request = new RestRequest("/todo/items", Method.Post);

            response = await client.ExecuteAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            request = new RestRequest("/todo/items", Method.Get);

            response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.EqualTo("Buy milk;Cancel newspaper subscription"));
        }

        [TearDown]
        public void StopServer()
        {
            server.Stop();
        }

    }
}