using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace common.server.servers.pipeLine
{
    public class PipelineServer
    {
        public NamedPipeServerStream Server { get; private set; }
        public StreamWriter Writer { get; private set; }
        public StreamReader Reader { get; private set; }
        public Func<string, string> Action { get; private set; }

        public PipelineServer(string pipeName, Func<string, string> action)
        {
            Server = new NamedPipeServerStream(pipeName);
            Writer = new StreamWriter(Server);
            Reader = new StreamReader(Server);
            Action = action;
        }
        public void BeginAccept()
        {
            IAsyncResult result = Server.BeginWaitForConnection(ProcessAccept, null);
            if (result.CompletedSynchronously)
            {
                ProcessAccept(result);
            }
        }
        private void ProcessAccept(IAsyncResult result)
        {
            Server.EndWaitForConnection(result);
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        string msg = await Reader.ReadLineAsync().ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(msg))
                        {
                            Server.Disconnect();
                            break;
                        }
                        string res = Action(msg);
                        await Writer.WriteLineAsync(res).ConfigureAwait(false);
                        Writer.Flush();
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                BeginAccept();
            });
        }

        public void Dispose()
        {
            Server.Dispose();
            Server = null;
            Writer = null;
            Reader = null;
            Action = null;
        }
    }

}
