using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TCPIPConnector
{
    public partial class TCPSerialService : ServiceBase
    {
        private Timer timer1 = null;
        public static TcpClient client = new TcpClient();
        public static TcpListener shut = new TcpListener(IPAddress.Parse("192.168.1.26") , 2014);
        public static SerialPort SerialPort1 = new SerialPort();
        public TCPSerialService()
        {
            InitializeComponent();
            OpenSerialPort();
        }

        protected override void OnStart(string[] args)
        {
            timer1 = new Timer();
            this.timer1.Interval = 100;
            this.timer1.Elapsed += new System.Timers.ElapsedEventHandler(this.timer1_Tick);
            timer1.Enabled = true;
            timer1.Start();
            
            TCPSettings.WriteErrorLog("Test window service started");
        }

        protected override void OnStop()
        {
            timer1.Enabled = false;
            TCPSettings.WriteErrorLog("Test window service stopped");
        }

        private void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            shut.Start();
            StartReceiving();
            Read();
        }

        //Function to receive sended data from Serial Communicator
        public void StartReceiving()
        {
            string message;
            try
            {
                if (shut.Pending())
                {
                    StringBuilder messageBuilder = new StringBuilder();
                    client = shut.AcceptTcpClient();
                    client.ReceiveBufferSize = 650000;
                    NetworkStream stream = client.GetStream();
                    using (StreamReader reader = new StreamReader(client.GetStream()))
                    {
                        reader.DiscardBufferedData();
                        while (reader.Peek() > -1)
                        {
                            messageBuilder.Append(Convert.ToChar(reader.Read()));
                        }
                        message = messageBuilder.ToString();
                        if (message.Contains("ORU^R01") == true)
                        {
                            string tempMessage = (char)11 + "MSH|^~\\&|LIS-Server|HOSPITAL BRNO|analyzer|ECL 760|" + DateTime.Now.ToString("yyyyMMddHHmmss") + "||ACK^R01|" + GetMCID(message) + "|P|2.3.1||||||ASCII|||" + (char)13 + "MSA|AA|" + GetMCID(message) + "|Message accepted|||0|ERR|0|" + (char)13 + (char)28 + (char)13;
                            byte[] response = Encoding.ASCII.GetBytes(tempMessage);
                            stream.Write(response, 0, response.Length);
                            TCPSettings.WriteErrorLog(message.Replace("\r\n", ""));
                        }
                        if (message.Contains("QRY^Q02") == true)
                        {
                            string tempMessage = (char)11 + "MSH|^~\\&|LIS|||3.1|" + DateTime.Now.ToString("yyyyMMddHHmmss") + "||ACK^Q03|" + GetMCID(message) + "|P|2.3.1||||||ASCII|||" + (char)13 + "MSA|AA|" + GetMCID(message) + "|Message accepted|||0|" + (char)13 + "QAK|SR|OK|" + (char)13 + (char)28 + (char)13;
                            byte[] response = Encoding.ASCII.GetBytes(tempMessage);
                            stream.Write(response, 0, response.Length);
                            TCPSettings.WriteErrorLog(tempMessage);
                        }
                        System.Threading.Thread.Sleep(2000);
                        SendingToSerial(message);
                        TCPSettings.WriteErrorLog(message.Replace("\r\n", ""));
                    }
                }
            }
            catch (Exception ex)
            {
                TCPSettings.WriteErrorLog(ex.ToString() + "Dito");
            }
        }
        //Function to send Data from LIS to Serial Commmunicator
        public static void StartSending(string message)
        {
            try
            {
                if (message.Contains((char)6) == false)
                {
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    client = shut.AcceptTcpClient();
                    client.ReceiveBufferSize = 650000;
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    TCPSettings.WriteErrorLog(message);
                }
                TCPSettings.WriteErrorLog(message);
            }
            catch (ArgumentNullException e)
            {
                TCPSettings.WriteErrorLog(e.Message + " ArgumentNullException");
            }
            catch (SocketException e)
            {
                TCPSettings.WriteErrorLog(e.Message + " SocketException");
            }
        }
        //Function to send Data from LIS to Serial Commmunicator
        public static void ReplyToSend(string message)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                client = shut.AcceptTcpClient();
                client.ReceiveBufferSize = 650000;
                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
            }
            catch (ArgumentNullException e)
            {
                TCPSettings.WriteErrorLog(e.Message + " ArgumentNullException");
            }
            catch (SocketException e)
            {
                TCPSettings.WriteErrorLog(e.Message + " SocketException");
            }
        }

        //Function to Receive data from LIS
        public static void Read()
        {
            try
            {
                if (SerialPort1.IsOpen)
                {
                    string message = SerialPort1.ReadExisting();
                    if (message != "")
                    {
                        TCPSettings.WriteErrorLog(message);
                        StartSending(message);
                    }
                }
            }
            catch (Exception ex)
            {
                TCPSettings.WriteErrorLog(ex.Message + " Here");
            }
        }
        //Function to send data from Serial Communicator to LIS
        public static void SendingToSerial(string message)
        {
            if (SerialPort1.IsOpen)
            {
                if (message != "")
                {
                    SerialPort1.Write((char)2 + message + (char)3);
                }
            }
        }
        public static void OpenSerialPort()
        {
            SerialPort1.PortName = "COM6";
            SerialPort1.BaudRate = 9600;
            SerialPort1.DataBits = 8;
            SerialPort1.Parity = Parity.None;
            SerialPort1.StopBits = StopBits.One;
            SerialPort1.DtrEnable = true;
            SerialPort1.RtsEnable = true; 
            SerialPort1.Handshake = Handshake.None;
            SerialPort1.ReadTimeout = 10000;
            SerialPort1.Open();
        }
        public string GetMCID(string mess)
        {
            string[] str;
            string[] strTemp;
            str = mess.Split((char)13);
            strTemp = str[0].Split((char)124);
            return strTemp[9];
        }
    }
}
