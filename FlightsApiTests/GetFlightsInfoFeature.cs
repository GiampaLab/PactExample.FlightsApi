using FlightsApi;
using FlightsApi.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PactNet.Matchers;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using System.Collections.Generic;
using System.Net.Http;
using Xbehave;
using Xunit;

namespace FlightsApiTestsTests
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

        [Scenario]
        public void GetFlightInfo(PassengerDto expectedPassenger, Flight expectedFlight, HttpClient client, HttpResponseMessage result)
        {
            "Given that I have a flight with one passenger in the database".x(() =>
            {
                expectedPassenger = new PassengerDto
                {
                    Id = 5,
                    Name = "Bob",
                    Surname = "XYZ"
                };

                Passenger passenger = new Passenger { Id = expectedPassenger.Id };

                expectedFlight = new Flight
                {
                    Id = 123,
                    Number = "abc",
                    Operator = "xyz",
                    Passengers = new List<Passenger> { passenger }
                };

                client = factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "ProviderUrl", mockProviderServiceBaseUri }
                    });
                    });
                    builder.ConfigureServices(services =>
                    {
                        // Create a new service provider.
                        var serviceProvider = new ServiceCollection()
                            .AddEntityFrameworkInMemoryDatabase()
                            .BuildServiceProvider();

                        // Add a database context (ApplicationDbContext) using an in-memory 
                        // database for testing.
                        services.AddDbContext<DatabaseContext>(options =>
                        {
                            options.UseInMemoryDatabase("InMemoryDbForTesting");
                            options.UseInternalServiceProvider(serviceProvider);
                        });

                        // Build the service provider.
                        var sp = services.BuildServiceProvider();
                        using (var scope = sp.CreateScope())
                        {
                            var scopedServices = scope.ServiceProvider;
                            var dbContext = scopedServices.GetRequiredService<DatabaseContext>();
                            dbContext.Passengers.Add(passenger);
                            dbContext.Flights.Add(expectedFlight);
                            dbContext.SaveChanges();
                        }
                    });
                }).CreateClient();
            });

            "And the Passengers Api has the details of the passenger".x(() =>
            {
                mockProviderService
                .Given($"There is a passenger with id {expectedPassenger.Id}")
                .UponReceiving("A GET request to retrieve the passenger data")
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = $"/api/passengers/{expectedPassenger.Id}"
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
                        expectedPassenger.Id,
                        Name = Match.Type(expectedPassenger.Name),
                        Surname = Match.Type(expectedPassenger.Surname)
                    }
                });
            });

            "When I query the Flights Api".x(async () =>
            {
                result = await client.GetAsync($"/api/flights/{expectedFlight.Id}");
            });

            "Then the Flights Api should return the flight info with the passenger detail".x(async () =>
            {
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                var expectedPassengers = new List<PassengerDto> { expectedPassenger };
                string expectedResponse = JsonConvert.SerializeObject(new
                {
                    expectedFlight.Id,
                    expectedFlight.Number,
                    expectedFlight.Operator,
                    passengers = expectedPassengers
                });

                expectedResponse.Should().BeEquivalentTo(content);
            });
        }
    }
}