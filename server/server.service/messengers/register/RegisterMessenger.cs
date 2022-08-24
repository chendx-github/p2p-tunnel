using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.messengers.register
{
    public class RegisterMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly Config config;
        private readonly IRegisterKeyValidator registerKeyValidator;

        public RegisterMessenger(IClientRegisterCaching clientRegisterCache, Config config, IRegisterKeyValidator registerKeyValidator)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.config = config;
            this.registerKeyValidator = registerKeyValidator;
        }

        public byte[] Execute(IConnection connection)
        {
            RegisterParamsInfo model = new RegisterParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Memory);
            //验证key
            if (!registerKeyValidator.Validate(connection, model))
            {
                return new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.KEY_VERIFY }.ToBytes();
            }

            return connection.ServerType switch
            {
                ServerType.UDP => Udp(connection, model),
                ServerType.TCP => Tcp(connection, model),
                _ => new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.UNKNOW }.ToBytes()
            };
        }
        private byte[] Udp(IConnection connection, RegisterParamsInfo model)
        {
            (RegisterResultInfo verify, RegisterCacheInfo client) = VerifyAndAdd(model);
            if (verify != null)
            {
                return verify.ToBytes();
            }

            client.UpdateUdpInfo(connection);
            client.AddTunnel(new TunnelRegisterCacheInfo
            {
                Port = connection.Address.Port,
                LocalPort = model.LocalUdpPort,
                Servertype = ServerType.UDP,
                TunnelName = "udp",
                IsDefault = true,
            });

            return new RegisterResultInfo
            {
                Id = client.Id,
                Ip = connection.Address.Address,
                UdpPort = connection.Address.Port,
                TcpPort = 0,
                GroupId = client.GroupId,
                Relay = config.Relay,
                TimeoutDelay = config.TimeoutDelay
            }.ToBytes();
        }
        private byte[] Tcp(IConnection connection, RegisterParamsInfo model)
        {
            (RegisterResultInfo verify, RegisterCacheInfo client) = VerifyAndAdd(model);
            if (verify != null)
            {
                return verify.ToBytes();
            }

            client.UpdateTcpInfo(connection);
            client.AddTunnel(new TunnelRegisterCacheInfo
            {
                Port = connection.Address.Port,
                LocalPort = model.LocalTcpPort,
                Servertype = ServerType.TCP,
                TunnelName = "tcp",
                IsDefault = true,
            });

            return new RegisterResultInfo
            {
                Id = model.Id,
                Ip = client.UdpConnection.Address.Address,
                UdpPort = client.UdpConnection.Address.Port,
                TcpPort = connection.Address.Port,
                GroupId = client.GroupId,
                Relay = config.Relay,
                TimeoutDelay = config.TimeoutDelay
            }.ToBytes();
        }
        private (RegisterResultInfo, RegisterCacheInfo) VerifyAndAdd(RegisterParamsInfo model)
        {
            RegisterResultInfo verify = null;
            RegisterCacheInfo client;
            //不是第一次注册
            if (model.Id > 0)
            {
                if (!clientRegisterCache.Get(model.Id, out client))
                {
                    verify = new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.VERIFY };
                }
            }
            else
            {
                //第一次注册，检查有没有重名
                client = clientRegisterCache.GetBySameGroup(model.GroupId, model.Name).FirstOrDefault();
                if (client == null)
                {
                    client = new()
                    {
                        Name = model.Name,
                        GroupId = model.GroupId,
                        LocalIps = model.LocalIps,
                        Mac = model.Mac,
                        Id = 0
                    };
                    clientRegisterCache.Add(client);
                }
                else
                {
                    verify = new RegisterResultInfo { Code = RegisterResultInfo.RegisterResultInfoCodes.SAME_NAMES };
                }
            }
            return (verify, client);
        }

        public byte[] Ip(IConnection connection)
        {
            return connection.Address.Address.GetAddressBytes();
        }
        public byte[] Port(IConnection connection)
        {
            return connection.Address.Port.ToBytes();
        }

        public void Notify(IConnection connection)
        {
            clientRegisterCache.Notify(connection);
        }

    }
}
