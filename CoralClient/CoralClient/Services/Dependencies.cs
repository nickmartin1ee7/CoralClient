using System;
using CoralClient.DbContext;
using Microsoft.Extensions.DependencyInjection;

namespace CoralClient.Services
{
    internal static class Dependencies
    {
        public static IServiceProvider ServiceProvider { get; }

        static Dependencies()
        {
            ServiceProvider = CreateServiceProvider();
        }

        private static IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection()
                .AddSingleton<RconService>()
                .AddDbContext<ServerProfileContext>()
                ;

            return services.BuildServiceProvider();
        }
    }
}
