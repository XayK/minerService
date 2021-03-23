
using System.Diagnostics;


namespace minerService
{
    class ProcessChecker
    {
        public static bool isThereAProccess()
        {
            Process[] listProc = Process.GetProcesses();
            foreach (var p in listProc)
            {
                if (p.ProcessName == PN)
                {
                    return true;
                }
            }
            return false;
        }
        public static string PN = "none";
    }
}
