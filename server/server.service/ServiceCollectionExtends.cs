using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using common.server;
using common.server.servers.iocp;
using common.server.servers.rudp;
using System.Reflection;
using server.service.messengers.register;
using server.messengers.register;
using common.libs.database;

namespace server.service
{
    static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpServer(this ServiceCollection services)
        {
            services.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));
            services.AddSingleton<ITcpServer, TcpServer>();
            return services;
        }
        public static ServiceCollection AddUdpServer(this ServiceCollection services)
        {
            services.AddSingleton<IUdpServer, UdpServer>();
            return services;
        }

        public static ServiceProvider UseTcpServer(this ServiceProvider services)
        {
            var config = services.GetService<Config>();
            var server = services.GetService<ITcpServer>();
            server.SetBufferSize(config.TcpBufferSize);
            server.Start(config.Tcp, ip: IPAddress.Any);

            Logger.Instance.Info("TCP服务已开启");

            return services;
        }
        public static ServiceProvider UseUdpServer(this ServiceProvider services)
        {
            services.GetService<IUdpServer>().Start(services.GetService<Config>().Udp);

            Logger.Instance.Info("UDP服务已开启");

            return services;
        }

        public static ServiceCollection AddMessenger(this ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<IClientRegisterCaching, ClientRegisterCaching>();
            services.AddSingleton<ISourceConnectionSelector, SourceConnectionSelector>();
            services.AddSingleton<IRegisterKeyValidator, DefaultRegisterKeyValidator>();
            services.AddSingleton<MessengerResolver>();
            services.AddSingleton<MessengerSender>();
            services.AddSingleton<ICryptoFactory, CryptoFactory>();
            services.AddSingleton<IAsymmetricCrypto, RsaCrypto>();
            services.AddSingleton<WheelTimer<object>>();

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                services.AddSingleton(item);
            }

            return services;
        }
        public static ServiceProvider UseMessenger(this ServiceProvider services, Assembly[] assemblys)
        {
            MessengerResolver messenger = services.GetService<MessengerResolver>();
            MessengerSender sender = services.GetService<MessengerSender>();
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                messenger.LoadMessenger(item, services.GetService(item));
            }

            Loop(services);

            return services;
        }

        private static void Loop(ServiceProvider services)
        {
            IClientRegisterCaching clientRegisterCache = services.GetService<IClientRegisterCaching>();
            MessengerResolver messengerResolver = services.GetService<MessengerResolver>();
            MessengerSender messengerSender = services.GetService<MessengerSender>();

            clientRegisterCache.OnChanged.Sub((Action<RegisterCacheInfo>)((changeClient) =>
            {
                List<ClientsClientInfo> clients = clientRegisterCache.GetBySameGroup(changeClient.GroupId).Where(c => c.TcpConnection != null && c.TcpConnection.Connected).Select(c => new ClientsClientInfo
                {
                    TcpConnection = c.TcpConnection,
                    UdpConnection = c.UdpConnection,
                    Id = c.Id,
                    Name = c.Name,
                    Mac = c.Mac
                }).ToList();
                if (clients.Any())
                {
                    byte[] bytes = new ClientsInfo
                    {
                        Clients = clients.ToArray()
                    }.ToBytes();
                    foreach (ClientsClientInfo client in clients)
                    {
                        _ = messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = client.TcpConnection,
                            Memory = bytes,
                            Path = "clients/Execute"
                        }).ConfigureAwait(false);
                    }
                }
            }));
        }
    }
}
