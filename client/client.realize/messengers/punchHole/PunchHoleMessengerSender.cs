﻿using client.messengers.punchHole;
using client.messengers.register;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using client.messengers.clients;
using System.Collections.Concurrent;
using System.Threading;

namespace client.realize.messengers.punchHole
{
    /// <summary>
    /// 打洞消息
    /// </summary>
    public sealed class PunchHoleMessengerSender
    {
        private Dictionary<PunchHoleTypes, IPunchHole> plugins = new Dictionary<PunchHoleTypes, IPunchHole>();

        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly ServiceProvider serviceProvider;

        private NumberSpace numberSpace = new NumberSpace();
        private WheelTimer<TimeoutState> wheelTimer = new WheelTimer<TimeoutState>();
        private ConcurrentDictionary<ulong, WheelTimerTimeout<TimeoutState>> sends = new ConcurrentDictionary<ulong, WheelTimerTimeout<TimeoutState>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        /// <param name="registerState"></param>
        /// <param name="serviceProvider"></param>
        public PunchHoleMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, ServiceProvider serviceProvider)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        /// <param name="assemblys"></param>
        public void LoadPlugins(Assembly[] assemblys)
        {
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IPunchHole)))
            {
                IPunchHole obj = (IPunchHole)serviceProvider.GetService(item);
                if (!plugins.ContainsKey(obj.Type))
                {
                    plugins.Add(obj.Type, obj);
                }
            }
        }
        /// <summary>
        /// 收到打洞消息
        /// </summary>
        /// <param name="arg"></param>
        public void OnPunchHole(OnPunchHoleArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;
            if (plugins.TryGetValue(type, out IPunchHole value))
            {
                value?.Execute(arg);
            }
        }
        /// <summary>
        /// 收到回执
        /// </summary>
        /// <param name="response"></param>
        public void OnResponse(PunchHoleResponseInfo response)
        {
            if (sends.TryRemove(response.RequestId, out WheelTimerTimeout<TimeoutState> timeout))
            {
                timeout.Task.State.Tcs.SetResult(true);
            }
        }

        /// <summary>
        /// 发送回执
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task Response(IConnection connection, PunchHoleRequestInfo request)
        {

            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)PunchHoleMessengerIds.Response,
                Payload = new PunchHoleResponseInfo
                {
                    FromId = registerState.ConnectId,
                    ToId = request.FromId,
                    RequestId = request.RequestId
                }.ToBytes()
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <param name="requestid"></param>
        /// <returns></returns>
        public async Task<bool> Request<T>(SendPunchHoleArg<T> arg, ulong requestid = 0) where T : IPunchHoleStepInfo
        {
            IPunchHoleStepInfo msg = arg.Data;
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = arg.Connection,
                MessengerId = (ushort)PunchHoleMessengerIds.Request,
                Payload = new PunchHoleRequestInfo
                {
                    RequestId = requestid,
                    Data = msg.ToBytes(),
                    PunchForwardType = msg.ForwardType,
                    FromId = registerState.ConnectId,
                    PunchStep = msg.Step,
                    PunchType = (byte)msg.PunchType,
                    ToId = arg.ToId,
                    TunnelName = arg.TunnelName,
                    Index = arg.Index,
                }.ToBytes()
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 发送等待回执的请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> RequestReply<T>(SendPunchHoleArg<T> arg) where T : IPunchHoleStepInfo
        {
            ulong requestid = numberSpace.Increment();
            TimeoutState timeoutState = new TimeoutState
            {
                RequestId = requestid,
                Tcs = new TaskCompletionSource<bool>()
            };
            var timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TimeoutState> { Callback = Callback, State = timeoutState }, 2000);
            sends.TryAdd(requestid, timeout);

            bool res = await Request(arg, requestid);
            if (res == false)
            {
                timeoutState.Tcs.SetResult(false);
            }

            return await timeoutState.Tcs.Task.ConfigureAwait(false);
        }
        private void Callback(WheelTimerTimeout<TimeoutState> timeout)
        {
            if (sends.TryRemove(timeout.Task.State.RequestId, out _))
            {
                timeout.Task.State.Tcs.SetResult(false);
            }
        }

        /// <summary>
        /// 收到反向连接
        /// </summary>
        public SimpleSubPushHandler<OnPunchHoleArg> OnReverse { get; } = new SimpleSubPushHandler<OnPunchHoleArg>();
        /// <summary>
        /// 通知其反向连接
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public async Task SendReverse(ClientInfo info)
        {
            byte times = info.TryReverseValue;
            await Request(new SendPunchHoleArg<PunchHoleReverseInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = info.Id,
                Data = new PunchHoleReverseInfo { Value = times }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<PunchHoleTunnelInfo> OnTunnel { get; } = new SimpleSubPushHandler<PunchHoleTunnelInfo>();
        /// <summary>
        /// 通知其开启新通道
        /// </summary>
        /// <param name="toid"></param>
        /// <param name="tunnelName"></param>
        /// <returns></returns>
        public async Task SendTunnel(ulong toid, ulong tunnelName)
        {
            await Request(new SendPunchHoleArg<PunchHoleTunnelInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleTunnelInfo { TunnelName = tunnelName, }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 通知其重置注册
        /// </summary>
        /// <param name="toid"></param>
        /// <returns></returns>
        public async Task SendReset(ulong toid)
        {
            await Request(new SendPunchHoleArg<PunchHoleResetInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleResetInfo { }
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 通知其掉线
        /// </summary>
        /// <param name="toid"></param>
        /// <returns></returns>
        public async Task SendOffline(ulong toid)
        {
            await Request(new SendPunchHoleArg<PunchHoleOfflineInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleOfflineInfo { }
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 发送打洞消息的参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SendPunchHoleArg<T>
    {
        /// <summary>
        /// 连接
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 给谁
        /// </summary>
        public ulong ToId { get; set; }
        /// <summary>
        /// 通道
        /// </summary>
        public ulong TunnelName { get; set; } = 0;
        /// <summary>
        /// 次数
        /// </summary>
        public byte Index { get; set; } = 0;

        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    /// 请求消息超时缓存
    /// </summary>
    public class TimeoutState
    {
        /// <summary>
        /// 请求id
        /// </summary>
        public ulong RequestId { get; set; }
        /// <summary>
        /// task
        /// </summary>
        public TaskCompletionSource<bool> Tcs { get; set; }
    }

}
