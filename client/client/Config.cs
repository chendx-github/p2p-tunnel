using common.libs.database;
using MessagePack;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace client
{
    /// <summary>
    /// 配置信息
    /// </summary>
    [Table("appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Client = config.Client;
            Server = config.Server;
        }
        /// <summary>
        /// 客户端配置
        /// </summary>
        public ClientConfig Client { get; set; } = new ClientConfig();
        /// <summary>
        /// 服务器配置
        /// </summary>
        public ServerConfig Server { get; set; } = new ServerConfig();

        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.Client = Client;
            config.Server = Server;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 客户端配置
    /// </summary>
    [MessagePackObject]
    public class ClientConfig
    {
        /// <summary>
        /// 分组编号
        /// </summary>
        [Key(1)]
        public string GroupId { get; set; } = string.Empty;
        /// <summary>
        /// 客户端名
        /// </summary>
        [Key(2)]
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 自动注册
        /// </summary>
        [Key(3)]
        public bool AutoReg { get; set; } = false;
        /// <summary>
        /// 自动注册重试次数
        /// </summary>
        [Key(4)]
        public int AutoRegTimes { get; set; } = 10;
        /// <summary>
        /// 上报MAC地址
        /// </summary>
        [Key(5)]
        public bool UseMac { get; set; } = false;
        /// <summary>
        /// 使用ipv6
        /// </summary>
        [Key(6)]
        public bool UseIpv6 { get; set; } = false;

        [Key(7)]
        public int TcpBufferSize { get; set; } = 128 * 1024;

        [Key(8)]
        public string Key { get; set; } = string.Empty;

        [Key(9)]
        public bool Encode { get; set; } = false;

        [JsonIgnore, IgnoreMember]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            }
        }

        [JsonIgnore, IgnoreMember]
        public IPAddress LoopbackIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }
    }

    /// <summary>
    /// 服务器配置
    /// </summary>
    [MessagePackObject]
    public class ServerConfig
    {
        [Key(1)]
        public string Ip { get; set; } = string.Empty;
        [Key(2)]
        public int UdpPort { get; set; } = 8099;
        [Key(3)]
        public int TcpPort { get; set; } = 8000;
        [Key(4)]
        public bool Encode { get; set; } = false;
    }
}
