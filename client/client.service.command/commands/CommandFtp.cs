using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace client.service.command.commands
{
    internal class CommandFtp : CommandBase
    {
        public CommandFtp(RootCommand rootCommand)
        {
            Command ftp = new Command("ftp", "文件传输相关命令") { };
            rootCommand.Add(ftp);


            Command list = new Command("list", "上传下载列表") { };
            list.SetHandler(HandlerList);


            Command upload = new Command("upload", "上传文件") { };
            Argument<int> connectId = new Argument<int>("id", "目标客户端id");
            upload.Add(connectId);
            Argument<string> path = new Argument<string>("path", "文件或文件夹路径");
            upload.Add(path);
            Argument<string> targetPath = new Argument<string>("path1", "上传到目标客户端目录");
            upload.Add(targetPath);
            upload.SetHandler((connectId, path, targetPath) =>
            {
                JsonNode res = JsonNode.Parse(Request("ftp/upload", new { ID = connectId, Path = path, TargetPath = targetPath }.ToJson()));
                PrintRequestState(res);
            }, connectId, path, targetPath);
            ftp.Add(upload);
        }

        private void HandlerList()
        {
            var pos = Console.GetCursorPosition();
            while (true)
            {
                JsonNode res = JsonNode.Parse(Request("ftp/info"));
                if (res.Root["Code"].GetValue<int>() == 0)
                {
                    var uploads = res.Root["Content"]["Uploads"].AsArray();
                    var downloads = res.Root["Content"]["Downloads"].AsArray();

                    int uploadLast = uploads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Wait).Count();
                    var uploadTable = new List<List<object>> {
                            new List<object>{ "文件名", "文件大小", "已上传", "速度" }
                        }.Concat(uploads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Uploading).Select(c => new List<object> {
                            Path.GetFileName(c["FullName"].ToString()),
                            SizeFormat(double.Parse(c["TotalLength"].ToString())),
                            SizeFormat(double.Parse(c["IndexLength"].ToString())),
                            SizeFormat(double.Parse(c["Speed"].ToString()))
                        })).ToList();

                    int downloadLast = downloads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Wait).Count();
                    var downloadTable = new List<List<object>> {
                            new List<object>{ "文件名", "文件大小", "已下载", "速度" }
                        }.Concat(downloads.Where(c => c["State"].GetValue<int>() == (int)UploadStates.Uploading).Select(c => new List<object> {
                            Path.GetFileName(c["FullName"].ToString()),
                            SizeFormat(double.Parse(c["TotalLength"].ToString())),
                            SizeFormat(double.Parse(c["IndexLength"].ToString())),
                            SizeFormat(double.Parse(c["Speed"].ToString()))
                        })).ToList();

                    Console.SetCursorPosition(pos.Left, pos.Top);
                    PrintTable(uploadTable);
                    Console.WriteLine($"上传等待中:{uploadLast}");
                    Console.WriteLine();
                    Console.WriteLine();
                    PrintTable(downloadTable);
                    Console.WriteLine($"下载等待中:{downloadLast}");

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

        private string[] sizeUnits = new string[] { "B", "KB", "MB", "GB", "TB" };
        private string SizeFormat(double size)
        {
            int i;
            for (i = 0; i < sizeUnits.Length; i++)
            {
                if (size < 1024)
                {
                    break;
                }
                size /= 1024;
            }
            return $"{size:N2}{sizeUnits[i]}";
        }
    }

    [Flags]
    internal enum UploadStates : byte
    {
        [Description("等待中")]
        Wait = 0,
        [Description("上传中")]
        Uploading = 1,
        [Description("已取消")]
        Canceled = 2,
        [Description("出错")]
        Error = 3
    }
}
