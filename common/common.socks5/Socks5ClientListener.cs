﻿using common.libs;
using common.libs.extends;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocks5ClientListener
    {
        /// <summary>
        /// 
        /// </summary>
        ushort Port { get; }

        /// <summary>
        /// 
        /// </summary>
        Func<Socks5Info, Task<bool>> OnData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        Action<Socks5Info> OnClose { get; set; }

        void SetBufferSize(int bufferSize);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        void Start(int port, int bufferSize);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        Task Response(Socks5Info info);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        void Close(ulong id);
        /// <summary>
        /// 
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// 
    /// </summary>
    public class Socks5ClientListener : ISocks5ClientListener
    {
        private Socket socket;
        private UdpClient udpClient;
        private int bufferSize = 8 * 1024;

        /// <summary>
        /// 
        /// </summary>
        public ushort Port { get; private set; }


        private readonly ConcurrentDictionary<ulong, AsyncUserToken> connections = new();
        private readonly NumberSpaceUInt32 numberSpace = new NumberSpaceUInt32(0);
        SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// 
        /// </summary>
        public Func<Socks5Info, Task<bool>> OnData { get; set; } = async (data) => await Task.FromResult(true);
        /// <summary>
        /// 
        /// </summary>
        public Action<Socks5Info> OnClose { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5ClientListener()
        {
        }

        public void SetBufferSize(int bufferSize)
        {
            this.bufferSize = bufferSize;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        public void Start(int port, int bufferSize)
        {
            this.bufferSize = bufferSize;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            Port = (ushort)port;

            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
            udpClient.Client.WindowsUdpBug();

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
                if (token.Socket.AcceptAsync(acceptEventArg) == false)
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
        private void BindReceive(Socket socket)
        {
            if (socket == null) return;

            uint id = numberSpace.Increment();
            AsyncUserToken token = new AsyncUserToken
            {
                Socket = socket,
                DataWrap = new Socks5Info { Id = id }
            };
            connections.TryAdd(token.DataWrap.Id, token);
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            socket.SendBufferSize = bufferSize;
            socket.ReceiveBufferSize = bufferSize;
            token.PoolBuffer = new byte[bufferSize];
            readEventArgs.SetBuffer(token.PoolBuffer, 0, bufferSize);
            readEventArgs.Completed += IO_Completed;
            if (socket.ReceiveAsync(readEventArgs) == false)
            {
                ProcessReceive(readEventArgs);
            }
        }
        private async void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int totalLength = e.BytesTransferred;
                    token.DataWrap.Data = e.Buffer.AsMemory(e.Offset, totalLength);

                    //有些客户端，会把一个包拆开发送，很奇怪，不得不验证一下数据完整性
                    if (token.DataWrap.Socks5Step < Socks5EnumStep.Forward)
                    {
                        bool gt = false;
                        while (ValidateData(token.DataWrap, out gt) == false && gt == false)
                        {
                            totalLength += await token.Socket.ReceiveAsync(e.Buffer.AsMemory(e.Offset + totalLength), SocketFlags.None);
                            token.DataWrap.Data = e.Buffer.AsMemory(e.Offset, totalLength);
                        }
                        if (gt)
                        {
                            CloseClientSocket(e);
                            return;
                        }
                    }
                    await ExecuteHandle(token.DataWrap);
                    token.DataWrap.Data = Helper.EmptyArray;

                    if (token.Socket.Available > 0 && token.DataWrap.Socks5Step >= Socks5EnumStep.Forward)
                    {
                        while (token.Socket.Available > 0)
                        {
                            int length = await token.Socket.ReceiveAsync(e.Buffer.AsMemory(), SocketFlags.None);
                            if (length > 0)
                            {
                                token.DataWrap.Data = e.Buffer.AsMemory(0, length);
                                await ExecuteHandle(token.DataWrap);
                                token.DataWrap.Data = Helper.EmptyArray;
                            }
                        }
                    }

                    if (token.Socket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (token.Socket.ReceiveAsync(e) == false)
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
                Logger.Instance.DebugError($"step:{token.DataWrap.Socks5Step}-{string.Join(",", token.DataWrap.Data.ToArray())}");
                CloseClientSocket(e);
                Logger.Instance.DebugError(ex);
            }
        }
        private bool ValidateData(Socks5Info info, out bool gt)
        {
            gt = false;
            return info.Socks5Step switch
            {
                Socks5EnumStep.Request => Socks5Parser.ValidateRequestData(info.Data, out gt),
                Socks5EnumStep.Command => Socks5Parser.ValidateCommandData(info.Data, out gt),
                Socks5EnumStep.Auth => Socks5Parser.ValidateAuthData(info.Data, info.AuthType, out gt),
                Socks5EnumStep.Forward => true,
                Socks5EnumStep.ForwardUdp => true,
                _ => true
            };
        }


        Socks5Info udpInfo = new Socks5Info { Id = 0, Socks5Step = Socks5EnumStep.ForwardUdp };
        private async void ProcessReceiveUdp(IAsyncResult result)
        {
            IPEndPoint rep = null;
            try
            {
                udpInfo.Data = udpClient.EndReceive(result, ref rep);
                udpInfo.SourceEP = rep;

                await ExecuteHandle(udpInfo);
                udpInfo.Data = Helper.EmptyArray;

                result = udpClient.BeginReceive(ProcessReceiveUdp, null);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"socks5 listen udp -> error " + ex);
            }
        }
        private async Task ExecuteHandle(Socks5Info info)
        {
            await Semaphore.WaitAsync();
            if (await OnData(info) == false)
            {
                CloseClientSocket(info.Id);
            }
            Semaphore.Release();
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        public async Task Response(Socks5Info info)
        {
            if (connections.TryGetValue(info.Id, out AsyncUserToken token))
            {
                if (info.Data.Length == 0)
                {
                    CloseClientSocket(info.Id);
                }
                else
                {
                    if (info.Socks5Step < token.DataWrap.Socks5Step)
                    {
                        return;
                    }
                    token.DataWrap.Socks5Step = info.Socks5Step;
                    token.DataWrap.AuthType = info.AuthType;
                    if (info.Socks5Step == Socks5EnumStep.ForwardUdp)
                    {
                        await udpClient.SendAsync(info.Data, info.SourceEP);
                    }
                    else
                    {
                        try
                        {
                            await token.Socket.SendAsync(info.Data, SocketFlags.None);
                        }
                        catch (Exception)
                        {
                            CloseClientSocket(info.Id);
                        }
                    }

                }
            }
            else if (info.SourceEP != null)
            {
                try
                {
                    await udpClient.SendAsync(info.Data, info.SourceEP);
                }
                catch (Exception)
                {
                }
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.Disposabled == false)
            {
                e.Dispose();
                if (connections.TryRemove(token.DataWrap.Id, out _))
                {
                    if (OnClose != null && token.Disposabled == false)
                    {
                        OnClose(token.DataWrap);
                    }
                    token.Clear();
                }
            }
        }
        private void CloseClientSocket(ulong id)
        {
            if (connections.TryRemove(id, out AsyncUserToken token))
            {
                token.Clear();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void Close(ulong id)
        {
            CloseClientSocket(id);
        }
        /// <summary>
        /// 
        /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    public class AsyncUserToken
    {
        /// <summary>
        /// 
        /// </summary>
        public Socket Socket { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] PoolBuffer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5Info DataWrap { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Disposabled { get; private set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            Socket?.SafeClose();
            Socket = null;
            PoolBuffer = Helper.EmptyArray;

            Disposabled = true;
        }
    }

}
