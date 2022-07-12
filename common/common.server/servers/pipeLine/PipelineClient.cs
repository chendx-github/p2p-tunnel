using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.server.servers.pipeLine
{
    public class PipelineClient
    {
        public NamedPipeClientStream Client { get; private set; }
        public StreamWriter Writer { get; private set; }
        public StreamReader Reader { get; private set; }

        public PipelineClient(string pipeName)
        {
            Client = new NamedPipeClientStream(pipeName);
            Writer = new StreamWriter(Client);
            Reader = new StreamReader(Client);

        }

        public void Connect()
        {
            Client.Connect();
        }

        public void Dispose()
        {
            Client.Dispose();
            Client = null;

            Reader = null;

            Writer = null;
        }
    }
}
