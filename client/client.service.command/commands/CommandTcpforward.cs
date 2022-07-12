using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;

namespace client.service.command.commands
{
    internal class CommandTcpforward : CommandBase
    {
        public CommandTcpforward(RootCommand rootCommand)
        {
            Command tcpforward = new Command("tcpforward", "tcp转发相关命令") { };
            rootCommand.Add(tcpforward);


            Command list = new Command("list", "查看列表") { };
            tcpforward.Add(list);
            list.SetHandler(HandlerList);


            Command del = new Command("del", "删除监听") { };
            tcpforward.Add(del);
            var delid = new Argument<int>("id", "id");
            del.Add(delid);
            del.SetHandler((delid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/RemoveListen", new { id = delid, content = string.Empty }.ToJson()));
                PrintRequestState(res);
            }, delid);


            Command delForward = new Command("del-forward", "删除转发") { };
            tcpforward.Add(delForward);
            var delListenid = new Argument<int>("listenid", "监听id");
            var delForwardid = new Argument<int>("forwardid", "转发id");
            delForward.Add(delListenid);
            delForward.Add(delForwardid);
            delForward.SetHandler((delListenid, delForwardid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/RemoveForward", new { id = 0, content = new { ListenID = delListenid, ForwardID = delForwardid }.ToJson() }.ToJson()));
                PrintRequestState(res);
            }, delListenid, delForwardid);


            Command start = new Command("start", "启动监听") { };
            tcpforward.Add(start);
            var startid = new Argument<int>("id", "id");
            start.Add(startid);
            start.SetHandler((startid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/Start", new { id = startid, content = string.Empty }.ToJson()));
                PrintRequestState(res);
            }, startid);


            Command stop = new Command("stop", "停止监听") { };
            tcpforward.Add(stop);
            var stopid = new Argument<int>("id", "id");
            stop.Add(stopid);
            stop.SetHandler((stopid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/Stop", new { id = stopid, content = string.Empty }.ToJson()));
                PrintRequestState(res);
            }, stopid);


            Command add = new Command("add", "添加监听") { };
            tcpforward.Add(add);
            Option<int> addPort = new Option<int>("--port", description: "端口") { IsRequired = true };
            add.AddOption(addPort);
            Option<int> addAliveType = new Option<int>("--alive-type", description: $"连接类型，{(int)TcpForwardAliveTypes.TUNNEL}长连接，{(int)TcpForwardAliveTypes.WEB}短链接") { IsRequired = true }.FromAmong("1", "2");
            add.AddOption(addAliveType);
            add.SetHandler((addPort, addAliveType) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/AddListen", new
                {
                    id = 0,
                    content = new
                    {
                        ID = 0,
                        Port = addPort,
                        AliveType = addAliveType,
                        ForwardType = (int)TcpForwardTypes.FORWARD,
                    }.ToJson()
                }.ToJson()));
                PrintRequestState(res);
            }, addPort, addAliveType);



            Command addForward = new Command("add-forward", "添加转发") { };
            tcpforward.Add(addForward);
            Argument<int> listenid = new Argument<int>("--listenid", description: "监听id");
            addForward.AddArgument(listenid);

            Option<int> addTunnelType = new Option<int>("--tunnel-type",
                description: $"通道类型，{(int)TcpForwardTunnelTypes.TCP_FIRST}优先tcp，{(int)TcpForwardTunnelTypes.TCP}仅tcp，{(int)TcpForwardTunnelTypes.UDP_FIRST}优先udp，{(int)TcpForwardTunnelTypes.UDP}仅udp"
            )
            { IsRequired = true }.FromAmong("8", "2", "16", "4");
            addForward.AddOption(addTunnelType);

            Option<string> addName = new Option<string>("--name", description: "目标客户端名") { IsRequired = true };
            addForward.AddOption(addName);

            Option<string> addSourceIp = new Option<string>("--source-ip", description: "访问ip") { IsRequired = true };
            addForward.AddOption(addSourceIp);

            Option<string> addTarget = new Option<string>("--target", description: "目标地址，带端口，例如127.0.0.1:80") { IsRequired = true };
            addForward.AddOption(addTarget);
            addForward.SetHandler((listenid, addTunnelAliveType, addName, addSourceIp, addTarget) =>
            {
                IPEndPoint ip = IPEndPoint.Parse(addTarget);
                JsonNode res = JsonNode.Parse(Request("tcpforward/AddForward", new
                {
                    id = 0,
                    content = new
                    {
                        ListenID = listenid,
                        Forward = new
                        {
                            ID = 0,
                            Name = addName,
                            TunnelType = addTunnelAliveType,
                            SourceIp = addSourceIp,
                            TargetIp = ip.Address.ToString(),
                            TargetPort = ip.Port
                        },
                    }.ToJson()
                }.ToJson()));
                PrintRequestState(res);
            }, listenid, addTunnelType, addName, addSourceIp, addTarget);


        }

        private void HandlerList()
        {
            JsonNode res = JsonNode.Parse(Request("tcpforward/list"));

            var tunnelTypes = new Dictionary<string, string> { { "2", "tcp" }, { "4", "udp" }, { "8", "tcp first" }, { "16", "udp first" } };
            if (res.Root["Code"].GetValue<int>() == 0)
            {
                var array = res.Root["Content"].AsArray();
                foreach (var item in array)
                {
                    PrintTable(new List<List<object>> {
                            new List<object>{"id","监听端口", "监听状态", "连接类型" },
                            new List<object>{
                                item["ID"].ToString(),
                                item["Port"].ToString(),
                                item["Listening"].GetValue<bool>() ? "已监听":"--------",
                                item["AliveType"].GetValue<int>() == 1 ?"长连接":"短链接"
                            }
                        });

                    var forwards = new List<List<object>> {
                            new List<object>{"id","访问源", "通道类型", "目标" }
                        }.Concat(item["Forwards"].AsArray().Select(c => new List<object> {
                                    c["ID"].ToString() ,
                                    $"{c["SourceIp"]}:{item["Port"]}" ,
                                    tunnelTypes[c["TunnelType"].ToString()],
                                    $"[{c["Name"]}]{c["TargetIp"]}:{c["TargetPort"]}",
                                }).ToList()).ToList();
                    PrintTable(forwards);
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }

            PrintRequestState(res);
        }
    }

    [Flags]
    public enum TcpForwardTypes : byte
    {
        [Description("转发")]
        FORWARD = 1,
        [Description("代理")]
        PROXY = 2
    }
    [Flags]
    public enum TcpForwardAliveTypes : byte
    {
        [Description("长连接")]
        TUNNEL = 1,
        [Description("短连接")]
        WEB = 2
    }
    [Flags]
    public enum TcpForwardTunnelTypes : byte
    {
        [Description("只tcp")]
        TCP = 1 << 1,
        [Description("只udp")]
        UDP = 1 << 2,
        [Description("优先tcp")]
        TCP_FIRST = 1 << 3,
        [Description("优先udp")]
        UDP_FIRST = 1 << 4,
    }
}
