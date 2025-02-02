﻿using common.libs;
using Microsoft.Extensions.DependencyInjection;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using common.server;
using common.server.servers.iocp;
using common.server.servers.rudp;
using System.Reflection;
using server.service.messengers.register;
using server.messengers.register;
using common.libs.database;
using server.messengers;
using server.service.validators;

namespace server.service
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Plugin : IPlugin
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));
            services.AddSingleton<Config>();
            services.AddSingleton<ITcpServer, TcpServer>();
            services.AddSingleton<IUdpServer, UdpServer>();

            services.AddSingleton<IClientRegisterCaching, ClientRegisterCaching>();
            services.AddSingleton<IRelaySourceConnectionSelector, messengers.RelaySourceConnectionSelector>();

            services.AddSingleton<IRegisterKeyValidator, RegisterValidator>();
            services.AddSingleton<IServiceAccessValidator, JsonFileServiceAccessValidator>();
            services.AddSingleton<IRelayValidator, RelayValidator>();


            services.AddSingleton<MessengerResolver>();
            services.AddSingleton<MessengerSender>();
            services.AddSingleton<ICryptoFactory, CryptoFactory>();
            services.AddSingleton<IAsymmetricCrypto, RsaCrypto>();
            services.AddSingleton<WheelTimer<object>>();

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)))
            {
                services.AddSingleton(item);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var config = services.GetService<Config>();
            services.GetService<IRegisterKeyValidator>();

            var server = services.GetService<ITcpServer>();
            server.SetBufferSize(config.TcpBufferSize);
            server.Start(config.Tcp);
            Logger.Instance.Info("TCP服务已开启");

            services.GetService<IUdpServer>().Start(services.GetService<Config>().Udp, timeout: config.TimeoutDelay);
            Logger.Instance.Info("UDP服务已开启");


            MessengerResolver messenger = services.GetService<MessengerResolver>();
            MessengerSender sender = services.GetService<MessengerSender>();
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)).Distinct())
            {
                messenger.LoadMessenger(item, services.GetService(item));
            }
            Loop(services);
        }

        private void Loop(ServiceProvider services)
        {
            IClientRegisterCaching clientRegisterCache = services.GetService<IClientRegisterCaching>();
            MessengerResolver messengerResolver = services.GetService<MessengerResolver>();
            MessengerSender messengerSender = services.GetService<MessengerSender>();

            clientRegisterCache.OnChanged.Sub((changeClient) =>
            {
                List<ClientsClientInfo> clients = clientRegisterCache.Get(changeClient.GroupId).Where(c => c.OnLineConnection != null && c.OnLineConnection.Connected).OrderBy(c=>c.Id).Select(c => new ClientsClientInfo
                {
                    Connection = c.OnLineConnection,
                    Id = c.Id,
                    Name = c.Name,
                    Access = c.ClientAccess,
                }).ToList();

                if (clients.Any())
                {
                    byte[] bytes = new ClientsInfo
                    {
                        Clients = clients.ToArray()
                    }.ToBytes();
                    foreach (ClientsClientInfo client in clients)
                    {
                        messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = client.Connection,
                            Payload = bytes,
                            MessengerId = (ushort)ClientsMessengerIds.Notify
                        }).Wait();
                    }
                }
            });
        }
    }
}
