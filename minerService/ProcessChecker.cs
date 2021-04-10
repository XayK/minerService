
using System.Diagnostics;


namespace minerService
{
    class ProcessChecker
    {

        public static string PN = "nbminer";
        public static bool isThereAProccess()
        {
            Process[] listProc = Process.GetProcessesByName(PN);
            if (listProc.Length == 0)
                return false;
            else
                return true;
        }
        public static bool isUserLoged()
        {
            Process[] proc = Process.GetProcessesByName("LogonUI");
            if (proc.Length == 0)
                return false;
            else
                return true;
        }
    }
}
