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
using client.service.ui.api.service.webServer;
using client.service.ui.api.webrtc;
using common.libs;
using common.libs.database;
using common.server.middleware;
using common.socks5;
using System.Reflection;

namespace client.service.app
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            ThreadPool.SetMinThreads(150, 150);

            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();
            builder.Services.AddMauiBlazorWebView();

            MauiApp app = builder.Build();

            Startup.Start();

            return app;
        }
    }

    class Startup
    {
        static ServiceProvider serviceProvider = null;
        public static void Start()
        {
            Logger.Instance.Info("正在启动...");

            ServiceCollection serviceCollection = new ServiceCollection();
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);


            //加载插件程序集，当单文件发布或者动态加载dll外部插件时需要，否则如果本程序集没有显式的使用它的相关内容的话，会加载不出来
            Assembly[] assemblys = new Assembly[] {
                //typeof(FtpMessengerBase).Assembly,
                typeof(LoggerClientService).Assembly,
                typeof(TcpForwardMessenger).Assembly,
                typeof(UdpForwardMessenger).Assembly,
                typeof(ClientServer).Assembly,
                typeof(CounterClientService).Assembly,
                //typeof(WebRTCMessenger).Assembly,
                typeof(Socks5Messenger).Assembly,
                typeof(PunchHoleMessenger).Assembly,

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
            ;

            serviceCollection.AddTransient(typeof(IConfigDataProvider<>), typeof(ConfigDataFileProvider<>));
            serviceCollection.AddSingleton<IWebServerFileReader, WebServerFileReader>();

            serviceProvider = serviceCollection.BuildServiceProvider();

            serviceProvider.UseMiddleware(assemblys);
            serviceProvider.UseServerPlugin(assemblys);//基础的功能
            serviceProvider.UseUI(assemblys);//客户端管理
            serviceProvider.UseTcpForwardPlugin();//客户端tcp转发
            serviceProvider.UseUdpForwardPlugin();//客户端tcp转发
            serviceProvider.UseSocks5();//socks5代理
            serviceProvider.UseFtpPlugin(); //文件服务
            serviceProvider.UseLoggerPlugin(); //日志

            Logger.Instance.Warning(string.Empty.PadRight(50, '='));
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning(string.Empty.PadRight(50, '='));

            //自动注册
            if (serviceProvider.GetService<Config>().Client.AutoReg)
            {
                serviceProvider.GetService<IRegisterTransfer>().Register();
            }
        }
    }
}