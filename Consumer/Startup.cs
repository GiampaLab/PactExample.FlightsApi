using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace TestConsumer
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.Map("/api/values", builder =>
            {
                builder.Run(async context =>
                {
                    var client = new HttpClient
                    {
                        BaseAddress = new Uri(configuration["ProviderUrl"])
                    };
                    var id = Convert.ToInt32(context.Request.Path.Value.Substring(context.Request.Path.Value.LastIndexOf('/') + 1));
                    var result = await client.GetAsync($"/api/providerValues/{id}");
                    result.EnsureSuccessStatusCode();
                    var content = await result.Content.ReadAsStringAsync();
                    await context.Response.WriteAsync(content);
                });
            });
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
