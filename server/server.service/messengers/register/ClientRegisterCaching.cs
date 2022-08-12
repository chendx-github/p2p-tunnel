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


        public ClientRegisterCaching(WheelTimer<object> wheelTimer, Config config)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = TimeoutCallback, }, 1000, true);
            this.config = config;
        }
        private void TimeoutCallback(WheelTimerTimeout<object> timeout)
        {
            if (cache.Count > 0)
            {
                long time = DateTimeHelper.GetTimeStamp();
                var offlines = cache.Values.Where(c => c.UdpConnection != null && c.UdpConnection.IsTimeout(time, config.TimeoutDelay));
                foreach (RegisterCacheInfo item in offlines)
                {
                    Remove(item.Id);
                }
            }
        }

        public int Count()
        {
            return cache.Count;
        }

        public bool Get(ulong id, out RegisterCacheInfo client)
        {
            return cache.TryGetValue(id, out client);
        }

        public RegisterCacheInfo GetBySameGroup(string groupid, string name)
        {
            return cache.Values.FirstOrDefault(c => c.GroupId == groupid && c.Name == name);
        }

        public List<RegisterCacheInfo> GetAll()
        {
            return cache.Values.ToList();
        }
        public RegisterCacheInfo GetByName(string name)
        {
            return cache.Values.FirstOrDefault(c => c.Name == name);
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

        public bool Remove(ulong id)
        {
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                return Offline(client);
            }
            return false;
        }
        public bool Offline(RegisterCacheInfo client)
        {
            if (client != null)
            {
                client.UdpConnection?.Disponse();
                client.TcpConnection?.Disponse();
                OnChanged.Push(client);
                OnOffline.Push(client);
                return true;
            }
            return false;
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
