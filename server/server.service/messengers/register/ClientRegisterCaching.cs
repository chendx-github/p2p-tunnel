﻿using common.libs;
using common.libs.extends;
using common.server;
using server.messengers.register;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace server.service.messengers.register
{
    public class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly ConcurrentDictionary<ulong, RegisterCacheInfo> cache = new();
        private NumberSpace idNs = new NumberSpace(0);
        private readonly Config config;

        public SimpleSubPushHandler<RegisterCacheInfo> OnChanged { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        public SimpleSubPushHandler<RegisterCacheInfo> OnOffline { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        WheelTimer<IConnection> wheelTimer = new WheelTimer<IConnection>();

        public int Count { get => cache.Count; }

        public ClientRegisterCaching(Config config, IUdpServer udpServer, ITcpServer tcpServer)
        {
            //心跳检测timer
            //wheelTimer.NewTimeout(new WheelTimerTimeoutTask<IConnection> { Callback = TimeoutCallback, }, 1000, true);
            this.config = config;

            tcpServer.OnDisconnect.Sub((IConnection connection) =>
            {
                Remove(connection.ConnectId);
            });
            udpServer.OnDisconnect.Sub((IConnection connection) =>
            {
                Remove(connection.ConnectId);
            });
            tcpServer.OnConnected = (IConnection connection) =>
            {
                wheelTimer.NewTimeout(new WheelTimerTimeoutTask<IConnection>
                {
                    Callback = ConnectionTimeoutCallback,
                    State = connection
                }, config.RegisterTimeout, false);
            };
            udpServer.OnConnected = (IConnection connection) =>
            {
                wheelTimer.NewTimeout(new WheelTimerTimeoutTask<IConnection>
                {
                    Callback = ConnectionTimeoutCallback,
                    State = connection
                }, config.RegisterTimeout, false);
            };
        }
        private void ConnectionTimeoutCallback(WheelTimerTimeout<IConnection> timeout)
        {
            IConnection connection = timeout.Task.State;
            if (connection != null && connection.ConnectId == 0)
            {
                connection.Disponse();
            }
        }

        /// <summary>
        /// 超时检测
        /// </summary>
        /// <param name="timeout"></param>
        private void TimeoutCallback(WheelTimerTimeout<IConnection> timeout)
        {
            if (cache.Count > 0)
            {
                try
                {
                    long time = DateTimeHelper.GetTimeStamp();
                    var offlines = cache.Values.Where(c => c.UdpConnection != null && c.UdpConnection.IsTimeout(time, config.TimeoutDelay));
                    if (offlines.Any())
                    {
                        Logger.Instance.Debug($"检测到超时 {offlines.Count()} 个");
                        foreach (RegisterCacheInfo item in offlines)
                        {
                            Remove(item.Id);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public ulong Add(RegisterCacheInfo model)
        {
            if (model.Id == 0)
            {
                model.Id = idNs.Increment();
            }
            if (string.IsNullOrWhiteSpace(model.GroupId))
            {
                model.GroupId = Guid.NewGuid().ToString().Md5();
            }
            cache.AddOrUpdate(model.Id, model, (a, b) => model);
            return model.Id;
        }

        public bool Get(ulong id, out RegisterCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }
        public IEnumerable<RegisterCacheInfo> GetBySameGroup(string groupid)
        {
            return cache.Values.Where(c => c.GroupId == groupid);
        }
        public IEnumerable<RegisterCacheInfo> GetBySameGroup(string groupid, string name)
        {
            return cache.Values.Where(c => c.GroupId == groupid && c.Name == name);
        }
        public List<RegisterCacheInfo> GetAll()
        {
            return cache.Values.ToList();
        }
        public RegisterCacheInfo GetByName(string name)
        {
            return cache.Values.FirstOrDefault(c => c.Name == name);
        }
        /// <summary>
        /// 移除并断开客户端
        /// </summary>
        /// <param name="id"></param>
        public void Remove(ulong id)
        {
            Logger.Instance.Debug($"Remove id {id} ");
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                if (client != null)
                {
                    client.UdpConnection?.Disponse();
                    client.TcpConnection?.Disponse();
                    OnChanged.Push(client);
                    OnOffline.Push(client);
                }
            }
        }

        public bool Notify(IConnection connection)
        {
            if (Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                OnChanged.Push(client);
            }
            return false;
        }
    }
}
