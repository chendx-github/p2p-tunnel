using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace client.service.command.commands
{
    internal class CommandTcpforward : CommandBase
    {
        public CommandTcpforward(RootCommand rootCommand)
        {
            Command tcpforward = new Command("tcpforward", "注册相关命令") { };
            rootCommand.Add(tcpforward);

            Command list = new Command("list", "tcp转发列表") { };
            list.SetHandler(HandlerList);
            tcpforward.Add(list);


            Command del = new Command("del", "删除端口监听") { };
            var idArg = new Argument<int>("id", "监听的id");
            del.Add(idArg);
            del.SetHandler((idArg) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/RemoveListen", new { id = idArg, content = string.Empty }.ToJson()));
                PrintRequestState(res);
            }, idArg);
            tcpforward.Add(del);

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
}
