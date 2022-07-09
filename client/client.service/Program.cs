using client.messengers.register;
using client.service.ftp;
using client.service.logger;
using client.service.messengers;
using client.service.socks5;
using client.service.tcpforward;
using client.service.ui.api.manager;
using client.service.ui.api.service;
using client.service.ui.api.service.clientServer;
using client.service.ui.api.webrtc;
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
                typeof(ClientServer).Assembly,
                typeof(CounterClientService).Assembly,
                typeof(WebRTCMessenger).Assembly,
                typeof(Socks5Messenger).Assembly,

            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();

            serviceCollection
                .AddMiddleware(assemblys)
                .AddServerPlugin(assemblys)//基础的功能
                .AddUI(assemblys)//客户端管理
                .AddTcpForwardPlugin()  //客户端tcp转发
                .AddSocks5() //socks5代理
                .AddFtpPlugin() //文件服务
                .AddLoggerPlugin() //日志
            ;

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider
                .UseMiddleware(assemblys)
                .UseServerPlugin(assemblys)//基础的功能
                .UseUI(assemblys)//客户端管理
                .UseTcpForwardPlugin()//客户端tcp转发
                .UseSocks5()//socks5代理
                .UseFtpPlugin() //文件服务
                .UseLoggerPlugin() //日志
               ;
            //自动注册
            serviceProvider.GetService<IRegisterTransfer>().AutoReg();

            Logger.Instance.Warning(string.Empty.PadRight(50,'='));
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            ThreadPool.SetMaxThreads(65535, 65535);
        }
    }
}
