using common.server.model;
using System.Threading.Tasks;

namespace client.messengers.register
{
    public interface IRegisterTransfer
    {
        void AutoReg();
        void Exit();
        Task<CommonTaskResponseInfo<bool>> Register();
    }
}
