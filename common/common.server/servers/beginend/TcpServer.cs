using common.libs;
using common.libs.extends;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace common.server.servers.beginend
{
    public class TcpServer : ITcpServer
    {
        private int bufferSize = 8 * 1024;
        private Socket socket;
        private CancellationTokenSource cancellationTokenSource;
        public SimpleSubPushHandler<IConnection> OnPacket { get; } = new SimpleSubPushHandler<IConnection>();

        public SimpleSubPushHandler<IConnection> OnDisconnect { get; } = new SimpleSubPushHandler<IConnection>();

        public void SetBufferSize(int bufferSize = 8 * 1024)
        {
            this.bufferSize = bufferSize;
        }

        public void Start(int port, IPAddress ip = null)
        {
            if (socket == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
                socket = BindAccept(port, ip ?? IPAddress.Any);
            }
        }

        public Socket BindAccept(int port, IPAddress ip)
        {
            IPEndPoint localEndPoint = new IPEndPoint(ip, port);

            var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            IAsyncResult result = socket.BeginAccept(ProcessAccept, socket);
            if (result.CompletedSynchronously)
            {
                ProcessAccept(result);
            }

            return socket;
        }
        private void ProcessAccept(IAsyncResult result)
        {
            Socket listenSocket = (result.AsyncState as Socket);
            try
            {
                Socket client = listenSocket.EndAccept(result);
                BindReceive(client, bufferSize);

                result = listenSocket.BeginAccept(ProcessAccept, listenSocket);
                if (result.CompletedSynchronously)
                {
                    ProcessAccept(result);
                }
            }
            catch (Exception)
            {
                listenSocket.SafeClose();
            }
        }


        public IConnection BindReceive(Socket socket, int bufferSize = 8192)
        {
            AsyncUserToken token = new AsyncUserToken
            {
                Buffer = new byte[bufferSize],
                Connection = CreateConnection(socket),
                Socket = socket,
            };
            IAsyncResult result = socket.BeginReceive(token.Buffer, 0, token.Buffer.Length, SocketFlags.None, ProcessReceive, token);
            if (result.CompletedSynchronously)
            {
                ProcessReceive(result);
            }

            return token.Connection;
        }
        private void ProcessReceive(IAsyncResult result)
        {
            AsyncUserToken token = (result.AsyncState as AsyncUserToken);
            try
            {
                int length = token.Socket.EndReceive(result);
                if (length > 0)
                {
                    token.DataBuffer.AddRange(token.Buffer,0, length);
                    do
                    {
                        int packageLen = token.DataBuffer.Data.Span.ToInt32();
                        if (packageLen > token.DataBuffer.Size - 4)
                        {
                            break;
                        }
                        token.Connection.ReceiveData = token.DataBuffer.Data.Slice(4, packageLen);

                        OnPacket.Push(token.Connection);

                        token.DataBuffer.RemoveRange(0, packageLen + 4);
                    } while (token.DataBuffer.Size > 4);

                    if (!token.Socket.Connected)
                    {
                        return;
                    }

                    result = token.Socket.BeginReceive(token.Buffer, 0, token.Buffer.Length, SocketFlags.None, ProcessReceive, token);
                    if (result.CompletedSynchronously)
                    {
                        if (token.SyncCount >= 500)
                        {
                            token.SyncCount = 1;
                            Task.Run(() =>
                            {
                                ProcessReceive(result);
                            });
                        }
                        else
                        {
                            token.SyncCount++;
                            ProcessReceive(result);
                        }
                    }
                    else
                    {
                        token.SyncCount = 1;
                    }
                }
                else
                {
                    token.Clear();
                }
            }
            catch (Exception)
            {
                token.Clear();
            }
        }

        public IConnection CreateConnection(Socket socket)
        {
            return new TcpConnection(socket);
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            socket?.SafeClose();
        }
    }

    public class AsyncUserToken
    {
        public short SyncCount { get; set; } = 0;
        public IConnection Connection { get; set; }
        public Socket Socket { get; set; }
        public byte[] Buffer { get; set; }
        public ReceiveDataBuffer DataBuffer { get; set; } = new ReceiveDataBuffer();

        public void Clear()
        {
            Socket?.SafeClose();
            Socket = null;

            DataBuffer.Clear(true);
        }
    }
}
