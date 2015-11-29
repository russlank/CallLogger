using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Threading;
using MySql.Data.MySqlClient;

namespace CallLogger
{
    public enum ListenerType: byte
    {
        UNDEFINED = 0, 
        COMM = 1, 
        TCP = 2, 
        UDP = 3
    }

    public struct ListenerSettingsForSerialPort
    {
        public string PortName; // COM1, COM2, COM3, ...
        public int BaudRate; 
        public System.IO.Ports.Parity Parity;
        public int DataBits;
        public System.IO.Ports.StopBits StopBits;
    }

    public struct ListenerSettingsForTCPPort
    {
        public System.Net.IPAddress Address; // Listinning interface IP address
        public ushort Port; // TCP ports number to listen to
    }

    public struct ListenerSettingsUDPPort
    {
        public System.Net.IPAddress Address;  // Listinning interface IP address
        public ushort Port; // UDP ports number to listen to
    }

    // 'ListenerInstanceSettings' class object holds the settings of the 'LogListener' class instance
    // Actually this way is a bad OO design, since we can make it associated with the 'LogListener' class type, but for now lets keep it as is
    
    public class ListenerInstanceSettings
    {
        private string m_StationId;
        private ListenerType m_ListenerType; // Determines the type of 'LogListener' whether it is 'LogListenerForSerialPort', 'LogListenerForTCPPort' or 'LogListenerForUDPPort'
        private ListenerSettingsForSerialPort m_SerialPortSettings; // Settings for the 'LogListenerForSerialPort' instance of listener
        private ListenerSettingsForTCPPort m_TCPPortSettings; // Settings for the 'LogListenerForTCPPort' instance of listener
        private ListenerSettingsUDPPort m_UDPPortSettings; // Settings for the 'LogListenerForUDPPort' instance of listener
        
        public ListenerInstanceSettings()
        {
            this.m_StationId = "default";
            this.m_ListenerType = ListenerType.UNDEFINED; // Set the default settings to be for unknown yet listiner

            // Fill with default settings of 'LogListenerForSerialPort' object
            this.m_SerialPortSettings.PortName = "COM1";
            this.m_SerialPortSettings.BaudRate = 9600;
            this.m_SerialPortSettings.DataBits = 8;
            this.m_SerialPortSettings.Parity = System.IO.Ports.Parity.None;
            this.m_SerialPortSettings.StopBits = System.IO.Ports.StopBits.One;

            // Fill with default settings of 'LogListenerForTCPPort' object
            this.m_TCPPortSettings.Address = System.Net.IPAddress.Parse("0.0.0.0");
            this.m_TCPPortSettings.Port = 2020;

            //  Fill with default settings of 'LogListenerForUDPPort' object 
            this.m_UDPPortSettings.Address = System.Net.IPAddress.Parse("0.0.0.0");
            this.m_UDPPortSettings.Port = 2020;
        }

        public string StationId
        {
            get
            {
                return (this.m_StationId);
            }

            set
            {
                this.m_StationId = value;
            }
        }
        
        public ListenerType Type
        {
            get
            {
                return (this.m_ListenerType);
            }

            set
            {
                this.m_ListenerType = value;
            }
        }

        public ListenerSettingsForSerialPort SerialPortSettings
        {
            get
            {
                return (this.m_SerialPortSettings);
            }

            set
            {
                this.m_SerialPortSettings = value;
            }
        }

        public ListenerSettingsForTCPPort TCPPortSettings
        {
            get
            {
                return (this.m_TCPPortSettings);
            }
        }

        public ListenerSettingsUDPPort UDPPortSettings
        {
            get
            {
                return (this.m_UDPPortSettings);
            }
        }
    }

    // 'LogListenerContainerConfiguration' class holds the settings of the complete 'LogListenerContainer' that act as a core of the service
    public class LogListenerContainerConfiguration
    {
        // Log file is a text file that will hold the calls log that are not posted to the database due to some problem
        // Actually, it might be better to have the file in some kind of CSV or XML format, so it can be passed to the DB later more easily
        // The file name is supposed to have some tags to be replaced by final string values, like date and time of the log, so the han be handled more easily

