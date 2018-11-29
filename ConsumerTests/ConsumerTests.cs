using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PactNet.Matchers;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestConsumer;
using Xunit;

namespace ConsumerTests
{
    public class ConumerPactTest : IClassFixture<WebApplicationFactory<Startup>>, IClassFixture<ConsumerEventApiPact>
    {
        private readonly WebApplicationFactory<Startup> factory;
        private readonly ConsumerEventApiPact data;
        private readonly IMockProviderService mockProviderService;
        private string mockProviderServiceBaseUri;

        public ConumerPactTest(WebApplicationFactory<Startup> factory, ConsumerEventApiPact data)
        {
            this.factory = factory;
            this.data = data;
            mockProviderService = data.MockProviderService;
            mockProviderService.ClearInteractions();
            mockProviderServiceBaseUri = data.MockProviderServiceBaseUri;
        }

        [Fact]
        public async Task ShouldReturnTestValues()
        {
            var expectedItem = new
            {
                Id = 5,
                Value = "Some Value"
            };
            //Arrange
            mockProviderService
                    .Given($"There is an item with id {expectedItem.Id}")
                    .UponReceiving("A GET request to retrieve provider values")
                    .With(new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = $"/api/providerValues/{expectedItem.Id}"
                    })
                    .WillRespondWith(new ProviderServiceResponse
                    {
                        Status = 200,
                        Headers = new Dictionary<string, object>
                        {
                            { "Content-Type", "application/json; charset=utf-8" }
                        },
                        Body = new
                        {
                            Id = Match.Type(expectedItem.Id),
                            Value = Match.Type(expectedItem.Value)
                        }
                    });

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "ProviderUrl", mockProviderServiceBaseUri }
                    });
                });
            }).CreateClient();

            var result = await client.GetAsync($"/api/values/{expectedItem.Id}");

            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            Assert.Equal(JsonConvert.SerializeObject(expectedItem), content);
        }
    }
}
