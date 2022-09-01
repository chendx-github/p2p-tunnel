using client.messengers.clients;
using common.socks5;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace client.service.vea
{
    public class VirtualEthernetAdapterTransfer
    {
        Process Tun2SocksProcess;
        const string veaName = "p2p-tunnel";
        private readonly ConcurrentDictionary<ulong, IPAddress> ips = new ConcurrentDictionary<ulong, IPAddress>();
        private readonly ConcurrentDictionary<IPAddress, ClientInfo> ips2 = new ConcurrentDictionary<IPAddress, ClientInfo>();
        public ConcurrentDictionary<ulong, IPAddress> IPList => ips;
        public ConcurrentDictionary<IPAddress, ClientInfo> IPList2 => ips2;

        private readonly Config config;
        private readonly IVeaSocks5ClientListener socks5ClientListener;

        public VirtualEthernetAdapterTransfer(Config config, IClientInfoCaching clientInfoCaching, VeaMessengerSender veaMessengerSender, IVeaSocks5ClientListener socks5ClientListener)
        {
            this.config = config;
            this.socks5ClientListener = socks5ClientListener;

            clientInfoCaching.OnOnline.Sub((client) =>
            {
                Task.Run(async () =>
                {
                    IPAddress ip = await veaMessengerSender.IP(client.OnlineConnection);
                    ips.AddOrUpdate(client.Id, ip, (a, b) => ip);
                    ips2.AddOrUpdate(ip, client, (a, b) => client);
                });
            });
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                if (ips.TryRemove(client.Id, out IPAddress ip))
                {
                    ips2.TryRemove(ip, out _);
                }
            });
        }

        public void Run()
        {
            if (Tun2SocksProcess != null)
            {
                Tun2SocksProcess.Close();
                Tun2SocksProcess.Dispose();
                Tun2SocksProcess = null;
            }

            socks5ClientListener.Stop();
            if (config.Enable)
            {
                RunTun2Socks();
                socks5ClientListener.Start(config.SocksPort, config.BufferSize);
            }
        }

        private void RunTun2Socks()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Windows();
            }
        }
        private void Windows()
        {
            Tun2SocksProcess = new Process();
            Tun2SocksProcess.StartInfo.CreateNoWindow = true;
            Tun2SocksProcess.StartInfo.FileName = "tun2socks.exe";
            Tun2SocksProcess.StartInfo.UseShellExecute = false;
            Tun2SocksProcess.StartInfo.RedirectStandardError = true;
            Tun2SocksProcess.StartInfo.RedirectStandardInput = true;
            Tun2SocksProcess.StartInfo.RedirectStandardOutput = true;
            Tun2SocksProcess.StartInfo.Arguments = $" -device {veaName} -proxy socks5://127.0.0.1:{config.SocksPort} -loglevel silent";
            //设置启动动作,确保以管理员身份运行 
            Tun2SocksProcess.StartInfo.Verb = "runas";
            Tun2SocksProcess.Start();

            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "bash";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            //proc.StartInfo.Arguments = cmd;
            //设置启动动作,确保以管理员身份运行 
            proc.StartInfo.Verb = "runas";
            proc.Start();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc.StandardInput.WriteLine($"netsh interface ip set address name=\"{veaName}\" source=static addr=${config.IP} mask=255.255.255.0 gateway=none");
                if (config.ProxyAll)
                {
                    proc.StandardInput.WriteLine($"route add 0.0.0.0 mask 0.0.0.0 {config.IP} metric 5");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                proc.StandardInput.WriteLine($"sudo ifconfig {veaName} {config.IP} {config.IP} up");
            }

            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine("exit");
            proc.StandardOutput.ReadToEnd();
            proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            proc.Close();
            proc.Dispose();
        }
    }
}