        private string m_LogFilePath; // Log file place on the machine
        private string m_LogFileName; // Log file name on the machine
        private string m_LogDBHost; // Database host IP address of hostname
        private string m_LogDBName; // Database name on the server
        private string m_LogDBUser; // The username to be used to logon to the databes
        private string m_LogDBPassword; // The password to be used to logon to the databes
        private string m_LogDBTable; // The database table name that will hold the calls log
        private ArrayList m_ListenerInstances; // The list of 'ListenerInstanceSettings' objects
        
        public LogListenerContainerConfiguration()
        {
            m_LogFilePath = "C:\\";
            m_LogFileName = "calls-log.txt";
            m_LogDBHost = "192.168.1.10";
            m_LogDBName = "billing";
            m_LogDBUser = "billing";
            m_LogDBPassword = "billing";
            m_LogDBTable = "calls";
            m_ListenerInstances = new ArrayList();
        }

        public string DBConnectionString
        {
            get
            {
                return (String.Format("server={0};user id={1};password={2}; database={3};pooling=true;Connect Timeout=30",
                this.m_LogDBHost, this.m_LogDBUser, this.m_LogDBPassword, this.m_LogDBName));
            }
        }

        public string LogDBTable
        {
            get
            {
                return (this.m_LogDBTable);
            }
        }

        public string LogFile
        {
            get
            {
                return (this.m_LogFilePath + this.m_LogFileName);
            }
        }

        public void AddListenerSettings( ListenerInstanceSettings aSettings)
        {
            if (aSettings != null) this.m_ListenerInstances.Add( aSettings);
        }

        public ArrayList ListenerInstancesSettings 
        {
            get
            {
                return this.m_ListenerInstances;
            }

        }

        public void LoadConfiguration()
        {
            // Here, we need to determine the location of the configuration XML file, from Windows 'Registry', and apply the settings stored in it to the values
            // For now, we will leave the settings to their default values specified in the class constructor
            //
            //m_LogFilePath = "C:\\";
            //m_LogFileName = "calls-log.txt";
            //m_LogDBHost = "192.168.1.10";
            //m_LogDBName = "billing";
            //m_LogDBUser = "billing";
            //m_LogDBPassword = "billing";
            //m_LogDBTable = "calls";
            //

            // Here, for the test purpose, we will specify only one listener of the type 'COMM'
            for (int i = 0; i < 3; i++)
            {
                ListenerSettingsForSerialPort sps;
                ListenerInstanceSettings lts = new ListenerInstanceSettings();
                lts.Type = ListenerType.COMM;
                lts.StationId = "INSTANCE" + i.ToString();

                sps = lts.SerialPortSettings;
                sps.PortName = "COM1" + i.ToString();
                lts.SerialPortSettings = sps;

                this.AddListenerSettings(lts);
            }
        }
    }

    // 'LogEntry' represent the class for the log entries

    public class LogEntry
    {
        private string m_StationId;
        private string m_FirstParty;
        private string m_SecondParty;
        private int m_Duration;
        DateTime m_Start;
        DateTime m_Stop;
                
        public LogEntry()
        {
        }

        public string StationId
        {
            set
            {
                this.m_StationId = value;
            }

            get
            {
                return (this.m_StationId);
            }
        }

        public string FirstParty
        {
            set
            {
                this.m_FirstParty = value;
            }

            get
            {
                return (this.m_FirstParty);
            }
        }

        public string SecondParty
        {
            set
            {
                this.m_SecondParty = value;
            }

            get
            {
                return (this.m_SecondParty);
            }
        }

        public int Duration
        {
            set
            {
                this.m_Duration = value;
            }

            get
            {
                return (this.m_Duration);
            }
        }

        public DateTime Start
        {
            set
            {
                this.m_Start = value;
            }

            get
            {
                return (this.m_Start);
            }
        }

        public string StartAsString
        {
            get
            {
                return (this.m_Start.ToString("yyyy-MM-dd hh:mm:ss"));
            }
        }

        public DateTime Stop
        {
            set
            {
                this.m_Stop = value;
            }

            get
            {
                return (this.m_Stop);
            }
        }

        public string StopAsString
        {
            get
            {
                return (this.m_Stop.ToString("yyyy-MM-dd hh:mm:ss"));
            }
        }

        override public string ToString()
        {
            string Str = this.StationId + ";" + this.FirstParty + ";" + this.SecondParty + ";" + this.StartAsString + ";" + this.StopAsString;
            return Str;
        }
    }

