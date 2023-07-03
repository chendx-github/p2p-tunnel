using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 
        /// </summary>
        public static byte[] EmptyArray = Array.Empty<byte>();
        /// <summary>
        /// 
        /// </summary>
        public static byte[] TrueArray = new byte[] { 1 };
        /// <summary>
        /// 
        /// </summary>
        public static byte[] FalseArray = new byte[] { 0 };
        /// <summary>
        /// 
        /// </summary>
        public static byte[] AnyIpArray = IPAddress.Any.GetAddressBytes();
        /// <summary>
        /// 
        /// </summary>
        public static byte[] AnyIpv6Array = IPAddress.IPv6Any.GetAddressBytes();
        /// <summary>
        /// 
        /// </summary>
        public static byte[] AnyPoryArray = new byte[] { 0, 0 };


        /// <summary>
        /// 
        /// </summary>
        public static string SeparatorString = ",";
        /// <summary>
        /// 
        /// </summary>
        public static char SeparatorChar = ',';
        /// <summary>
        /// 
        /// </summary>
        public static int Version = 1;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetStackTraceModelName()
        {
            string result = "";
            var stacktrace = new StackTrace();
            for (var i = 0; i < stacktrace.FrameCount; i++)
            {
                var method = stacktrace.GetFrame(i).GetMethod();
                result += (stacktrace.GetFrame(i).GetFileName() + "->" + method.Name + "\n");
            }
            return result;
        }

        public static async Task Await()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { cancellationTokenSource.Cancel(); };
            Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();
            await Task.Delay(-1,cancellationTokenSource.Token);
        }
    }
}
