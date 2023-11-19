using System.Diagnostics;

namespace Util
{
    public static class Logger
    {
        public static void Log(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
