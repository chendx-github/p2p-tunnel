using common.libs;
using common.libs.extends;
using System;
using System.Text;

namespace common.tcpforward
{
    public class TcpForwardTransferBase
    {
        private readonly ITcpForwardServer tcpForwardServer;
        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        private readonly ITcpForwardTargetProvider tcpForwardTargetProvider;


        public TcpForwardTransferBase(ITcpForwardServer tcpForwardServer, TcpForwardMessengerSender tcpForwardMessengerSender, ITcpForwardTargetProvider tcpForwardTargetProvider)
        {
            this.tcpForwardServer = tcpForwardServer;
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.tcpForwardTargetProvider = tcpForwardTargetProvider;

            //A来了请求 ，转发到B，
            tcpForwardServer.OnRequest.Sub(OnRequest);
            //A收到B的回复
            tcpForwardMessengerSender.OnResponseHandler.Sub(tcpForwardServer.Response);
        }

        private void OnRequest(TcpForwardRequestInfo request)
        {
            if (request.Connection == null || !request.Connection.Connected)
            {
                request.Connection = null;
                GetTarget(request);
            }

            if (request.Connection == null)
            {
                request.Msg.Buffer = HttpParseHelper.BuildMessage("未选择转发对象，或者未与转发对象建立连接");
                tcpForwardServer.Response(request.Msg);
            }
            else
            {
                request.Connection.ReceiveBytes += (ulong)request.Msg.Buffer.Length;
                tcpForwardMessengerSender.SendRequest(new SendArg
                {
                    Data = request.Msg,
                    Connection = request.Connection
                }).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        private void GetTarget(TcpForwardRequestInfo request)
        {
            TcpForwardTargetInfo target;
            Memory<byte> ip = Helper.EmptyArray;

            request.Msg.ForwardType = TcpForwardTypes.FORWARD;
            //短链接
            if (request.Msg.AliveType == TcpForwardAliveTypes.WEB)
            {
                //http1.1代理
                if (HttpConnectMethodHelper.IsConnectMethod(request.Msg.Buffer.Span))
                {
                    request.Msg.ForwardType = TcpForwardTypes.PROXY;
                    target = tcpForwardTargetProvider?.Get(request.SourcePort);
                    if (target != null)
                    {
                        ip = HttpConnectMethodHelper.GetHost(request.Msg.Buffer);
                    }
                }
                //正常的http请求
                else
                {
                    string domain = HttpParseHelper.GetHost(request.Msg.Buffer.Span).GetString();
                    target = tcpForwardTargetProvider?.Get(domain);
                    if (target != null)
                    {
                        ip = target.Endpoint;
                    }
                }
            }
            //长连接
            else
            {
                target = tcpForwardTargetProvider?.Get(request.SourcePort);
                if (target != null)
                {
                    ip = target.Endpoint;
                }
            }

            if (target != null)
            {
                request.Connection = target.Connection;
                request.Msg.TargetEndpoint = ip;
            }
        }
    }
}
