using client.service.command.commands;
using System.CommandLine;
using System.Threading.Tasks;

namespace client.service.command
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Startup.Start(args);
        }
    }

    class Startup
    {
        public static async Task<int> Start(string[] args)
        {
            var rootCommand = new RootCommand("p2p-tunnel client.service.command");

            new CommandRegister(rootCommand);
            new CommandTcpforward(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
