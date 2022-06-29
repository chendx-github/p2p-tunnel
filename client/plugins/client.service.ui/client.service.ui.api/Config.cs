using common.libs.database;
using MessagePack;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace client.service.ui.api
{
    /// <summary>
    /// 配置信息
    /// </summary>
    [Table("ui-appsettings")]
    public class Config
    {
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Websocket = config.Websocket;
            Web = config.Web;
        }

        /// <summary>
        /// 本地websocket
        /// </summary>
        public WebsocketConfig Websocket { get; set; } = new WebsocketConfig();
        /// <summary>
        /// 本地web管理端配置
        /// </summary>
        public WebConfig Web { get; set; } = new WebConfig();


        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig().ConfigureAwait(false);

            config.Web = Web;
            config.Websocket = Websocket;

            await configDataProvider.Save(config).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 本地web管理端配置
    /// </summary>
    [MessagePackObject]
    public class WebConfig
    {
        [Key(1)]
        public int Port { get; set; } = 8098;
        [Key(2)]
        public string Root { get; set; } = "./web";
        [Key(3)]
        public bool UseIpv6 { get; set; } = false;

        [JsonIgnore, IgnoreMember]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }

    }
    [MessagePackObject]
    public class WebsocketConfig
    {
        [Key(1)]
        public int Port { get; set; } = 8098;
        [Key(2)]
        public bool UseIpv6 { get; set; } = false;

        [JsonIgnore, IgnoreMember]
        public IPAddress BindIp
        {
            get
            {
                return UseIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            }
        }
    }
}
