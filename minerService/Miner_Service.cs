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
        Thread thBroadcast;
        EventLog logger;

        Process nbProcess;
        string POOL = "eth.2miners.com:2020", USER = "0xfc6a8b9f868ec7593c7c7e5e1682f9a421025786.GPUMiner";
        int Runes = 1;
        /// ////////
        /// ЗАПУСК
        protected override void OnStart(string[] args)
        {
            Runes = 1;
            /////////////////Загрузка настроек
            ReadSettingAsync();
            ///////////////

            thMiner = new Thread(new ThreadStart(toDo));
            thJSONsender = new Thread(new ThreadStart(ServerForSendingData));
            thBroadcast = new Thread(new ThreadStart(GettingBroadcastMessage));
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

            nbProcess = new Process();
            nbProcess.StartInfo.FileName = @"С:\nb\nbminer.exe";
            nbProcess.StartInfo.UseShellExecute = false;
            nbProcess.StartInfo.RedirectStandardError = true;
            nbProcess.StartInfo.RedirectStandardInput = true;
            nbProcess.StartInfo.RedirectStandardOutput = true;
            nbProcess.StartInfo.CreateNoWindow = true;

            thMiner.Start();
            thJSONsender.Start();
            thBroadcast.Start();
        }
        /// <summary>
        /// ////ОСТАНОВКА
        /// </summary>
        protected override void OnStop()
        {
            Interlocked.Exchange(ref Runes, 0);
            //Runes = 0;
            try
            {
                while (thJSONsender.IsAlive)
                {
                    logger.WriteEntry("Closing #1 listener.", EventLogEntryType.Warning);
                    thJSONsender.Abort();
                    
                    Thread.Sleep(1000);
                }
                while (thBroadcast.IsAlive)
                {
                    logger.WriteEntry("Closing #2 listener.", EventLogEntryType.Warning);
                    thBroadcast.Abort();
                    
                    Thread.Sleep(1000);
                }
                thMiner.Abort();
            }
            catch(Exception ex)
            {
                logger.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
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

        /// <summary>
        /// ///////////
        /// ////////////
        /// </summary>
        protected void toDo()
        {
            logger.WriteEntry("Starting up a programm cycle.", EventLogEntryType.Information);
            while (DateTime.Now < new DateTime(2021, 04, 17))
            {
                if (!ProcessChecker.isThereAProccess() && ProcessChecker.isUserLoged())
                {
                    //Overclocing();
                    nbProcess = new Process();

                    
                    //p.StartInfo.ErrorDialog = false;
                    //p.PriorityClass = ProcessPriorityClass.High;
                    nbProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    logger.WriteEntry("Setting up a miner configuration to start", EventLogEntryType.Warning);
                    nbProcess.StartInfo.Arguments = "-a ethash -o " + POOL + " -u " + USER;
                    try
                    {
                        nbProcess.Start();
                        ProcessChecker.PN = nbProcess.ProcessName;
                        logger.WriteEntry("Succesfully started procces of mining.", EventLogEntryType.Information);
                    }
                    catch(Exception ex)
                    {
                        logger.WriteEntry(ex.Message, EventLogEntryType.Error);
                    }

                }
                else if (!ProcessChecker.isUserLoged())
                {
                    //DeOverclocing();
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
                                    logger.WriteEntry("Stoping Miner cause of login action.", EventLogEntryType.Warning);
                                    Thread.Sleep(1000);
                                }
                                catch
                                {
                                    logger.WriteEntry("Cannot stop a miner. Trying again.", EventLogEntryType.Error);
                                    Thread.Sleep(1000);
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
            int port = 9078; // порт для прослушивания подключений
            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);

                // запуск слушателя
                server.Start();

               

                while (Runes==1)
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
                logger.WriteEntry(Runes.ToString(), EventLogEntryType.Error);

            }
            catch (Exception e)
            {
                logger.WriteEntry(e.Message, EventLogEntryType.Error);
            }
            finally
            {

                if (server != null)
                    server.Stop();
            }

        }
        string IPadressofWatcher = "";
        void GettingBroadcastMessage()
        {
            IPAddress remoteAddress = IPAddress.Parse("224.0.0.10");
            UdpClient receiver = new UdpClient(9077); // UdpClient для получения данных
            receiver.JoinMulticastGroup(remoteAddress, 20);
            IPEndPoint remoteIp = null;
            string localAddress = LocalIPAddress();
            try
            {
                while (Runes==1)
                {
                    byte[] data = receiver.Receive(ref remoteIp); // получаем данные
                    if (remoteIp.Address.ToString().Equals(localAddress))
                        continue;
                    string message = Encoding.Unicode.GetString(data);
                    IPadressofWatcher = message;
                   

                    SendMyIptoWatcher();
                }
            }
            catch (Exception ex)
            {
                logger.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
            finally
            {
                receiver.Close();
            }
        }
        private static string LocalIPAddress()
        {
            string localIP = "";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
        private void SendMyIptoWatcher()
        {
            UdpClient sender = new UdpClient(); // создаем UdpClient для отправки сообщений
            try
            {
                //while (true)
                {
                    string message = LocalIPAddress(); // сообщение для отправки
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    sender.Send(data, data.Length, IPadressofWatcher, 9077); // отправка
                }
            }
            catch (Exception ex)
            {
                logger.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
            finally
            {
                sender.Close();
            }
        }
        private void Overclocing()
        {

            MSI.Afterburner.ControlMemory mc = new MSI.Afterburner.ControlMemory();
            try
            {
                mc.GpuEntries[0].CoreClockCur = UInt32.Parse((double.Parse(mc.GpuEntries[0].CoreClockDef.ToString()) * 0.85).ToString());
                logger.WriteEntry("Succesfully downgraded clock to 85%", EventLogEntryType.Information);
            }
            catch (Exception e)
            {

                logger.WriteEntry(e.Message, EventLogEntryType.Information);
            }
            mc.Disconnect();

        }
        private void DeOverclocing()
        {

            MSI.Afterburner.ControlMemory mc = new MSI.Afterburner.ControlMemory();
            try
            {
                mc.GpuEntries[0].ResetToDefaults();
                logger.WriteEntry("Succesfully setted default clock", EventLogEntryType.Information);
            }
            catch (Exception e)
            {

                logger.WriteEntry(e.Message, EventLogEntryType.Information);
            }
            mc.Disconnect();

        }
    }
}
