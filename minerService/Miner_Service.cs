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
            starterAsync();
            
        }
        async void starterAsync()
        {
            thJSONsender.Start();
            thMiner.Start();
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

        void ServerForSendingData()
        {
                int port = 8005; // порт для приема входящих запросов   
                // получаем адреса для запуска сокета
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

                // создаем сокет
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    // связываем сокет с локальной точкой, по которой будем принимать данные
                    listenSocket.Bind(ipPoint);

                    // начинаем прослушивание
                    listenSocket.Listen(10);

                    //Console.WriteLine("Сервер запущен. Ожидание подключений...");

                    while (true)
                    {
                        Socket handler = listenSocket.Accept();
                        // получаем сообщение
                        StringBuilder builder = new StringBuilder();
                        int bytes = 0; // количество полученных байтов
                        byte[] data = new byte[256]; // буфер для получаемых данных

                        do
                        {
                            bytes = handler.Receive(data);
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        while (handler.Available > 0);

                        if (builder.ToString() == "GetData")
                        {
                            // отправляем ответ
                            MinerStatus ms = new MinerStatus { pool = POOL, user = USER, running = ProcessChecker.isThereAProccess(), GPUtemp = 30 };
                            string message = JsonSerializer.Serialize(ms);
                            data = Encoding.Unicode.GetBytes(message);
                            handler.Send(data);
                        }
                        // закрываем сокет
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            
        }

    }
}
