using client.service.ui.api.clientServer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace client.service.ui.api.service.clientServer
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddClientServer(this ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<IClientServer, ClientServer>();

            IEnumerable<Type> types = assemblys.SelectMany(c => c.GetTypes());

            foreach (var item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientService))))
            {
                services.AddSingleton(item);
            }
            foreach (var item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientConfigure))))
            {
                services.AddSingleton(item);
            }
            return services;
        }

        public static ServiceProvider UseClientServer(this ServiceProvider services, Assembly[] assemblys)
        {
            IClientServer clientServer = services.GetService<IClientServer>();
            clientServer.LoadPlugins(assemblys);
            clientServer.Start();

            return services;
        }
    }
}
