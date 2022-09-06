using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
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
        /// <summary>
        /// 收到线路的数据
        /// </summary>
        /// <param name="request"></param>
        private void OnRequest(TcpForwardInfo request)
        {
            if (request.Connection == null || !request.Connection.Connected)
            {
                request.Connection = null;
                GetTarget(request);
            }
            if (request.Connection == null)
            {
                request.Buffer = HttpParseHelper.BuildMessage("未选择转发对象，或者未与转发对象建立连接");
                tcpForwardServer.Response(request);//关闭监听来的连接
            }
            else
            {
                request.Connection = request.Connection;
                request.Connection.ReceiveBytes += request.Buffer.Length;
                tcpForwardMessengerSender.SendRequest(request).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
        /// <summary>
        /// 获取发送对象
        /// </summary>
        /// <param name="request"></param>
        private void GetTarget(TcpForwardInfo request)
        {
            request.ForwardType = TcpForwardTypes.FORWARD;
            //短链接
            if (request.AliveType == TcpForwardAliveTypes.WEB)
            {
                //http1.1代理
                if (HttpConnectMethodHelper.IsConnectMethod(request.Buffer.Span))
                {
                    request.ForwardType = TcpForwardTypes.PROXY;
                    tcpForwardTargetProvider?.Get(request.SourcePort, request);
                    request.TargetEndpoint = HttpConnectMethodHelper.GetHost(request.Buffer);
                }
                //正常的http请求
                else
                {
                    string domain = HttpParseHelper.GetHost(request.Buffer.Span).GetString();
                    tcpForwardTargetProvider?.Get(domain, request);
                }
            }
            //长连接
            else
            {
                tcpForwardTargetProvider?.Get(request.SourcePort, request);
            }
        }
    }
}
