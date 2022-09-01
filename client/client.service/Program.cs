using client.messengers.register;
using client.realize.messengers;
using client.realize.messengers.punchHole;
using client.service.ftp;
using client.service.logger;
using client.service.socks5;
using client.service.tcpforward;
using client.service.udpforward;
using client.service.ui.api.manager;
using client.service.ui.api.service;
using client.service.ui.api.service.clientServer;
using client.service.vea;
using common.libs;
using common.server.middleware;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace client.service
{
    class Program
    {
        static void Main(string[] args)
        {
            Startup.Start();
            Console.ReadLine();
        }
    }

    class Startup
    {
        public static void Start()
        {
            ThreadPool.SetMinThreads(150, 150);

            LoggerConsole();

            Logger.Instance.Info("正在启动...");

            ServiceCollection serviceCollection = new ServiceCollection();
            ServiceProvider serviceProvider = null;
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);

            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            Assembly[] assemblys = new Assembly[] {
                typeof(FtpMessengerBase).Assembly,
                typeof(LoggerClientService).Assembly,
                typeof(TcpForwardMessenger).Assembly,
                typeof(UdpForwardMessenger).Assembly,
                typeof(ClientServer).Assembly,
                typeof(CounterClientService).Assembly,
                typeof(Socks5ClientService).Assembly,
                typeof(Socks5Messenger).Assembly,
                typeof(PunchHoleMessenger).Assembly,
                typeof(VeaMessenger).Assembly,
            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();

            serviceCollection
                .AddMiddleware(assemblys)
                .AddServerPlugin(assemblys)//基础的功能
                .AddUI(assemblys)//客户端管理
                .AddTcpForwardPlugin()  //客户端tcp转发
                .AddUdpForwardPlugin()  //客户端udp转发
                .AddSocks5() //socks5代理
                .AddFtpPlugin() //文件服务
                .AddLoggerPlugin() //日志
                .AddVirtualEthernetAdapterPlugin()
            ;

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider
                .UseMiddleware(assemblys)
                .UseServerPlugin(assemblys)//基础的功能
                .UseUI(assemblys)//客户端管理
                .UseTcpForwardPlugin()//客户端tcp转发
                .UseUdpForwardPlugin()//客户端tcp转发
                .UseSocks5()//socks5代理
                .UseFtpPlugin() //文件服务
                .UseLoggerPlugin() //日志
                .UseVirtualEthernetAdapterPlugin()
               ;

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            //自动注册
            if (serviceProvider.GetService<Config>().Client.AutoReg)
            {
                serviceProvider.GetService<IRegisterTransfer>().Register();
            }
        }

        static void LoggerConsole()
        {
            Logger.Instance.OnLogger.Sub((model) =>
             {
                 ConsoleColor currentForeColor = Console.ForegroundColor;
                 switch (model.Type)
                 {
                     case LoggerTypes.DEBUG:
                         Console.ForegroundColor = ConsoleColor.Blue;
                         break;
                     case LoggerTypes.INFO:
                         Console.ForegroundColor = ConsoleColor.White;
                         break;
                     case LoggerTypes.WARNING:
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         break;
                     case LoggerTypes.ERROR:
                         Console.ForegroundColor = ConsoleColor.Red;
                         break;
                     default:
                         break;
                 }
                 Console.WriteLine($"[{model.Type.ToString().PadRight(7)}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}");
                 Console.ForegroundColor = currentForeColor;
             });
        }
    }
}
