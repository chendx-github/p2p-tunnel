using common.libs;
using common.libs.extends;
using common.server;
using server.messengers.register;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.messengers.register
{
    public class ClientRegisterCaching : IClientRegisterCaching
    {
        private readonly ConcurrentDictionary<ulong, RegisterCacheInfo> cache = new();
        private NumberSpace idNs = new NumberSpace(0);

        public SimpleSubPushHandler<RegisterCacheInfo> OnChanged { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();
        public SimpleSubPushHandler<RegisterCacheInfo> OnOffline { get; } = new SimpleSubPushHandler<RegisterCacheInfo>();


        public ClientRegisterCaching(WheelTimer<object> wheelTimer)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = TimeoutCallback, }, 1000, true);
        }
        private async void TimeoutCallback(WheelTimerTimeout<object> timeout)
        {
            if (cache.Count > 0)
            {
                long time = DateTimeHelper.GetTimeStamp();
                foreach (RegisterCacheInfo item in cache.Values)
                {
                    if ((time - item.UdpConnection.LastTime) > 20 * 1000)
                    {
                        await Remove(item.Id).ConfigureAwait(false);
                    }
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
            return cache.Values.FirstOrDefault(c => c.GroupId == groupid.Md5() && c.Name == name);
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
            if (string.IsNullOrWhiteSpace(model.OriginGroupId))
            {
                model.OriginGroupId = Guid.NewGuid().ToString().Md5();
            }
            model.GroupId = model.OriginGroupId.Md5();
            cache.AddOrUpdate(model.Id, model, (a, b) => model);
            return model.Id;
        }

        public async Task<bool> Remove(ulong id)
        {
            if (cache.TryRemove(id, out RegisterCacheInfo client))
            {
                Console.WriteLine("remove");

                client.UdpConnection?.Disponse();
                client.TcpConnection?.Disponse();
                Console.WriteLine("remove1");
                await OnChanged.PushAsync(client).ConfigureAwait(false);
                Console.WriteLine("remove2");
                await OnOffline.PushAsync(client).ConfigureAwait(false);
                Console.WriteLine("remove3");
                return true;
            }
            return false;
        }

        public async Task<bool> Notify(IConnection connection)
        {
            if (Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                await OnChanged.PushAsync(client).ConfigureAwait(false);
            }
            return false;
        }


    }
}
