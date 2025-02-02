﻿using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace common.tcpforward
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TcpForwardResolver
    {
        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        private readonly Config config;
        private readonly ITcpForwardValidator tcpForwardValidator;
        private ConcurrentDictionary<ConnectionKey, ConnectUserToken> connections = new ConcurrentDictionary<ConnectionKey, ConnectUserToken>(new ConnectionComparer());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpForwardMessengerSender"></param>
        /// <param name="config"></param>
        /// <param name="tcpForwardValidator"></param>
        public TcpForwardResolver(TcpForwardMessengerSender tcpForwardMessengerSender, Config config, ITcpForwardValidator tcpForwardValidator)
        {
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.config = config;

            this.tcpForwardValidator = tcpForwardValidator;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public async Task InputData(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection.FromConnection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            await OnRequest(data);
        }

        private async Task OnRequest(TcpForwardInfo arg)
        {
            ConnectionKey key = new ConnectionKey(arg.Connection.ConnectId, arg.RequestId);
            if (arg.StateType == TcpForwardStateTypes.Success)
            {
                if (arg.DataType == TcpForwardDataTypes.Forward)
                {
                    if (connections.TryGetValue(key, out ConnectUserToken token))
                    {
                        if (arg.Buffer.Length > 0)
                        {
                            try
                            {
                                await token.TargetSocket.SendAsync(arg.Buffer, SocketFlags.None);
                                return;
                            }
                            catch (Exception)
                            {
                                await CloseClientSocket(token);
                                return;
                            }
                        }
                    }

                    arg.StateType = TcpForwardStateTypes.Close;
                    await ReceiveAsync(arg, Helper.EmptyArray);

                }
                else if (arg.DataType == TcpForwardDataTypes.Connect)
                {
                    await Connect(arg);
                }
            }
            else
            {
                if (connections.TryRemove(key, out ConnectUserToken token))
                {
                    await CloseClientSocket(token);
                }
            }
        }

        private async Task Connect(TcpForwardInfo arg)
        {
            if (tcpForwardValidator.Validate(arg) == false)
            {
                arg.StateType = TcpForwardStateTypes.Fail;
                await ReceiveAsync(arg, Helper.EmptyArray);
                return;
            }

            IPEndPoint endpoint = NetworkHelper.EndpointFromArray(arg.TargetEndpoint);
            if (endpoint == null)
            {
                arg.StateType = TcpForwardStateTypes.Fail;
                await ReceiveAsync(arg, Helper.EmptyArray);
                return;
            }

            // maxNumberAcceptedClients.WaitOne();
            Socket socket = new(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
            socket.SendTimeout = 15000;

            SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = endpoint;
            saea.Completed += IO_Completed;
            saea.UserToken = new ConnectUserToken
            {
                Key = new ConnectionKey(arg.Connection.ConnectId, arg.RequestId),
                TargetSocket = socket,
                SendArg = arg
            };
            if (socket.ConnectAsync(saea) == false)
            {
                ProcessConnect(saea);
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    break;
            }
        }
        private async void ProcessConnect(SocketAsyncEventArgs e)
        {
            ConnectUserToken token = (ConnectUserToken)e.UserToken;
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    connections.TryAdd(token.Key, token);

                    token.SendArg.TargetEndpoint = Helper.EmptyArray;
                    token.SendArg.StateType = TcpForwardStateTypes.Success;
                    await ReceiveAsync(token.SendArg, Helper.EmptyArray);

                    token.TargetSocket.SendBufferSize = config.BufferSize;
                    token.TargetSocket.ReceiveBufferSize = config.BufferSize;
                    token.PoolBuffer = new byte[config.BufferSize];
                    e.SetBuffer(token.PoolBuffer, 0, config.BufferSize);

                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    await CloseClientSocket(token, TcpForwardStateTypes.Fail);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                await CloseClientSocket(token);
            }
        }
        private async void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                ConnectUserToken token = e.UserToken as ConnectUserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;

                    await ReceiveAsync(token.SendArg, e.Buffer.AsMemory(offset, length));

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = await token.TargetSocket.ReceiveAsync(e.Buffer.AsMemory(), SocketFlags.None);
                            if (length > 0)
                            {
                                await ReceiveAsync(token.SendArg, e.Buffer.AsMemory(0, length));
                            }
                        }
                    }
                    if (token.TargetSocket.Connected == false)
                    {
                        await CloseClientSocket(token);
                        return;
                    }
                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    await CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                await CloseClientSocket(e);
            }
        }

        private async Task ReceiveAsync(TcpForwardInfo arg, Memory<byte> data)
        {
            arg.Buffer = data;
            await tcpForwardMessengerSender.SendResponse(arg, arg.Connection);
            arg.Buffer = Helper.EmptyArray;
        }

        private async Task CloseClientSocket(SocketAsyncEventArgs e)
        {
            ConnectUserToken token = e.UserToken as ConnectUserToken;
            await CloseClientSocket(token);
        }
        private async Task CloseClientSocket(ConnectUserToken token, TcpForwardStateTypes state = TcpForwardStateTypes.Close)
        {
            if (token != null)
            {
                //maxNumberAcceptedClients.Release();
                token.SendArg.StateType = state;
                await ReceiveAsync(token.SendArg, Helper.EmptyArray);
                token.Clear();
                connections.TryRemove(token.Key, out _);
            }
        }
    }

    sealed class ConnectUserToken
    {
        public Socket TargetSocket { get; set; }
        public ConnectionKey Key { get; set; }
        public TcpForwardInfo SendArg { get; set; }
        public IConnection Connection { get; set; }
        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            TargetSocket?.SafeClose();
            TargetSocket = null;

            PoolBuffer = Helper.EmptyArray;

            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }

    sealed class ConnectionComparer : IEqualityComparer<ConnectionKey>
    {
        public bool Equals(ConnectionKey x, ConnectionKey y)
        {
            return x.RequestId == y.RequestId && x.ConnectId == y.ConnectId;
        }

        public int GetHashCode(ConnectionKey obj)
        {
            return obj.RequestId.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    readonly struct ConnectionKey
    {
        public readonly uint RequestId { get; }
        public readonly ulong ConnectId { get; }

        public ConnectionKey(ulong connectId, uint requestId)
        {
            ConnectId = connectId;
            RequestId = requestId;
        }

        public override string ToString()
        {
            return $"{ConnectId},{RequestId}";
        }
    }
}