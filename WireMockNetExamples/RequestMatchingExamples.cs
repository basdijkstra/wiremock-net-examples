﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WireMockNetExamples
{
    [TestClass]
    public class RequestMatchingExamples
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

        private void CreateStubHeaderMatching()
        {
            server.Given(
                Request.Create().WithPath("/header-matching").UsingGet()
                // this makes the mock only respond to requests that contain
                // a 'Content-Type' header with the exact value 'application/json'
                .WithHeader("Content-Type", new ExactMatcher("application/json"))
                // this makes the mock only respond to requests that do not contain
                // the 'ShouldNotBeThere' header
                .WithHeader("ShouldNotBeThere", ".*", matchBehaviour: MatchBehaviour.RejectOnMatch)
            )
            .RespondWith(
                Response.Create()
                .WithStatusCode(200)
                .WithBody("Header matching successful")
            );
        }

        private void CreateStubRequestBodyMatching()
        {
            server.Given(
                Request.Create().WithPath("/request-body-matching").UsingPost()
                // this makes the mock only respond to requests with a JSON request body
                // that produces a match for the specified JSON path expression
                .WithBody(new JsonPathMatcher("$.cars[?(@.make == 'Alfa Romeo')]"))
            )
            .RespondWith(
                Response.Create()
                .WithStatusCode(201)
            );
        }

        [TestMethod]
        public async Task TestStubHeaderMatching()
        {
            CreateStubHeaderMatching();

            RestRequest request = new RestRequest("/header-matching", Method.Get);

            request.AddHeader("Content-Type", "application/json");

            RestResponse response = await client.ExecuteAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Header matching successful", response.Content);
        }

        [TestMethod]
        public async Task TestStubRequestBodyMatching()
        {
            CreateStubRequestBodyMatching();

            var requestBody = new { cars = new[] {
                new { make = "Alfa Romeo" },
                new { make = "Lancia" }
            }.ToList() };

            RestRequest request = new RestRequest("/request-body-matching", Method.Post);

            request.AddHeader("Content-Type", "application/json");

            request.AddJsonBody(requestBody);

            RestResponse response = await client.ExecuteAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestCleanup]
        public void StopServer()
        {
            server.Stop();
        }

    }
}