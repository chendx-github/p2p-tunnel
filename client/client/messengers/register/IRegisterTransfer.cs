using common.server.model;
using System.Threading.Tasks;

namespace client.messengers.register
{
    public interface IRegisterTransfer
    {
        Task AutoReg();
        Task Exit();
        Task<CommonTaskResponseInfo<bool>> Register();
    }
}
