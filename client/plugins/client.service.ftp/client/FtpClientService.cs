using client.messengers.clients;
using client.service.ui.api.clientServer;
using common.libs.extends;
using System;
using System.Threading.Tasks;

namespace client.service.ftp.client
{
    public class FtpClientService : IClientService
    {
        private readonly FtpClient ftpClient;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly Config config;
        public FtpClientService(FtpClient ftpClient, IClientInfoCaching clientInfoCaching, Config config)
        {
            this.ftpClient = ftpClient;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
        }

        public SpecialFolderInfo LocalSpecialList(ClientServiceParamsInfo arg)
        {
            return ftpClient.GetSpecialFolders();
        }
        public object LocalList(ClientServiceParamsInfo arg)
        {
            var list = ftpClient.LocalList(arg.Content);
            return new
            {
                Current = config.ClientCurrentPath,
                Data = list
            };
        }
        public void SetLocalPath(ClientServiceParamsInfo arg)
        {
            ftpClient.SetCurrentPath(arg.Content);
        }
        public void LocalCreate(ClientServiceParamsInfo arg)
        {
            ftpClient.Create(arg.Content);
        }
        public void LocalDelete(ClientServiceParamsInfo arg)
        {
            ftpClient.Delete(arg.Content);
        }
        public async Task LocalCancel(ClientServiceParamsInfo arg)
        {
            RemoteCancelParamsInfo model = arg.Content.DeJson<RemoteCancelParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                await ftpClient.OnFileUploadCancel(new commands.FtpCancelCommand { Md5 = model.Md5 }, client).ConfigureAwait(false);
            }
        }

        public async Task<bool> RemoteCancel(ClientServiceParamsInfo arg)
        {
            RemoteCancelParamsInfo model = arg.Content.DeJson<RemoteCancelParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                var res = await ftpClient.RemoteCancel(model.Md5, client).ConfigureAwait(false);
                if (res.Code != FtpResultInfo.FtpResultCodes.OK)
                {
                    arg.SetErrorMessage(res.Code.GetDesc((byte)res.Code));
                }
            }
            return false;

        }
        public async Task<FileInfo[]> RemoteList(ClientServiceParamsInfo arg)
        {
            RemoteListParamsInfo model = arg.Content.DeJson<RemoteListParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                return await ftpClient.RemoteList(model.Path, client).ConfigureAwait(false);
            }
            return Array.Empty<FileInfo>();
        }
        public async Task<bool> Download(ClientServiceParamsInfo arg)
        {
            RemoteDownloadParamsInfo model = arg.Content.DeJson<RemoteDownloadParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                return await ftpClient.Download(model.Path, client).ConfigureAwait(false);
            }
            return false;
        }
        public async Task Upload(ClientServiceParamsInfo arg)
        {
            RemoteUploadParamsInfo model = arg.Content.DeJson<RemoteUploadParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                await ftpClient.Upload(model.Path, client, model.TargetPath).ConfigureAwait(false);
            }
        }
        public async Task<bool> RemoteDelete(ClientServiceParamsInfo arg)
        {
            RemoteDeleteParamsInfo model = arg.Content.DeJson<RemoteDeleteParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                var res = await ftpClient.RemoteDelete(model.Path, client).ConfigureAwait(false);
                if (res.Code != FtpResultInfo.FtpResultCodes.OK)
                {
                    arg.SetErrorMessage(res.Code.GetDesc((byte)res.Code));
                }
            }
            return false;
        }
        public async Task<bool> RemoteCreate(ClientServiceParamsInfo arg)
        {
            RemoteDeleteParamsInfo model = arg.Content.DeJson<RemoteDeleteParamsInfo>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                var res = await ftpClient.RemoteCreate(model.Path, client).ConfigureAwait(false);
                if (res.Code != FtpResultInfo.FtpResultCodes.OK)
                {
                    arg.SetErrorMessage(res.Code.GetDesc((byte)res.Code));
                }
            }
            return false;
        }

        public object Info(ClientServiceParamsInfo arg)
        {
            return new
            {
                Uploads = ftpClient.GetUploads(),
                Downloads = ftpClient.GetDownloads(),
            };
        }
    }

    public class RemoteListParamsInfo
    {
        public ulong Id { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    public class RemoteDeleteParamsInfo : RemoteListParamsInfo
    {
    }

    public class RemoteUploadParamsInfo : RemoteListParamsInfo
    {
        public string TargetPath { get; set; } = string.Empty;
    }

    public class RemoteDownloadParamsInfo : RemoteListParamsInfo
    {
    }
    public class RemoteCancelParamsInfo
    {
        public ulong Id { get; set; }
        public ulong Md5 { get; set; }
    }


}
