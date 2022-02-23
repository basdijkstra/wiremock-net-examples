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
    public class FaultSimulationExamples
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

        private void CreateStubReturningDelayedResponse()
        {
            server.Given(
                Request.Create().WithPath("/delay").UsingGet()
            )
            .RespondWith(
                Response.Create()
                .WithStatusCode(200)
                // this returns the response after a 2000ms delay
                .WithDelay(TimeSpan.FromMilliseconds(2000))
            );
        }

        private void CreateStubReturningFault()
        {
            server.Given(
                Request.Create().WithPath("/fault").UsingGet()
            )
            .RespondWith(
                Response.Create()
                // returns a response with HTTP status code 200
                // and garbage in the response body
                .WithFault(FaultType.MALFORMED_RESPONSE_CHUNK)
            );
        }

        [Test]
        public async Task TestStubDelay()
        {
            CreateStubReturningDelayedResponse();

            RestRequest request = new RestRequest("/delay", Method.Get);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            RestResponse response = await client.ExecuteAsync(request);

            stopwatch.Stop();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(2000));
        }

        [Test]
        public async Task TestStubFault()
        {
            CreateStubReturningFault();

            RestRequest request = new RestRequest("/fault", Method.Get);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.Throws<JsonReaderException>(() => JObject.Parse(response.Content));
        }

        [TearDown]
        public void StopServer()
        {
            server.Stop();
        }

    }
}