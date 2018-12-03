using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FlightsApi.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlightsApi
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DatabaseContext>(options =>
               options.UseInMemoryDatabase("TestingDB"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var databaseContext = serviceScope.ServiceProvider.GetRequiredService<Model.DatabaseContext>();
                if (databaseContext.Flights.SingleOrDefault(i => i.Id == 1) == null)
                {
                    Passenger passenger = new Passenger { Id = 1 };
                    databaseContext.Flights.Add(new Flight
                    {
                        Id = 1,
                        Number = "abc",
                        Operator = "BA",
                        Passengers = new List<Passenger> { passenger }
                    });
                    databaseContext.Passengers.Add(passenger);
                    databaseContext.SaveChanges();
                }
            }
            app.Map("/api/flights", builder =>
            {
                builder.Run(async context =>
                {
                    using (var serviceScope = app.ApplicationServices.CreateScope())
                    {
                        var id = Convert.ToInt32(context.Request.Path.Value.Substring(context.Request.Path.Value.LastIndexOf('/') + 1));
                        var flightsContext = serviceScope.ServiceProvider.GetRequiredService<Model.DatabaseContext>();
                        var flight = flightsContext.Flights.Include(f => f.Passengers).SingleOrDefault(f => f.Id == id);
                        if (flight != null)
                        {
                            var client = new HttpClient
                            {
                                BaseAddress = new Uri(configuration["ProviderUrl"])
                            };
                            var passengers = new List<PassengerDto>();
                            foreach(var p in flight.Passengers)
                            {
                                var result = await client.GetAsync($"/api/passengers/{p.Id}");
                                result.EnsureSuccessStatusCode();
                                var content = await result.Content.ReadAsStringAsync();
                                var passengerJson = JObject.Parse(content);
                                passengers.Add(new PassengerDto
                                {
                                    Id = (int)passengerJson["Id"],
                                    Name = (string)passengerJson["Name"],
                                    Surname = (string)passengerJson["Surname"]
                                });
                            }
                            
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                            {
                                flight.Id,
                                flight.Number,
                                flight.Operator,
                                passengers
                            }));
                        }
                    }
                });
            });
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
