using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace minerService
{
    public partial class Miner_Service : ServiceBase
    {
        public Miner_Service()
        {
            InitializeComponent();

        }

        /// ////////// VARIABLES
        Thread thMiner;
        Thread thJSONsender;
        EventLog logger;
        string POOL = "eth.2miners.com:2020", USER = "0xfc6a8b9f868ec7593c7c7e5e1682f9a421025786.GPUMiner";
        /// ////////
        /// 
        protected override void OnStart(string[] args)
        {
            /////////////////Загрузка настроек
            ReadSettingAsync();
            ///////////////

            thMiner = new Thread(new ThreadStart(toDo));
            thJSONsender = new Thread(new ThreadStart(ServerForSendingData));
            /////logger///
            logger = new EventLog();
            this.AutoLog = false;
            if (!EventLog.SourceExists("IdlingMinerService"))
            {
                EventLog.CreateEventSource(
                    "IdlingMinerService", "MinerLogger");
            }
            ////////////////////
            logger.Source = "IdlingMinerService";
            logger.Log = "MinerLogger";
            logger.WriteEntry("Started miner service.", EventLogEntryType.Information);

            
            thMiner.Start();
            thJSONsender.Start();
        }

        void toDo()
        {

            logger.WriteEntry("Starting up a programm cycle.", EventLogEntryType.Information);
            while (DateTime.Now < new DateTime(2021, 04, 17))
            {
                if (!ProcessChecker.isThereAProccess() && ProcessChecker.isUserLoged())
                {
                    Process p = new Process();

                    p.StartInfo.FileName = @"D:\nb\nbminer.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.ErrorDialog = false;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    logger.WriteEntry("Setting up a miner configuration to start", EventLogEntryType.Warning);
                    p.StartInfo.Arguments = "-a ethash -o " + POOL + " -u " + USER;
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
                else if (!ProcessChecker.isUserLoged())
                {
                    while (ProcessChecker.isThereAProccess())
                    {
                        Process[] listProc = Process.GetProcesses();
                        foreach (var p in listProc)
                        {
                            if (p.ProcessName == ProcessChecker.PN)
                            {
                                try
                                {
                                    p.Kill();
                                    p.Dispose();
                                    logger.WriteEntry("Stoping Miner cause of input action.", EventLogEntryType.Warning);
                                }
                                catch
                                {
                                    logger.WriteEntry("Cannot stop a miner. Trying again.", EventLogEntryType.Error);
                                }
                            }
                        }
                    }
                }
                /* Process[] listProc1 = Process.GetProcesses();
                 string tmpstr = "";
                 foreach (var p in listProc1)
                 {
                     tmpstr += p.ProcessName+"\n";
                 }
                 logger.WriteEntry(tmpstr, EventLogEntryType.FailureAudit);*/
                Thread.Sleep(1000);
            }
            logger.WriteEntry("Trial Expired", EventLogEntryType.Error);
        }
        protected override void OnStop()
        {
            thJSONsender.Abort();
            thMiner.Abort();
            Process[] listProc = Process.GetProcesses();
            foreach (var p in listProc)
            {
                if (p.ProcessName == ProcessChecker.PN)
                {
                    p.Kill();
                    p.Dispose();
                    logger.WriteEntry("Stoping Miner on service logout.", EventLogEntryType.Warning);
                }
            }
            logger.WriteEntry("Service is shutted down.", EventLogEntryType.Warning);
        }



        ///READING SETTING FROM a JSON file      
        private async Task ReadSettingAsync()
        {
            //чтение настроек из файла
            if (File.Exists("settings.json"))
                using (FileStream fs = new FileStream("settings.json", FileMode.OpenOrCreate))
                {
                    Configuration readConf = await JsonSerializer.DeserializeAsync<Configuration>(fs);
                    USER = readConf.user;
                    POOL = readConf.pool;
                }
            else
            {                //создание нового со стандартными настройками при отсутствии
                using (FileStream fs = new FileStream("settings.json", FileMode.OpenOrCreate))
                {
                    Configuration newConf = new Configuration { pool = POOL, user = USER };
                    await JsonSerializer.SerializeAsync<Configuration>(fs, newConf);
                }
            }
        }
        float getGPUtemp()
        {
            OpenHardwareMonitor.Hardware.Computer myComputer = new OpenHardwareMonitor.Hardware.Computer();
            myComputer.GPUEnabled = true;
            myComputer.Open();

            foreach (var hardwareItem in myComputer.Hardware)
            {

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == OpenHardwareMonitor.Hardware.SensorType.Temperature)
                            return (float)sensor.Value;
                    }
            }
            return 0;
        }
        void ServerForSendingData()
        {
            int port = 8888; // порт для прослушивания подключений
            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);

                // запуск слушателя
                server.Start();


                while (true)
                {
                    // получаем входящее подключение
                    TcpClient client = server.AcceptTcpClient();
                    // получаем сетевой поток для чтения и записи
                    NetworkStream stream = client.GetStream();
                    // сообщение для отправки клиенту


                    string response = JsonSerializer.Serialize(new MinerStatus { pool = POOL, user = USER, running = ProcessChecker.isThereAProccess(), GPUtemp = getGPUtemp() });
                    // преобразуем сообщение в массив байтов
                    byte[] data = Encoding.UTF8.GetBytes(response);

                    // отправка сообщения
                    stream.Write(data, 0, data.Length);
                    //Console.WriteLine("Отправлено сообщение: {0}", response);
                    // закрываем поток
                    stream.Close();
                    // закрываем подключение
                    client.Close();

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {

                if (server != null)
                    server.Stop();
            }

        }

    }
}
