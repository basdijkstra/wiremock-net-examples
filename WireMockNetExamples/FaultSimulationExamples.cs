using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Diagnostics;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WireMockNetExamples
{
    [TestClass]
    public class FaultSimulationExamples
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

        [TestMethod]
        public async Task TestStubDelay()
        {
            CreateStubReturningDelayedResponse();

            RestRequest request = new RestRequest("/delay", Method.Get);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            RestResponse response = await client.ExecuteAsync(request);

            stopwatch.Stop();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 2000);
        }

        [TestMethod]
        public async Task TestStubFault()
        {
            CreateStubReturningFault();

            RestRequest request = new RestRequest("/fault", Method.Get);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.ThrowsException<JsonReaderException>(() => JObject.Parse(response.Content));
        }

        [TestCleanup]
        public void StopServer()
        {
            server.Stop();
        }

    }
}