    public delegate void OnNewLogEntry(object aSender, LogEntry aEntry); 
    public delegate void OnLogListenerEvent(object aSender);
       
    public abstract class LogListener
    {
        public OnNewLogEntry OnNewEntry;
        public OnLogListenerEvent OnStart;
        public OnLogListenerEvent OnStop;
        private Thread m_ListenerThead;
        private bool m_StopRequest;
        private string m_StationId;

        public LogListener()
        {
            m_StopRequest = false;
            this.m_ListenerThead = new Thread(new ThreadStart(this.Execute));

            OnNewEntry = null;
            OnStart = null;
            OnStop = null;

            m_StationId = "";
        }

        public string StationId
        {
            set
            {
                lock(this) this.m_StationId = value;
            }

            get
            {
                string Id;
                
                lock(this) Id = this.m_StationId;

                return Id;
            }
        }

        public void StartListening()
        {
            m_ListenerThead.Name = this.m_StationId;
            m_ListenerThead.Start();
        }

        public void StopListening()
        {
            lock (this) m_StopRequest = true;
        }

        public bool StopRequest
        {
            get
            {
                bool result;

                lock (this) result = this.m_StopRequest;

                return result;
            }
        }

        public abstract void ExecuteLoop();

        public void Execute()
        {
            if (this.OnStart != null)
            {
                lock (this) this.OnStart(this);
            }

            lock (this) m_StopRequest = false;
            
            ExecuteLoop();

            if (this.OnStop != null)
            {
                lock (this) this.OnStop(this);
            }
        }

        public void NewLogEntry(object aSender, LogEntry aEntry)
        {
            if ((aEntry != null) && (OnNewEntry != null))
            {
                OnNewEntry(this, aEntry);
            }
        }
    }

    public class LogListenerForSerialPort : LogListener
    {
        private const int READ_BUFFER_SIZE = 2048;
        private ListenerSettingsForSerialPort m_PortSettings;
        private System.IO.Ports.SerialPort m_Port;
        private int m_NextEvenetTime; // Just for test
        private Random m_Random; // Just for test
        
        public LogListenerForSerialPort(ListenerSettingsForSerialPort aPortSettings)
        {
            this.m_PortSettings = aPortSettings;

            m_Port = new System.IO.Ports.SerialPort(this.m_PortSettings.PortName, this.m_PortSettings.BaudRate, this.m_PortSettings.Parity, this.m_PortSettings.DataBits, this.m_PortSettings.StopBits);

            // Just for test
            m_Random = new Random(Environment.TickCount);
            m_NextEvenetTime = Environment.TickCount + 3000 + (m_Random.Next(10000));
            Thread.Sleep(100);
        }

        public override void ExecuteLoop()
        {
            this.m_Port.Open();

            while (true)
            {
                int BytesToRead = this.m_Port.BytesToRead;

                if (BytesToRead > 0)
                {
                    //byte[] Buffer = new byte[BytesToRead];
                    char[] Buffer = new char[BytesToRead];

                    this.m_Port.Read(Buffer, 0, BytesToRead);
                    {
                        // pass the Buffer[i] to the syntax analyser
                        
                        // Just for test
                        if (this.OnNewEntry != null)
                        {
                            LogEntry newLogEntry = new LogEntry();
                            string Data = "";

                            for (int i = 0; i < Buffer.Length; i++)
                            {
                                Data = Data + Buffer[i].ToString();
                            }

                            this.m_Port.Write(Data);
                            
                            newLogEntry.StationId = this.StationId;
                            newLogEntry.Start = DateTime.Now;
                            newLogEntry.Duration = m_Random.Next(600);
                            newLogEntry.Stop = DateTime.Now;
                            newLogEntry.FirstParty = Data;
                            newLogEntry.SecondParty = Data;

                            this.OnNewEntry(this, newLogEntry);
                        }

                    }

                }

                // Just for test

                /*
                        if (this.OnNewEntry != null)
                        {
                            if (this.m_NextEvenetTime <= Environment.TickCount)
                            {
                                int NextEvenetTime = Environment.TickCount + (m_Random.Next(5000));
                                LogEntry newLogEntry = new LogEntry();

                                newLogEntry.StationId = this.StationId;
                                newLogEntry.Start = DateTime.Now;
                                newLogEntry.Duration = m_Random.Next(600);
                                newLogEntry.Stop = DateTime.Now;
                                newLogEntry.FirstParty = this.m_NextEvenetTime.ToString();
                                newLogEntry.SecondParty = NextEvenetTime.ToString();

                                this.m_NextEvenetTime = NextEvenetTime;

                                this.OnNewEntry(this, newLogEntry);
                            }

                        }
                */

                Thread.Sleep(100);
                if (this.StopRequest) 
                    break;
            }

            this.m_Port.Close();
        }

    }

