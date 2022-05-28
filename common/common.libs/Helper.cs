using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace common.libs
{
    public static class Helper
    {
        public static byte[] EmptyArray = Array.Empty<byte>();

        public static string SeparatorString = ",";
        public static char SeparatorChar = ',';

        public static string GetStackTrace()
        {
            List<string> strs = new();
            StackTrace trace = new(true);
            foreach (StackFrame frame in trace.GetFrames())
            {
                strs.Add($"文件:{frame.GetFileName()},方法:{frame.GetMethod().Name},行:{frame.GetFileLineNumber()},列:{frame.GetFileColumnNumber()}");

            }
            return string.Join("\r\n", strs);
        }
    }
}
