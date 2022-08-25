using common.libs;
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

        public int Count { get => cache.Count; }

        public ClientRegisterCaching(WheelTimer<object> wheelTimer, Config config, IUdpServer udpServer, ITcpServer tcpServer)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = TimeoutCallback, }, 1000, true);
            this.config = config;

            tcpServer.OnDisconnect.Sub((IConnection connection) =>
            {
                Remove(connection.ConnectId);
            });
            udpServer.OnDisconnect.Sub((IConnection connection) =>
            {
                Remove(connection.ConnectId);
            });
        }
        private void TimeoutCallback(WheelTimerTimeout<object> timeout)
        {
            if (cache.Count > 0)
            {
                try
                {
                    long time = DateTimeHelper.GetTimeStamp();
                    var offlines = cache.Values
                        .Where(c => c.TcpConnection != null)
                        .Where(c => c.UdpConnection.IsTimeout(time, config.TimeoutDelay));
                    if (offlines.Any())
                    {
                        foreach (RegisterCacheInfo item in offlines)
                        {
                            cache.TryRemove(item.Id, out _);
                            item.UdpConnection?.Disponse();
                            item.TcpConnection?.Disponse();
                        }
                        foreach (RegisterCacheInfo item in offlines)
                        {
                            OnChanged.Push(item);
                            OnOffline.Push(item);
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


        private void Remove(ulong id)
        {
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
