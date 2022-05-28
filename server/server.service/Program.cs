using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using server.service.tcpforward;
using common.server.middleware;
using server.service.manager;
using System.Linq;
using common.socks5;
using server.service.socks5;

namespace server.service
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = File.ReadAllText("appsettings.json").DeJson<Config>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton((e) => config);


            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            Assembly[] assemblys = new Assembly[] {
                typeof(CounterMessenger).Assembly,
                typeof(TcpForwardMessenger).Assembly,
                typeof(WebRTCMessenger).Assembly,
                typeof(Socks5Messenger).Assembly,
            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();

            serviceCollection.AddMiddleware(assemblys).AddMessenger(assemblys).AddTcpServer().AddUdpServer().AddTcpForwardPlugin().AddSocks5();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseMiddleware(assemblys).UseMessenger(assemblys).UseTcpServer().UseUdpServer().UseTcpForwardPlugin().UseSocks5();

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Info("没什么报红的，就说明运行成功了");
            Logger.Instance.Info($"UDP端口:{config.Udp}");
            Logger.Instance.Info($"TCP端口:{config.Tcp}");
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            Console.ReadLine();
        }
    }
}
