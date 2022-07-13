using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Nodes;

namespace client.service.command.commands
{
    internal class CommandClients : CommandBase
    {
        public CommandClients(RootCommand rootCommand)
        {
            Command clients = new Command("clients", "客户端相关命令") { };
            rootCommand.Add(clients);


            Command list = new Command("list", "客户端列表") { };
            list.SetHandler(HandlerList);
            clients.Add(list);


            Command connect = new Command("connect", "连接目标客户端") { };
            Argument<int> connectId = new Argument<int>("id", "目标客户端id");
            connect.Add(connectId);
            connect.SetHandler((connectId) =>
            {
                JsonNode res = JsonNode.Parse(Request("clients/connect", new { ID = connectId }.ToJson()));
                PrintRequestState(res);
            }, connectId);
            clients.Add(connect);


            Command connectReverse = new Command("connect-reverse", "目标客户端反向连接，让它连我") { };
            Argument<int> connectReverseId = new Argument<int>("id", "目标客户端id");
            connectReverse.Add(connectReverseId);
            connectReverse.SetHandler((connectReverseId) =>
            {
                JsonNode res = JsonNode.Parse(Request("clients/connectReverse", new { ID = connectReverseId }.ToJson()));
                PrintRequestState(res);
            }, connectReverseId);
            clients.Add(connectReverse);


            Command offline = new Command("offline", "离线目标客户端") { };
            Argument<int> offlineId = new Argument<int>("id", "目标客户端id");
            offline.Add(offlineId);
            offline.SetHandler((offlineId) =>
            {
                JsonNode res = JsonNode.Parse(Request("clients/offline", new { ID = offlineId }.ToJson()));
                PrintRequestState(res);
            }, offlineId);
            clients.Add(offline);


            Command reset = new Command("reset", "重置目标客户端") { };
            Argument<int> resetId = new Argument<int>("id", "目标客户端id");
            reset.Add(resetId);
            reset.SetHandler((resetId) =>
            {
                JsonNode res = JsonNode.Parse(Request("clients/reset", new { ID = resetId }.ToJson()));
                PrintRequestState(res);
            }, resetId);
            clients.Add(reset);
        }

        private void HandlerList()
        {
            var pos = Console.GetCursorPosition();
            while (true)
            {
                JsonNode res = JsonNode.Parse(Request("clients/list"));
                if (res.Root["Code"].GetValue<int>() == 0)
                {
                    var array = res.Root["Content"].AsArray();
                    if (array.Count == 0)
                    {
                        Console.WriteLine("Empty");
                        break;
                    }
                    var clients = new List<List<object>> {
                            new List<object>{"id","名称","ip","mac", "udp", "tcp" }
                        }.Concat(array.Select(c => new List<object> {
                                    c["Id"].ToString() ,
                                    c["Name"].ToString() ,
                                    c["Ip"].ToString() ,
                                    c["Mac"].ToString() ,
                                    ConnectText(c,"Udp"),
                                    ConnectText(c,"Tcp")
                                }).ToList()).ToList();

                    Console.SetCursorPosition(pos.Left, pos.Top);
                    PrintTable(clients);
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    PrintRequestState(res);
                    break;
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
        private string ConnectText(JsonNode node, string type)
        {
            if (node[$"{type}Connected"].GetValue<bool>())
            {
                return (ClientConnectTypes)node[$"{type}ConnectType"].GetValue<int>() == ClientConnectTypes.P2P ? "打洞" : "中继";
            }
            else if (node[$"{type}Connecting"].GetValue<bool>())
            {
                return "......";
            }
            return "/";
        }
    }

    [Flags]
    public enum ClientConnectTypes : byte
    {
        [Description("打洞")]
        P2P = 1 << 0,
        [Description("中继")]
        Relay = 1 << 1
    }

}
