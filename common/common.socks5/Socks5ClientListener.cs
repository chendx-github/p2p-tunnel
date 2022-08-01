using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace common.socks5
{
    public class Socks5ClientListener
    {
        private Socket socket;
        private UdpClient udpClient;
        private IPEndPoint endpoint;
        private ConcurrentDictionary<ulong, AsyncUserToken> connections = new();

        private readonly NumberSpace numberSpace = new NumberSpace(0);

        private readonly ISocks5ClientHandler socks5Handler;
        private readonly Config config;
        public Socks5ClientListener(ISocks5ClientHandler socks5Handler, Config config)
        {
            this.socks5Handler = socks5Handler;
            this.config = config;
        }

        public void Start(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            endpoint = new IPEndPoint(IPAddress.Loopback, port);

            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = new AsyncUserToken
                {
                    Socket = socket,
                },
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;

            StartAccept(acceptEventArg);

            udpClient = new UdpClient(localEndPoint);
            IAsyncResult result = udpClient.BeginReceive(ProcessReceiveUdp, null);
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    Logger.Instance.DebugError(e.LastOperation.ToString());
                    break;
            }
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            AsyncUserToken token = ((AsyncUserToken)acceptEventArg.UserToken);
            try
            {
                if (!token.Socket.AcceptAsync(acceptEventArg))
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {

            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            BindReceive(e.AcceptSocket);
            StartAccept(e);
        }

        public void BindReceive(Socket socket)
        {
            ulong id = numberSpace.Increment();
            AsyncUserToken token = new AsyncUserToken
            {
                Socket = socket,
                Id = id,
                DataWrap = new Socks5Info { Id = id }
            };
            connections.TryAdd(token.Id, token);
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            token.PoolBuffer = ArrayPool<byte>.Shared.Rent(config.BufferSize);
            readEventArgs.SetBuffer(token.PoolBuffer, 0, config.BufferSize);
            readEventArgs.Completed += IO_Completed;
            if (!socket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    Memory<byte> buffer = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    token.DataWrap.Data = buffer;
                    ExecuteHandle(token);

                    if (token.Socket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(token.Socket.Available);
                        while (token.Socket.Available > 0)
                        {
                            int length = token.Socket.Receive(arr);
                            if (length > 0)
                            {
                                token.DataWrap.Data = arr.AsMemory(0, length);
                                ExecuteHandle(token);
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (!token.Socket.Connected)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (!token.Socket.ReceiveAsync(e))
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                token.Clear();
                Logger.Instance.DebugError(ex);
            }
        }
        private void ProcessReceiveUdp(IAsyncResult result)
        {
            IPEndPoint rep = null;
            try
            {
                byte[] data = udpClient.EndReceive(result, ref rep);

                Socks5Info info = new Socks5Info { Id = 0, Data = data, SourceEP = rep };
                socks5Handler.HndleForwardUdp(info);

                result = udpClient.BeginReceive(ProcessReceiveUdp, null);
            }
            catch (Exception)
            {
            }
        }

        private void ExecuteHandle(AsyncUserToken token)
        {
            bool closeFlag = true;
            if (token.Socks5Step >= Socks5EnumStep.Forward)
            {
                closeFlag = socks5Handler.HndleForward(token.DataWrap);
            }
            else if (token.Socks5Step == Socks5EnumStep.Request)
            {
                token.Version = token.DataWrap.Data.Span[0];
                closeFlag = socks5Handler.HandleRequest(token.DataWrap);
            }
            else if (token.Socks5Step == Socks5EnumStep.Auth)
            {
                token.Version = token.DataWrap.Data.Span[0];
                closeFlag = socks5Handler.HandleAuth(token.DataWrap);
            }
            else if (token.Socks5Step == Socks5EnumStep.Command)
            {
                closeFlag = socks5Handler.HandleCommand(token.DataWrap);
            }
            if (!closeFlag)
            {
                CloseClientSocket(token.Id);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (!token.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        public void RequestResponse(ulong id, Socks5EnumAuthType socks5EnumAuthType)
        {
            if (socks5EnumAuthType == Socks5EnumAuthType.NotSupported)
            {
                CloseClientSocket(id);
            }
            else
            {
                if (connections.TryGetValue(id, out AsyncUserToken token))
                {
                    if (socks5EnumAuthType == Socks5EnumAuthType.NoAuth)
                    {
                        token.Socks5Step = Socks5EnumStep.Command;
                    }
                    else
                    {
                        token.Socks5Step = Socks5EnumStep.Auth;
                    }

                    token.Socket.Send(new byte[] { token.Version, (byte)socks5EnumAuthType });
                }
            }
        }
        public void AuthResponse(ulong id, Socks5EnumAuthState socks5EnumAuthState)
        {
            if (socks5EnumAuthState != Socks5EnumAuthState.Success)
            {
                CloseClientSocket(id);
            }
            else
            {
                if (connections.TryGetValue(id, out AsyncUserToken token))
                {
                    if (socks5EnumAuthState == Socks5EnumAuthState.Success)
                    {
                        token.Socks5Step = Socks5EnumStep.Command;
                    }
                    token.Socket.Send(new byte[] { token.Version, (byte)socks5EnumAuthState });
                }
            }
        }
        public void CommandResponse(ulong id, Socks5EnumResponseCommand socks5EnumResponseCommand)
        {
            if (socks5EnumResponseCommand != Socks5EnumResponseCommand.ConnecSuccess)
            {
                CloseClientSocket(id);
            }
            else
            {
                if (connections.TryGetValue(id, out AsyncUserToken token))
                {
                    if (socks5EnumResponseCommand == Socks5EnumResponseCommand.ConnecSuccess)
                    {
                        token.Socks5Step = Socks5EnumStep.Forward;
                    }
                    var resp = Socks5Parser.MakeConnectResponse(endpoint, (byte)socks5EnumResponseCommand);
                    token.Socket.Send(resp);
                    if (socks5EnumResponseCommand == Socks5EnumResponseCommand.ConnecSuccess)
                    {
                        token.Socks5Step = Socks5EnumStep.UnKnow;
                    }
                    if (token.Buffer.Size > 0)
                    {
                        token.Socket.Send(token.Buffer.Data.Slice(0, token.Buffer.Size).Span);
                        token.Buffer.Clear(true);
                    }
                    if (token.CloseFlag)
                    {
                        CloseClientSocket(token.Id);
                    }
                }
            }
        }
        public void Response(ulong id, Memory<byte> memory)
        {
            if (connections.TryGetValue(id, out AsyncUserToken token))
            {
                if (token.Socks5Step == Socks5EnumStep.UnKnow)
                {
                    if (memory.Length == 0)
                    {
                        CloseClientSocket(id);
                    }
                    else
                    {
                        token.Socket.Send(memory.Span);
                    }
                }
                else
                {
                    if (memory.Length == 0)
                    {
                        token.CloseFlag = true;
                    }
                    else
                    {
                        token.Buffer.AddRange(memory, memory.Length);
                    }
                }
            }
        }
        public void ResponseUdp(Socks5Info info)
        {
            udpClient.Send(Socks5Parser.MakeUdpResponse(endpoint, info.Data).Span, info.SourceEP);
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            e.Dispose();
            token.Clear();
            connections.TryRemove(token.Id, out _);
            socks5Handler.Close(token.Id);
        }
        private void CloseClientSocket(ulong id)
        {
            if (connections.TryRemove(id, out AsyncUserToken token))
            {
                token.Clear();
            }
        }

        public void Stop()
        {
            socket?.SafeClose();
            udpClient?.Dispose();
            foreach (var item in connections.Values)
            {
                item.Clear();
            }
            connections.Clear();
        }
    }

    public class AsyncUserToken
    {
        public Socks5EnumStep Socks5Step { get; set; } = Socks5EnumStep.Request;
        public byte Version { get; set; } = 0;
        public ulong Id { get; set; } = 0;
        public Socket Socket { get; set; }
        public ReceiveDataBuffer Buffer { get; set; } = new ReceiveDataBuffer();
        public bool CloseFlag { get; set; } = false;

        public byte[] PoolBuffer { get; set; }

        public Socks5Info DataWrap { get; set; }

        public void Clear()
        {
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }

            Buffer.Clear(true);
            Socket?.SafeClose();
            DataWrap = null;
        }
    }

}
