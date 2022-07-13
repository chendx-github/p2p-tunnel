using client.service.ui.api.clientServer;
using common.libs;
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

            var config = services.GetService<Config>();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            if (config.EnableWeb)
            {
                clientServer.Websocket();
                Logger.Instance.Debug($"管理UI，websocket已启用");
            }
            else
            {
                Logger.Instance.Info($"管理UI，websocket未启用");
            }
            if (config.EnableCommand)
            {
                clientServer.NamedPipe();
                Logger.Instance.Debug($"管理UI，命令行已启用");
            }
            else
            {
                Logger.Instance.Info($"管理UI，命令行未启用");
            }

            if (config.EnableApi)
            {
                clientServer.LoadPlugins(assemblys);
                Logger.Instance.Debug($"管理UI，api已启用");
            }
            else
            {
                Logger.Instance.Info($"管理UI，api未启用");
            }
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            return services;
        }
    }
}
