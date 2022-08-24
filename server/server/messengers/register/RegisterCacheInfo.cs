using common.libs;
using common.server;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;

namespace server.messengers.register
{
    public class RegisterCacheInfo
    {
        [JsonIgnore]
        public IConnection TcpConnection { get; private set; } = null;
        [JsonIgnore]
        public IConnection UdpConnection { get; private set; } = null;

        public ulong Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        [JsonIgnore]
        public long LastTime { get; set; }
        [JsonIgnore]
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        [JsonIgnore]
        public string Mac { get; set; } = string.Empty;

        public void UpdateUdpInfo(IConnection connection)
        {
            UdpConnection = connection;
            UdpConnection.ConnectId = Id;
            Logger.Instance.DebugDebug($"UpdateUdpInfo :{UdpConnection.GetHashCode()},{UdpConnection.ConnectId}");
        }
        public void UpdateTcpInfo(IConnection connection)
        {
            TcpConnection = connection;
            TcpConnection.ConnectId = Id;
            Logger.Instance.DebugDebug($"UpdateTcpInfo :{TcpConnection.GetHashCode()},{TcpConnection.ConnectId}");
        }

        private ConcurrentDictionary<string, TunnelRegisterCacheInfo> tunnels = new ConcurrentDictionary<string, TunnelRegisterCacheInfo>();
        public void AddTunnel(TunnelRegisterCacheInfo model)
        {
            tunnels.AddOrUpdate(model.TunnelName, model, (a, b) => model);
        }
        public bool TunnelExists(string tunnelName)
        {
            return tunnels.ContainsKey(tunnelName);
        }
        public bool GetTunnel(string tunnelName, out TunnelRegisterCacheInfo model)
        {
            return tunnels.TryGetValue(tunnelName, out model);
        }
    }

    public class UpdateUdpParamsInfo
    {
        public IConnection Connection { get; set; } = null;
    }
    public class UpdateTcpParamsInfo
    {
        public IConnection Connection { get; set; } = null;
    }

    public class TunnelRegisterCacheInfo
    {
        public string TunnelName { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public ServerType Servertype { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
