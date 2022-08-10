using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace common.server.servers.websocket
{
    public class WebSocket
    {
        private Socket socket;
        private int BufferSize = 4 * 1024;

        private readonly ConcurrentDictionary<ulong, AsyncUserToken> connections = new();
        private readonly NumberSpace numberSpace = new NumberSpace(0);

        /// <summary>
        /// 文本数据
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo, string> OnMessage;
        /// <summary>
        /// 二进制数据
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo, Memory<byte>> OnBinary;
        /// <summary>
        /// 非控制帧
        /// </summary>
        public Action<WebsocketConnection, WebsocketHeaderInfo> OnConnect = (connection, header) => { header.SecWebSocketExtensions = Helper.EmptyArray; };
        /// <summary>
        /// 控制帧
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo> OnControll = (connection, frame) => { };
        /// <summary>
        /// 非控制帧
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo> OnUnControll = (connection, frame) => { };

        public IEnumerable<WebsocketConnection> Connections
        {
            get
            {
                return connections.Values.Select(c => c.Connectrion);
            }
        }

        public WebSocket()
        {
            handles = new Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncUserToken>> {
                //直接添加数据
                { WebSocketFrameInfo.EnumOpcode.Data,HandleAppendData},
                //记录opcode并添加
                { WebSocketFrameInfo.EnumOpcode.Text,HandleData},
                { WebSocketFrameInfo.EnumOpcode.Binary,HandleData},

                { WebSocketFrameInfo.EnumOpcode.Ping,HandlePing},
                { WebSocketFrameInfo.EnumOpcode.Pong,HandlePong},
            };
        }

        public void Start(IPAddress bindip, int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(bindip, port);

            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = socket,
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);
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
                    break;
            }
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            Socket listenSocket = ((Socket)acceptEventArg.UserToken);
            try
            {
                if (!listenSocket.AcceptAsync(acceptEventArg))
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
            ulong id = numberSpace.Increment();
            AsyncUserToken token = new AsyncUserToken
            {
                Connectrion = new WebsocketConnection { Socket = socket, Id = numberSpace.Get() }
            };
            connections.TryAdd(token.Connectrion.Id, token);
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            token.PoolBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            readEventArgs.SetBuffer(token.PoolBuffer, 0, BufferSize);
            readEventArgs.Completed += IO_Completed;
            if (!socket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    var memory = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    ReadFrame(token, memory);
                    if (token.Connectrion.Socket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(token.Connectrion.Socket.Available);
                        while (token.Connectrion.Socket.Available > 0)
                        {
                            int length = token.Connectrion.Socket.Receive(arr);
                            if (length > 0)
                            {
                                memory = arr.AsMemory(0, length);
                                ReadFrame(token, memory);
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (!token.Connectrion.Socket.Connected)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (!token.Connectrion.Socket.ReceiveAsync(e))
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception)
            {
                CloseClientSocket(e);
            }
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (!token.Connectrion.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.Disposabled == false)
            {
                e.Dispose();
                if (connections.TryRemove(token.Connectrion.Id, out _))
                {
                    token.Clear();
                }
            }

        }
        public void Stop()
        {
            socket?.SafeClose();
            foreach (var item in connections.Values)
            {
                item.Clear();
            }
            connections.Clear();
        }


        private readonly Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncUserToken>> handles;
        private void ReadFrame(AsyncUserToken token, Memory<byte> data)
        {
            if (token.Connectrion.Connected)
            {
                if (token.FrameBuffer.Size == 0 && data.Length > 6)
                {
                    if (WebSocketFrameInfo.TryParse(data, out token.FrameInfo))
                    {
                        ExecuteHandle(token);
                        if (token.FrameInfo.TotalLength == data.Length)
                        {
                            return;
                        }
                        token.FrameBuffer.AddRange(data.Slice(token.FrameInfo.TotalLength));
                    }
                    else
                    {
                        token.FrameBuffer.AddRange(data);
                    }
                }
                else
                {
                    token.FrameBuffer.AddRange(data);
                }

                do
                {
                    if (!WebSocketFrameInfo.TryParse(token.FrameBuffer.Data, out token.FrameInfo))
                    {
                        break;
                    }
                    ExecuteHandle(token);
                    if (token.FrameInfo.TotalLength > 0)
                    {
                        token.FrameBuffer.RemoveRange(0, token.FrameInfo.TotalLength);
                    }
                } while (token.FrameBuffer.Size > 6);
            }
            else
            {
                HandleConnect(token, data);
            }
        }
        private void ExecuteHandle(AsyncUserToken token)
        {
            if (handles.TryGetValue(token.FrameInfo.Opcode, out Action<AsyncUserToken> action))
            {
                action(token);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll3 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll7)
            {
                OnUnControll(token.Connectrion, token.FrameInfo);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll11 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll15)
            {
                OnControll(token.Connectrion, token.FrameInfo);
            }
            else
            {
                HandleClose(token);
                return;
            }
        }
        private void HandleData(AsyncUserToken token)
        {
            token.Opcode = token.FrameInfo.Opcode;
            HandleAppendData(token);
        }
        private void HandleAppendData(AsyncUserToken token)
        {
            Memory<byte> tempData;
            if (token.ReceiveDataBuffer.Size == 0 && token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
            {
                tempData = token.FrameInfo.PayloadData;
            }
            else
            {
                token.ReceiveDataBuffer.AddRange(token.FrameInfo.PayloadData);
                tempData = token.ReceiveDataBuffer.Data;
            }
            if (token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
            {
                if (token.Opcode == WebSocketFrameInfo.EnumOpcode.Text)
                {
                    string str = tempData.GetString();
                    OnMessage(token.Connectrion, token.FrameInfo, str);
                }
                else
                {
                    OnBinary(token.Connectrion, token.FrameInfo, tempData);
                }
            }
            token.ReceiveDataBuffer.Clear();
        }

        private void HandleClose(AsyncUserToken token)
        {
            token.Connectrion.Close();
        }
        private void HandlePing(AsyncUserToken token)
        {
            token.Connectrion.Pong();
        }
        private void HandlePong(AsyncUserToken token) { }
        private void HandleConnect(AsyncUserToken token, Memory<byte> data)
        {
            WebsocketHeaderInfo header = WebsocketHeaderInfo.Parse(data);
            if (header.SecWebSocketKey.Length == 0)
            {
                token.Connectrion.Close();
                return;
            }

            OnConnect(token.Connectrion, header);
            token.Connectrion.Connected = true;
            token.Connectrion.ConnectResponse(header);
        }

    }


    public class WebsocketConnection
    {
        public ulong Id { get; set; }
        public Socket Socket { get; set; }
        public bool Connected { get; set; } = false;

        private bool Closed = false;

        public int Pong()
        {
            return SendRaw(WebSocketParser.BuildPongData());
        }
        public int SendFrame(byte[] buffer)
        {
            var frame = WebSocketParser.BuildFrameData(buffer, WebSocketFrameInfo.EnumOpcode.Text);
            return SendRaw(frame);
        }
        public int ConnectResponse(WebsocketHeaderInfo header)
        {
            var data = WebSocketParser.BuildConnectData(header);
            return SendRaw(data);
        }
        public int SendRaw(byte[] buffer)
        {
            return Socket.Send(buffer);
        }

        public void Close()
        {
            if (!Closed)
            {
                Socket?.SafeClose();
            }
            Closed = true;
            Connected = false;
        }
    }

    public class AsyncUserToken
    {
        public WebsocketConnection Connectrion { get; set; }

        //处理数据
        public WebSocketFrameInfo FrameInfo = null;
        public ReceiveDataBuffer FrameBuffer { get; } = new ReceiveDataBuffer();
        public ReceiveDataBuffer ReceiveDataBuffer { get; } = new ReceiveDataBuffer();
        public WebSocketFrameInfo.EnumOpcode Opcode { get; set; }

        public byte[] PoolBuffer { get; set; }
        public bool Disposabled { get; private set; } = false;
        public void Clear()
        {
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }

            Disposabled = true;
            Connectrion.Close();
        }
    }

}
