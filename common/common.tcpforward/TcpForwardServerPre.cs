using common.libs;
using common.libs.extends;
using common.server.servers.iocp;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace common.tcpforward
{
    public class TcpForwardServerPre : ITcpForwardServer
    {
        BufferManager bufferManager;
        const int opsToPreAlloc = 1;
        SocketAsyncEventArgsPool readWritePool;
        Semaphore maxNumberAcceptedClients;

        public SimpleSubPushHandler<TcpForwardRequestInfo> OnRequest { get; } = new SimpleSubPushHandler<TcpForwardRequestInfo>();
        public SimpleSubPushHandler<ListeningChangeInfo> OnListeningChange { get; } = new SimpleSubPushHandler<ListeningChangeInfo>();

        private NumberSpace requestIdNs = new NumberSpace(0);

        public TcpForwardServerPre()
        {
        }

        public void Init(int numConnections, int receiveBufferSize)
        {
            bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc, receiveBufferSize);

            readWritePool = new SocketAsyncEventArgsPool(numConnections);
            maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);

            bufferManager.InitBuffer();

            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < numConnections; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += IO_Completed;
                readWriteEventArg.UserToken = new ForwardAsyncUserToken();

                bufferManager.SetBuffer(readWriteEventArg);

                readWritePool.Push(readWriteEventArg);
            }
        }
        public void Start(int port, TcpForwardAliveTypes aliveType)
        {
            if (ServerInfo.Contains(port))
                return;

            BindAccept(port, aliveType);

            OnListeningChange.Push(new ListeningChangeInfo
            {
                Port = port,
                Listening = true
            });
        }

        private void BindAccept(int port, TcpForwardAliveTypes aliveType)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(endpoint);
            socket.Listen(int.MaxValue);

            ServerInfo.Add(new ServerInfo
            {
                SourcePort = port,
                Socket = socket
            });

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            ForwardAsyncUserToken token = new ForwardAsyncUserToken
            {
                SourceSocket = socket,
                SourcePort = port,
            };
            token.Request.Msg.AliveType = aliveType;
            token.Request.SourcePort = port;
            acceptEventArg.UserToken = token;
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            try
            {
                acceptEventArg.AcceptSocket = null;
                ForwardAsyncUserToken token = ((ForwardAsyncUserToken)acceptEventArg.UserToken);
                maxNumberAcceptedClients.WaitOne();
                if (!token.SourceSocket.AcceptAsync(acceptEventArg))
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
            }
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
                    Logger.Instance.Error(e.LastOperation.ToString());
                    break;
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            ForwardAsyncUserToken acceptToken = (e.UserToken as ForwardAsyncUserToken);
            SocketAsyncEventArgs readEventArgs = readWritePool.Pop();
            ForwardAsyncUserToken token = ((ForwardAsyncUserToken)readEventArgs.UserToken);
            try
            {
                token.SourceSocket = e.AcceptSocket;
                token.Request.Msg.RequestId = requestIdNs.Increment();
                token.Request.Msg.AliveType = acceptToken.Request.Msg.AliveType;
                token.Request.SourcePort = acceptToken.SourcePort;
                token.SourcePort = acceptToken.SourcePort;
                token.Request.Connection = null;

                ClientCacheInfo.Add(token);

                //长连接的话，得先发送个空数据过去，让它先连接，不能等得到第一次数据才过去连接并发送数据
                /*
                if (token.Request.Msg.AliveType == TcpForwardAliveTypes.TUNNEL)
                {
                    Receive(readEventArgs, Helper.EmptyArray, 0, 0);
                }
                */

                if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
                {
                    ProcessReceive(readEventArgs);
                }
            }
            catch (Exception)
            {
                CloseClientSocket(readEventArgs);
            }
            StartAccept(e);
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    ForwardAsyncUserToken token = (ForwardAsyncUserToken)e.UserToken;
                    Receive(e, e.Buffer, e.Offset, e.BytesTransferred);
                    if (token.SourceSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(token.SourceSocket.Available);
                        while (token.SourceSocket.Available > 0)
                        {
                            int length = token.SourceSocket.Receive(arr);
                            if (length > 0)
                            {
                                Receive(e, arr, 0, length);
                            }
                        }

                        ArrayPool<byte>.Shared.Return(arr);
                    }
                    //token.SourceSocket.Send(GetData("response text"));
                    if (token.SourceSocket.Connected)
                    {
                        if (!token.SourceSocket.ReceiveAsync(e))
                        {
                            ProcessReceive(e);
                        }
                    }
                    else
                    {
                        CloseClientSocket(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
            }
        }


        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ForwardAsyncUserToken token = (ForwardAsyncUserToken)e.UserToken;
                if (!token.SourceSocket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void Receive(SocketAsyncEventArgs e, byte[] data, int offset, int length)
        {
            ForwardAsyncUserToken token = (ForwardAsyncUserToken)e.UserToken;
            token.Request.Msg.Buffer = data.AsMemory(offset, length);
            OnRequest.Push(token.Request);
            token.Request.Msg.Buffer = Helper.EmptyArray;
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            ForwardAsyncUserToken token = e.UserToken as ForwardAsyncUserToken;

            ClientCacheInfo.Remove(token.Request.Msg.RequestId);
            token.SourceSocket.SafeClose();

            readWritePool.Push(e);
            maxNumberAcceptedClients.Release();

            if (token.Request.Connection != null)
            {
                token.Request.Msg.Buffer = Helper.EmptyArray;
                OnRequest.Push(token.Request);
            }
        }

        public void Response(TcpForwardInfo model)
        {
            if (ClientCacheInfo.Get(model.RequestId, out ForwardAsyncUserToken token))
            {
                if (model.Buffer.Span.Length > 0)
                {
                    try
                    {
                        token.SourceSocket.Send(model.Buffer.Span, SocketFlags.None);
                    }
                    catch (Exception)
                    {
                        ClientCacheInfo.Remove(model.RequestId);
                    }
                }
                else
                {
                    ClientCacheInfo.Remove(model.RequestId);
                }
            }
        }

        public void Stop(ServerInfo model)
        {
            OnListeningChange.Push(new ListeningChangeInfo
            {
                Port = model.SourcePort,
                Listening = false
            });

            ClientCacheInfo.Clear(model.SourcePort);
            model.Remove();
        }
        public void Stop(int sourcePort)
        {
            if (ServerInfo.Get(sourcePort, out ServerInfo model))
            {
                Stop(model);
            }
        }
        public void Stop()
        {
            ServerInfo.Clear();
            ClientCacheInfo.Clear();
        }
    }

    public class ForwardAsyncUserToken
    {
        public Socket SourceSocket { get; set; }
        public int SourcePort { get; set; } = 0;
        public TcpForwardRequestInfo Request { get; set; } = new TcpForwardRequestInfo { Msg = new TcpForwardInfo() };
    }

    public class ClientCacheInfo
    {
        private static ConcurrentDictionary<ulong, ForwardAsyncUserToken> clients = new();

        public static bool Add(ForwardAsyncUserToken model)
        {
            return clients.TryAdd(model.Request.Msg.RequestId, model);
        }
        public static bool Get(ulong id, out ForwardAsyncUserToken c)
        {
            return clients.TryGetValue(id, out c);
        }
        public static void Remove(ulong id)
        {
            if (clients.TryRemove(id, out ForwardAsyncUserToken c))
            {
                try
                {
                    c.SourceSocket.SafeClose();
                }
                catch (Exception)
                {
                }
            }
        }
        public static void Clear(int sourcePort)
        {
            IEnumerable<ulong> requestIds = clients.Where(c => c.Value.SourcePort == sourcePort).Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                Remove(requestId);
            }
        }
        public static void Clear()
        {
            IEnumerable<ulong> requestIds = clients.Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                Remove(requestId);
            }
        }
    }
    public class ServerInfo
    {
        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }

        public static ConcurrentDictionary<int, ServerInfo> services = new();

        public static bool Add(ServerInfo model)
        {
            return services.TryAdd(model.SourcePort, model);
        }
        public static bool Contains(int port)
        {
            return services.ContainsKey(port);
        }
        public static bool Get(int port, out ServerInfo c)
        {
            return services.TryGetValue(port, out c);
        }

        public static void Remove(int port)
        {
            if (services.TryRemove(port, out ServerInfo c))
            {
                try
                {
                    c.Socket.SafeClose();
                }
                catch (Exception)
                {
                }
            }
        }
        public static void Clear()
        {
            foreach (var item in services.Values)
            {
                try
                {
                    item.Socket.SafeClose();
                }
                catch (Exception)
                {
                }
            }
            services.Clear();
        }

        public void Remove()
        {
            Remove(SourcePort);
        }
    }
}