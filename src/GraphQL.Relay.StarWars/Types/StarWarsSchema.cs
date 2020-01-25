using System;
using GraphQL.Relay.StarWars.Api;
using GraphQL.Types;

namespace GraphQL.Relay.StarWars.Types
{
    public class StarWarsSchema : Schema
    {
        public StarWarsSchema(IServiceProvider provider)
            : base(provider)
        {
            Query = new StarWarsQuery((Swapi)provider.GetService(typeof(Swapi)));

            /*
            RegisterType<FilmGraphType>();
            RegisterType<PeopleGraphType>();
            RegisterType<PlanetGraphType>();
            RegisterType<SpeciesGraphType>();
            RegisterType<StarshipGraphType>();
            RegisterType<VehicleGraphType>();
        */
        }
    }
}