    public class LogListenerForTCPPort : LogListener
    {
        private ListenerSettingsForTCPPort m_PortSettings;

        public LogListenerForTCPPort(ListenerSettingsForTCPPort aPortSettings)
        {
            this.m_PortSettings = aPortSettings;
        }

        public override void ExecuteLoop()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (this.StopRequest) break;
            }
        }
    }

    public class LogListenerForUDPPort : LogListener
    {
        private ListenerSettingsUDPPort m_PortSettings;

        public LogListenerForUDPPort(ListenerSettingsUDPPort aPortSettings)
        {
            this.m_PortSettings = aPortSettings;
        }

        public override void ExecuteLoop()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (this.StopRequest) break;
            }
        }
    }

    public delegate void OnNewEvent(object aSender);

    public class LogListenersContainer
    {
        public OnNewLogEntry OnNewEntry;
        private LogListenerContainerConfiguration m_Configuration;
        private ArrayList m_LogListeners;
        private ArrayList m_Log;
        //private string m_ConnectionString;
        private MySqlConnection m_Connection;
        private Thread m_ContainerThread;
        private bool m_StopRequest;
        private bool m_IsRunning;
        private int m_NextLogFlushTick;

        public LogListenersContainer()
        {
            this.OnNewEntry = null;
            
            this.m_Configuration = new LogListenerContainerConfiguration();

            this.m_LogListeners = new ArrayList();
            this.m_Log = new ArrayList();

            this.m_Connection = null;
            //m_ConnectionString = String.Format("server={0};user id={1};password={2}; database={3};pooling=true;ConnectTimeout=30","192.168.1.10", "billing", "billing", "billing");

            this.m_Configuration.LoadConfiguration();

            this.m_StopRequest = false;
            this.m_IsRunning = false;

            m_ContainerThread = new Thread(new ThreadStart(this.Execute));
            m_NextLogFlushTick = Environment.TickCount + 5000;
        }

        public bool IsRunning
        {
            get
            {
                bool Value;
                lock (this) Value = this.m_IsRunning;
                return (Value);
            }

            private set
            {
                lock (this) this.m_IsRunning = value;
            }
        }



        private void OnLogListenerStart(object aSender)
        {
            //System.Windows.Forms.MessageBox.Show("OnLogListenerStart!");
        }

        private void OnLogListenerStop(object aSender)
        {
            //System.Windows.Forms.MessageBox.Show("OnLogListenerStop!");
            if (aSender != null)
            {
                bool LastListenerRemoved = false;

                //lock (this.m_LogListeners)
                lock (this)
                {
                    this.m_LogListeners.Remove(aSender);
                    if (this.m_LogListeners.Count <= 0) LastListenerRemoved = true;
                }

                if (LastListenerRemoved) this.ReleaseDBConnection();
            }
        }

        private void OnNewLogEntry(object aSender, LogEntry aEntry)
        {
            if (aEntry != null)
            {
                lock (m_Log)
                {
                    m_Log.Add(aEntry);
                }

                if (this.OnNewEntry != null) this.OnNewEntry(this, aEntry);
            }
        }

        private void InvokeLogListener( LogListener aListener, bool aStart)
        {
            if (aListener != null)
            {
                aListener.OnStart = new OnLogListenerEvent(this.OnLogListenerStart);
                aListener.OnStop = new OnLogListenerEvent(this.OnLogListenerStop);
                aListener.OnNewEntry = new OnNewLogEntry(this.OnNewLogEntry);

                //lock (this.m_LogListeners)
                lock (this)
                {
                    this.m_LogListeners.Add(aListener);
                }

                if (aStart) aListener.StartListening();
            }
        }

        private bool InitDBConnection()
        {
            bool Result = false;

            if (this.m_Connection == null)
            {
                try
                {
                    this.m_Connection = new MySqlConnection(this.m_Configuration.DBConnectionString);
                    this.m_Connection.Open();
                }
                catch (MySqlException ex)
                {
                    System.Windows.Forms.MessageBox.Show("Error connecting to database server: " + ex.Message);
                }
            }

            if (this.m_Connection != null)
            {
                try
                {
                    Result = this.m_Connection.Ping();
                    if (!Result)
                    {
                        this.m_Connection.Open();
                        Result = true;
                    }
                }
                catch (MySqlException ex)
                {
                    System.Windows.Forms.MessageBox.Show("Error in database connetion: " + ex.Message);
                }
            }          
            
            return Result;
        }

        private void ReleaseDBConnection()
        {
            if (this.m_Connection != null)
            {
                this.m_Connection.Dispose();
                this.m_Connection = null;
            }
        }
        
        private void FlushLog()
        {
            ArrayList Log = null;

            lock (this)
            {
                int LogEntriesCount;

                LogEntriesCount = m_Log.Count;

                if (LogEntriesCount > 0)
                {
                    Log = m_Log;
                    m_Log = new ArrayList();
                }
            }

            if (Log != null)
            {
                if (InitDBConnection())
                {
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = this.m_Connection;

                    foreach (object item in Log)
                    {
                        command.CommandText = String.Format("INSERT INTO {0} ( station, party_a, party_b, start_time, duration, direction) VALUES ('{1}','{2}','{3}','{4}','{5}','{6}')",
                            m_Configuration.LogDBTable, (item as LogEntry).StationId, (item as LogEntry).FirstParty, (item as LogEntry).SecondParty, (item as LogEntry).StartAsString, (item as LogEntry).Duration, '1');

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (MySqlException ex)
                        {
                            System.Windows.Forms.
                                MessageBox.Show("Error in inserting new records: " + ex.Message);
                        }
                    }

                }
                else
                {

                }

                Log.Clear();
            }
        }

        public void CreateListeners()
        {
            if (this.m_Configuration != null)
            {
                ArrayList ls = this.m_Configuration.ListenerInstancesSettings;

                if (ls != null)
                {
                    foreach (ListenerInstanceSettings settings in ls)
                    {
                        LogListener listener = null;
                        switch (settings.Type)
                        {
                            case ListenerType.COMM:
                                listener = new LogListenerForSerialPort(settings.SerialPortSettings);
                                break;
                            case ListenerType.TCP:
                                break;
                            case ListenerType.UDP:
                                break;
                            default:
                                break;
                        }

                        if (listener != null)
                        {
                            listener.StationId = settings.StationId;
                            this.InvokeLogListener(listener, true);
                        }
                    }
                }

            }
        }
        
        public void StopAllListeners(bool aWaitListenerToFinish)
        {
            //lock (this.m_LogListeners)
            lock (this)
			{
                foreach (LogListener logListener in this.m_LogListeners)
                    logListener.StopListening();
			}

            if (aWaitListenerToFinish)
            {
                //for (int i = 1; i < 30; i++)
                while (true)
                {
                    bool Finished = false;

                    Thread.Sleep(1000);

                    //lock (this.m_LogListeners)
                    lock (this)
                    {
                        if (this.m_LogListeners.Count <= 0) 
                            Finished = true;
                    }

                    if (Finished) break;
                }
            }

            //lock (this.m_LogListeners)
            lock (this)
            {
                this.m_LogListeners.Clear();
            }
        }

        public bool StopRequest
        {
            get
            {
                bool result;

                lock (this) result = this.m_StopRequest;

                return result;
            }
        }

        public void Execute()
        {
            this.IsRunning = true;

            this.CreateListeners();
            
            while (true)
            {

                Thread.Sleep(1000);

                {
                    int TickCount = Environment.TickCount;
                    if (TickCount >= this.m_NextLogFlushTick)
                    {
                        this.m_NextLogFlushTick = TickCount + 5000;
                        this.FlushLog();
                    }
                }


                if (this.StopRequest) break;
            }
            
            this.StopAllListeners(true);

            this.IsRunning = false;
        }

        public void Start()
        {
            this.m_ContainerThread.Start();
        }

        public void Stop( bool aWaitToFinish)
        {
            lock (this) m_StopRequest = true;

            if (aWaitToFinish)
            {
                while (this.IsRunning) Thread.Sleep(1000);             
            }
        }
    }
}