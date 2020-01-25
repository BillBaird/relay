using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Relay.Http;
using GraphQL.Relay.StarWars.Api;
using GraphQL.Relay.StarWars.Types;
using GraphQL.Relay.Types;
using GraphQL.Server;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Ui.Voyager;
using GraphQL.Types;
using GraphQL.Validation.Complexity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GraphQL.Relay.StarWars
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc();

            services.AddScoped<IDocumentExecuter, DocumentExecuter>();
            services.AddScoped<IDocumentWriter, DocumentWriter>();
            services.AddScoped<Relay.Http.RequestExecutor>();

            services.AddScoped<Swapi>();
            services.AddSingleton<ResponseCache>();

            services.AddScoped<NodeInterface>();    // Had to do this, not sure why it didn't happen automatically
            
            services.AddScoped<StarWarsQuery>();
            services.AddScoped<FilmGraphType>();
            services.AddScoped<PeopleGraphType>();
            services.AddScoped<PlanetGraphType>();
            services.AddScoped<SpeciesGraphType>();
            services.AddScoped<StarshipGraphType>();
            services.AddScoped<VehicleGraphType>();

            services.AddScoped<StarWarsSchema>();

            services.AddGraphQL(options =>
            {
                options.EnableMetrics = false;
                options.ExposeExceptions = true;
                options.ComplexityConfiguration = new ComplexityConfiguration {MaxDepth = 30}; //optional
            });
                //.AddUserContextBuilder(context => new GraphQLContext(context))
            //.AddWebSockets()
            //.AddDataLoader();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();

            //app.UseMvc().UseStaticFiles();
            app.UseStaticFiles();
            app.UseGraphQL<StarWarsSchema>("/graphql");

            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions()
            {
                Path = "/ui/playground"
            });
            app.UseGraphiQLServer(new GraphiQLOptions
            {
                Path = "/ui/graphiql",
                GraphQLEndPoint = "/graphql"
            });
            app.UseGraphQLVoyager(new GraphQLVoyagerOptions()
            {
                GraphQLEndPoint = "/graphql",
                Path = "/ui/voyager"
            });
        }
    }
}
