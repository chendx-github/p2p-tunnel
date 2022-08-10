using common.libs;
using common.server;
using System.Collections.Generic;

namespace server.messengers.register
{
    public interface IClientRegisterCaching
    {
        public SimpleSubPushHandler<RegisterCacheInfo> OnChanged { get; }
        public SimpleSubPushHandler<RegisterCacheInfo> OnOffline { get; }

        public int Count();
        public bool Get(ulong id, out RegisterCacheInfo client);

        public RegisterCacheInfo GetBySameGroup(string groupid, string name);
        public List<RegisterCacheInfo> GetAll();
        public RegisterCacheInfo GetByName(string name);
        public ulong Add(RegisterCacheInfo model);
        public bool Remove(ulong id);
        public bool Notify(IConnection connection);
    }
}
