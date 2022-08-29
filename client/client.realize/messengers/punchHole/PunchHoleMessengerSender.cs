﻿using client.messengers.punchHole;
using client.messengers.register;
using common.libs;
using common.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using client.realize.messengers.register;

namespace client.realize.messengers.punchHole
{
    public class PunchHoleMessengerSender
    {
        private Dictionary<PunchHoleTypes, IPunchHole> plugins = new Dictionary<PunchHoleTypes, IPunchHole>();

        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly ServiceProvider serviceProvider;
        private readonly RegisterMessengerSender registerMessengerSender;

        public PunchHoleMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, ServiceProvider serviceProvider, RegisterMessengerSender registerMessengerSender)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.serviceProvider = serviceProvider;
            this.registerMessengerSender = registerMessengerSender;
        }

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

        public void OnPunchHole(OnPunchHoleArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;

            if (plugins.ContainsKey(type))
            {
                IPunchHole plugin = plugins[type];
                plugin?.Execute(arg);
            }
        }

        public async Task Send<T>(SendPunchHoleArg<T> arg) where T : IPunchHoleStepInfo
        {
            IPunchHoleStepInfo msg = arg.Data;
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = arg.Connection,
                Path = "punchhole/Execute",
                Content = new PunchHoleParamsInfo
                {
                    Data = msg.ToBytes(),
                    PunchForwardType = msg.ForwardType,
                    FromId = 0,
                    PunchStep = msg.Step,
                    PunchType = (byte)msg.PunchType,
                    ToId = arg.ToId,
                    TunnelName = arg.TunnelName,
                    GuessPort = arg.GuessPort,
                }.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<int> GetGuessPort(ServerType serverType)
        {
            return await registerMessengerSender.GetGuessPort(serverType);
        }

        public SimpleSubPushHandler<OnPunchHoleArg> OnReverse { get; } = new SimpleSubPushHandler<OnPunchHoleArg>();
        public async Task SendReverse(ulong toid, byte tryreverse = 0)
        {
            await Send(new SendPunchHoleArg<PunchHoleReverseInfo>
            {
                Connection = registerState.UdpConnection,
                ToId = toid,
                Data = new PunchHoleReverseInfo { TryReverse = tryreverse }
            }).ConfigureAwait(false);
        }

        public async Task SendReset(ulong toid)
        {
            await Send(new SendPunchHoleArg<PunchHoleResetInfo>
            {
                Connection = registerState.UdpConnection,
                ToId = toid,
                Data = new PunchHoleResetInfo { }
            }).ConfigureAwait(false);
        }
    }

    public class SendPunchHoleArg<T>
    {
        public IConnection Connection { get; set; }

        public ulong ToId { get; set; }
        public int GuessPort { get; set; } = 0;

        public string TunnelName { get; set; } = string.Empty;

        public T Data { get; set; }
    }


}
