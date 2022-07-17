using common.libs;
using common.libs.extends;
using common.server.middleware;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace common.server
{
    public class MessengerResolver
    {

        private readonly Dictionary<ReadOnlyMemory<byte>, MessengerCacheInfo> messengers = new(new MemoryByteDictionaryComparer());

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;
        private readonly MessengerSender messengerSender;
        private readonly MiddlewareTransfer middlewareTransfer;


        public MessengerResolver(IUdpServer udpserver, ITcpServer tcpserver, MessengerSender messengerSender, MiddlewareTransfer middlewareTransfer)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;
            this.messengerSender = messengerSender;
            this.middlewareTransfer = middlewareTransfer;

            this.tcpserver.OnPacket.Sub((IConnection connection) =>
            {
                InputData(connection).Wait();
            });
            this.udpserver.OnPacket.Sub((IConnection connection) =>
            {
                connection.UpdateTime(DateTimeHelper.GetTimeStamp());
                InputData(connection).Wait();
            });
        }
        public void LoadMessenger(Type type, object obj)
        {
            Type voidType = typeof(void);
            string path = type.Name.Replace("Messenger", "");
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Memory<byte> key = $"{path}/{method.Name}".ToLower().GetBytes().AsMemory();
                if (!messengers.ContainsKey(key))
                {
                    MessengerCacheInfo cache = new MessengerCacheInfo
                    {
                        IsVoid = method.ReturnType == voidType,
                        Method = method,
                        Target = obj,
                        IsTask = method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null,
                        IsTaskResult = method.ReturnType.GetProperty("Result") != null
                    };
                    messengers.TryAdd(key, cache);
                }

            }
        }

        public async Task InputData(IConnection connection)
        {
            var receive = connection.ReceiveData;
            var responseWrap = connection.ReceiveResponseWrap;
            var requestWrap = connection.ReceiveRequestWrap;

            MessageTypes type = (MessageTypes)receive.Span[0];
            //回复的消息
            if (type == MessageTypes.RESPONSE)
            {
                responseWrap.FromArray(receive);
                if (connection.EncodeEnabled)
                {
                    responseWrap.Memory = connection.Crypto.Decode(responseWrap.Memory);
                }
                messengerSender.Response(responseWrap);
                return;
            }


            requestWrap.FromArray(receive);
            if (connection.EncodeEnabled)
            {
                requestWrap.Memory = connection.Crypto.Decode(requestWrap.Memory);
            }
            try
            {
                //404,没这个插件
                if (!messengers.ContainsKey(requestWrap.Path))
                {

                    Logger.Instance.Error($"{requestWrap.Path.Span.GetString()},{receive.Length},{connection.ServerType}, not found");
                    await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                    {
                        Connection = connection,
                        RequestId = requestWrap.RequestId,
                        Code = MessageResponeCodes.NOT_FOUND
                    }).ConfigureAwait(false);
                    return;
                }

                MessengerCacheInfo plugin = messengers[requestWrap.Path];

                if (middlewareTransfer != null)
                {
                    var res = middlewareTransfer.Execute(connection);
                    if (!res.Item1)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                        {
                            Connection = connection,
                            RequestId = requestWrap.RequestId,
                            Code = MessageResponeCodes.ERROR,
                            Data = res.Item2
                        }).ConfigureAwait(false);
                        return;
                    }
                }

                dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { connection });
                //void的，task的 没有返回值，不回复，需要回复的可以返回任意类型
                if (plugin.IsVoid)
                {
                    return;
                }

                object resultObject = null;
                if (plugin.IsTask)
                {
                    if (plugin.IsTaskResult)
                    {
                        await resultAsync.ConfigureAwait(false);
                        resultObject = resultAsync.Result;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    resultObject = resultAsync;
                }

                await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                {
                    Connection = connection,
                    Data = resultObject != null ? resultObject.ToBytes() : Helper.EmptyArray,
                    RequestId = requestWrap.RequestId
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                {
                    Connection = connection,
                    RequestId = requestWrap.RequestId,
                    Code = MessageResponeCodes.ERROR
                }).ConfigureAwait(false);
            }
            finally
            {
                requestWrap.Reset();
            }
        }

        private struct MessengerCacheInfo
        {
            public object Target { get; set; }
            public MethodInfo Method { get; set; }
            public bool IsVoid { get; set; }
            public bool IsTask { get; set; }
            public bool IsTaskResult { get; set; }
        }
    }
}