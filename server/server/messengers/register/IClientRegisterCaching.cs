using common.libs;
using common.server;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public Task<bool> Remove(ulong id);
        public Task<bool> Notify(IConnection connection);
    }
}
