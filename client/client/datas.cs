using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client
{
    public class datas
    {
        static datas _datas = null;
        public static datas Default { get { Interlocked.CompareExchange(ref _datas, new datas(), null);return _datas; } }
        public Dictionary<string, bool> canConnect = new Dictionary<string, bool>();
        public datas()
        {

        }

        public void log(string v)
        {

        }
    }
}
