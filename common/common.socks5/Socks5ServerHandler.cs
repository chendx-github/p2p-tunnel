using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace common.socks5
{
    public class Socks5ServerHandler : ISocks5ServerHandler
    {
        private ConcurrentDictionary<ConnectionKey, AsyncServerUserToken> connections = new(new ConnectionKeyComparer());

        private readonly Socks5MessengerSender socks5MessengerSender;
        private readonly Config config;
        Semaphore maxNumberAcceptedClients;
        public Socks5ServerHandler(Socks5MessengerSender socks5MessengerSender, Config config)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.config = config;
            maxNumberAcceptedClients = new Semaphore(config.NumConnections, config.NumConnections);
        }

        public void HandleRequest(IConnection connection, Socks5Info data)
        {
            if (!config.ConnectEnable)
            {
                data.Response[0] = (byte)Socks5EnumAuthType.NotSupported;
            }
            else
            {
                data.Response[0] = (byte)Socks5EnumAuthType.NoAuth;
            }
            data.Data = data.Response;
            socks5MessengerSender.RequestResponse(data, connection);
        }

        public void HandleAuth(IConnection connection, Socks5Info data)
        {
            if (!config.ConnectEnable)
            {
                data.Response[0] = (byte)Socks5EnumAuthState.UnKnow;
            }
            else
            {
                data.Response[0] = (byte)Socks5EnumAuthState.Success;
            }
            data.Data = data.Response;
            socks5MessengerSender.RequestResponse(data, connection);
        }

        public void HndleForward(IConnection connection, Socks5Info data)
        {
            if (config.ConnectEnable)
            {
                ConnectionKey key = new ConnectionKey(connection.ConnectId, data.Id);
                if (connections.TryGetValue(key, out AsyncServerUserToken token))
                {
                    if (data.Data.Length > 0)
                    {
                        _ = token.TargetSocket.SendAsync(data.Data, SocketFlags.None);
                    }
                    else
                    {
                        CloseClientSocket(token);
                    }
                }
            }
        }

        public void HandleCommand(IConnection connection, Socks5Info data)
        {
            data.Response[0] = 0xff;
            if (!config.ConnectEnable)
            {
                data.Response[0] = (byte)Socks5EnumResponseCommand.Unknow;
            }
            else
            {
                Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)data.Data.Span[1];
                if (command == Socks5EnumRequestCommand.Connect)
                {
                    IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(data.Data.Span);
                    if (!config.LanConnectEnable && remoteEndPoint.IsLan())
                    {
                        data.Response[0] = (byte)Socks5EnumResponseCommand.NetworkError;
                    }
                    else
                    {
                        Connect(connection, data, remoteEndPoint);
                    }
                }
                else if (command == Socks5EnumRequestCommand.Bind)
                {
                    data.Response[0] = (byte)Socks5EnumResponseCommand.CommandNotAllow;
                }
                else if (command == Socks5EnumRequestCommand.UdpAssociate)
                {
                    data.Response[0] = (byte)Socks5EnumResponseCommand.CommandNotAllow;
                }
                else
                {
                    data.Response[0] = (byte)Socks5EnumResponseCommand.CommandNotAllow;
                }
            }
            if (data.Response[0] != 0xff)
            {
                data.Data = data.Response;
                socks5MessengerSender.CommandResponse(data, connection);
            }
        }
        private void Connect(IConnection connection, Socks5Info data, IPEndPoint remoteEndPoint)
        {
            maxNumberAcceptedClients.WaitOne();
            Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            AsyncServerUserToken token = new AsyncServerUserToken
            {
                Connection = connection,
                TargetSocket = socket,
                Data = data,
                Key = new ConnectionKey(connection.ConnectId, data.Id)
            };
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            connectEventArgs.RemoteEndPoint = remoteEndPoint;
            connectEventArgs.Completed += Target_IO_Completed;
            if (!socket.ConnectAsync(connectEventArgs))
            {
                TargetProcessConnect(connectEventArgs);
            }
        }

        private void Target_IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    TargetProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    TargetProcessReceive(e);
                    break;
                default:
                    break;
            }
        }
        private void TargetProcessConnect(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
            Socks5EnumResponseCommand command = Socks5EnumResponseCommand.ServerError;
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    command = Socks5EnumResponseCommand.ConnecSuccess;
                    BindTargetReceive(token);
                }
                else
                {
                    if (e.SocketError == SocketError.ConnectionRefused)
                    {
                        command = Socks5EnumResponseCommand.DistReject;
                    }
                    else if (e.SocketError == SocketError.NetworkDown)
                    {
                        command = Socks5EnumResponseCommand.NetworkError;
                    }
                    else if (e.SocketError == SocketError.ConnectionReset)
                    {
                        command = Socks5EnumResponseCommand.DistReject;
                    }
                    else if (e.SocketError == SocketError.AddressFamilyNotSupported || e.SocketError == SocketError.OperationNotSupported)
                    {
                        command = Socks5EnumResponseCommand.AddressNotAllow;
                    }
                    else
                    {
                        command = Socks5EnumResponseCommand.ServerError;
                    }
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                command = Socks5EnumResponseCommand.ServerError;
                CloseClientSocket(token);
            }
            finally
            {
                token.Data.Response[0] = (byte)command;
                token.Data.Data = token.Data.Response;
                socks5MessengerSender.CommandResponse(token.Data, token.Connection);
            }
        }

        private void BindTargetReceive(AsyncServerUserToken connectToken)
        {
            AsyncServerUserToken token = new AsyncServerUserToken
            {
                Connection = connectToken.Connection,
                TargetSocket = connectToken.TargetSocket,
                Key = connectToken.Key,
                Data = connectToken.Data
            };
            try
            {
                connections.TryAdd(token.Key, token);
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None,
                };
                token.PoolBuffer = ArrayPool<byte>.Shared.Rent(config.BufferSize);
                readEventArgs.SetBuffer(token.PoolBuffer, 0, config.BufferSize);
                readEventArgs.Completed += Target_IO_Completed;
                if (!token.TargetSocket.ReceiveAsync(readEventArgs))
                {
                    TargetProcessReceive(readEventArgs);
                }
            }
            catch (Exception)
            {
                socks5MessengerSender.ResponseClose(token.Key.RequestId, token.Connection);
            }
        }
        private void TargetProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    token.Data.Data = e.Buffer.AsMemory(offset, length);
                    socks5MessengerSender.Response(token.Data, token.Connection);


                    if (token.TargetSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(config.BufferSize);
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(arr);
                            if (length > 0)
                            {
                                token.Data.Data = arr.AsMemory(0, length);
                                socks5MessengerSender.Response(token.Data, token.Connection);
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (!token.TargetSocket.Connected)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (!token.TargetSocket.ReceiveAsync(e))
                    {
                        TargetProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                CloseClientSocket(e);
                Logger.Instance.DebugError(ex);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = e.UserToken as AsyncServerUserToken;
            if (!token.IsClosed)
            {
                IConnection connection = token.Connection;
                token.Clear();
                connections.TryRemove(token.Key, out _);
                e.Dispose();

                maxNumberAcceptedClients.Release();
                socks5MessengerSender.ResponseClose(token.Key.RequestId, connection);
            }
        }
        private void CloseClientSocket(AsyncServerUserToken token)
        {
            token.IsClosed = true;
            token.Clear();
            connections.TryRemove(token.Key, out _);
        }
    }

    public class AsyncServerUserToken
    {
        public ConnectionKey Key { get; set; }
        public IConnection Connection { get; set; }
        public Socket TargetSocket { get; set; }
        public Socks5Info Data { get; set; }
        public bool IsClosed { get; set; } = false;

        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            TargetSocket?.SafeClose();
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }
        }
    }

    public class ConnectionKeyComparer : IEqualityComparer<ConnectionKey>
    {
        public bool Equals(ConnectionKey x, ConnectionKey y)
        {
            return x.RequestId == y.RequestId && x.ConnectId == y.ConnectId;
        }

        public int GetHashCode(ConnectionKey obj)
        {
            return 0;
        }
    }

    public readonly struct ConnectionKey
    {
        public readonly ulong RequestId { get; }
        public readonly ulong ConnectId { get; }

        public ConnectionKey(ulong connectId, ulong requestId)
        {
            ConnectId = connectId;
            RequestId = requestId;
        }
    }
}
