﻿using client.service.vea.socks5;
using common.libs;
using common.server;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace client.service.vea
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
        public void LoadAfter(ServiceProvider services, Assembly[] assemblys)
        {
            var transfer = services.GetService<VeaTransfer>();
            services.GetService<IVeaSocks5ClientHandler>();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug("vea 虚拟网卡插件已加载");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            if (services.GetService<Config>().Enable)
            {
                transfer.Run();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblys"></param>
        public void LoadBefore(ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<Config>();
            services.AddSingleton<VeaTransfer>();
            services.AddSingleton<VeaMessengerSender>();
            services.AddSingleton<IVeaSocks5ClientHandler, VeaSocks5ClientHandler>();
            services.AddSingleton<IVeaSocks5DstEndpointProvider, VeaSocks5DstEndpointProvider>();

            services.AddSingleton<IVeaSocks5ServerHandler, VeaSocks5ServerHandler>();
            services.AddSingleton<IVeaSocks5ClientListener, VeaSocks5ClientListener>();
            services.AddSingleton<IVeaSocks5MessengerSender, VeaSocks5MessengerSender>();

            services.AddSingleton<IVeaKeyValidator, DefaultVeaKeyValidator>();
            services.AddSingleton<ISocks5AuthValidator, Socks5AuthValidator>();

        }
    }
}
