using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace minerService
{
    public partial class Miner_Service : ServiceBase
    {
        public Miner_Service()
        {
            InitializeComponent();
            
        }
        Thread th;
        EventLog logger;
        protected override void OnStart(string[] args)
        {
           th = new Thread(toDo);
            /////logger///
           logger = new EventLog();
           this.AutoLog = false;
           if (!EventLog.SourceExists("SourceOfLogger"))
           {
               EventLog.CreateEventSource(
                   "SourceOfLogger", "MinerLogger");
           }
            ////////////////////
            logger.Source = "SourceOfLogger";
            logger.Log = "MinerLogger";
            logger.WriteEntry("Started miner service.", EventLogEntryType.Information);
            th.Start();
        }
        void toDo()
        {

            logger.WriteEntry("Starting up a programm cycle.", EventLogEntryType.Information);
            while (DateTime.Now < new DateTime(2021, 04, 01))
            {
                if (GetLastUserInput.GetIdleTickCount() / TimeSpan.TicksPerMillisecond >= 1 && !ProcessChecker.isThereAProccess())
                {
                    Process p = new Process();
                    //p.StartInfo.FileName = @"D:\nb\start_service.bat";
                    p.StartInfo.FileName = @"D:\nb\nbminer.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.ErrorDialog = false;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    logger.WriteEntry("Setting up a miner configuration to start", EventLogEntryType.Information);
                    p.StartInfo.Arguments = "-a ethash -o ethproxy+tcp://asia1.ethermine.org:4444 -u 0x12343bdgf.worker";
                    try
                    {
                        
                        p.Start();
                        ProcessChecker.PN = p.ProcessName;
                        logger.WriteEntry("Succesfully started procces of mining.", EventLogEntryType.Information);
                    }
                    catch
                    {
                        logger.WriteEntry("Cannot start a mining proccess. No access to miner executable!", EventLogEntryType.Error);
                    }
                   
                }
                else if (ProcessChecker.isThereAProccess() && GetLastUserInput.GetIdleTickCount() / TimeSpan.TicksPerMillisecond < 1)
                {
                    Process[] listProc = Process.GetProcesses();
                    foreach (var p in listProc)
                    {
                        if (p.ProcessName == ProcessChecker.PN)
                        {
                            p.Kill();
                            logger.WriteEntry("Stoping Miner cause of input action.", EventLogEntryType.Warning);
                        }
                    }
                }

            }
            logger.WriteEntry("Trial Expired", EventLogEntryType.Error);
        }
        protected override void OnStop()
        {
            th.Abort();
            Process[] listProc = Process.GetProcesses();
            foreach (var p in listProc)
            {
                if (p.ProcessName == ProcessChecker.PN)
                {
                    p.Kill();
                    logger.WriteEntry("Stoping Miner on service logout.", EventLogEntryType.Warning);
                }
            }
        }
    }
}
