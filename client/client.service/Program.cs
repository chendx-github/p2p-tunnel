﻿using client.messengers.register;
using client.realize.messengers.punchHole;
using client.service.logger;
using client.service.socks5;
using client.service.tcpforward;
using client.service.udpforward;
using client.service.ui.api.service.clientServer;
using client.service.vea.socks5;
using client.service.wakeup;
using common.libs;
using common.server;
using common.server.middleware;
using common.socks5;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace client.service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Startup.Start();
            await Helper.Await();
        }
    }

    class Startup
    {
        public static void Start()
        {
            ThreadPool.SetMinThreads(1024, 1024);
            ThreadPool.SetMaxThreads(65535, 65535);
            LoggerConsole();
            Logger.Instance.Info("正在启动...");

            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            //可以改为从dll文件加载
            Assembly[] assemblys = new Assembly[] {
                typeof(LoggerClientService).Assembly,
                typeof(TcpForwardMessenger).Assembly,
                typeof(UdpForwardMessenger).Assembly,
                typeof(ClientServer).Assembly,
                typeof(Socks5ClientService).Assembly,
                typeof(Socks5Messenger).Assembly,
                typeof(PunchHoleMessenger).Assembly,
                typeof(VeaSocks5Messenger).Assembly,
                typeof(WakeUpMessenger).Assembly,
            }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();
            //Assembly.LoadFile();

            ServiceCollection serviceCollection = new ServiceCollection();
            ServiceProvider serviceProvider = null;
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);

            serviceCollection.AddMiddleware(assemblys);
            IPlugin[] plugins = PluginLoader.LoadBefore(serviceCollection, assemblys);

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseMiddleware(assemblys);
            PluginLoader.LoadAfter(plugins, serviceProvider, assemblys);

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));

            Config config = serviceProvider.GetService<Config>();
            //自动注册
            if (config.Client.AutoReg)
            {
                serviceProvider.GetService<IRegisterTransfer>().Register();
            }
        }

        static void LoggerConsole()
        {
            if (Directory.Exists("log") == false)
            {
                Directory.CreateDirectory("log");
            }
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
                 string line = $"[{model.Type,-7}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}";
                 Console.WriteLine(line);
                 Console.ForegroundColor = currentForeColor;

                 using StreamWriter sw = File.AppendText(Path.Combine("log", $"{DateTime.Now:yyyy-MM-dd}.log"));
                 sw.WriteLine(line);
                 sw.Flush();
                 sw.Close();
                 sw.Dispose();
             });
        }
    }
}
