using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace client.service.vea
{
    public class VirtualEthernetAdapterTransfer
    {
        Process Tun2SocksProcess;
        const string veaName = "p2p-tunnel";

        private readonly Config config;
        public VirtualEthernetAdapterTransfer(Config config)
        {
            this.config = config;
        }

        public void Run()
        {
            if (Tun2SocksProcess != null)
            {
                Tun2SocksProcess.Close();
                Tun2SocksProcess.Dispose();
                Tun2SocksProcess = null;
            }

            if (config.Enable)
            {
                RunTun2Socks();
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
            Tun2SocksProcess = Process.Start($"tun2socks.exe",$" -device {veaName} -proxy socks5://127.0.0.1:{config.SocksPort} -loglevel silent");

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
