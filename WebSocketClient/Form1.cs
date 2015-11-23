using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClientLibrary;
using WebSocket4Net;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Security.AccessControl;

namespace ClientLibrary
{

    public partial class webSocketForm : Form
    {
        public webSocketForm()
        {
            InitializeComponent();
            textBox1.Text = "1";
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        List<SynchronousSocketClient> websocketClients = new List<SynchronousSocketClient>();
        SynchronousSocketClient ctemp;
        private void button1_Click(object sender, EventArgs e)
        {
            int threads;
            threads = Convert.ToInt32(textBox1.Text);
            var done = new CountdownEvent(1);
            ClientLibrary.webSocketForm form = new webSocketForm();
            DateTime current = DateTime.Now;

            Label failedNumber = new Label();
            LogWriter logdata = new LogWriter();
            for (int i = 0; i < threads; i++)
            {
                // RegisterWithWebSocketServer
                ctemp = new SynchronousSocketClient(i, label3, connectedLabel, errorLabel, msgText);
                websocketClients.Add(ctemp);
                ThreadPool.QueueUserWorkItem(ctemp.RegisterWithWebSocketServer, i);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            msgText.Clear();
            msgText.AppendText("Test Number of Client connection" + Environment.NewLine);
            label3.Text = " ";
            connectedLabel.Text = " ";
            errorLabel.Text = " ";
            SynchronousSocketClient._clientsClosed = 0;
            SynchronousSocketClient._clientsConnected = 0;
            SynchronousSocketClient._clientsFailed = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var item in websocketClients)
                {
                    item._websocket.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        private void webSocketForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            button2.PerformClick();
        }

        private void btnClearMessages_Click(object sender, EventArgs e)
        {
            msgText.Clear();
        }
    }


    public class SynchronousSocketClient
    {
        private static int ThreadPending;
        private static long[] time;
        ManualResetEvent _sendReceiveEvent = new ManualResetEvent(true);
        public WebSocket _websocket;
        int threadIndex;
        int Tnum;
        int connectionID;
        LogWriter writer;
        Label failedNumber;
        Label connectedLabelMsg;
        Label errorLabelMsg;
        RichTextBox _msgRoller;
        public static int _clientsClosed = 0;
        public static int _clientsConnected = 0;
        public static int _clientsFailed = 0;

        public SynchronousSocketClient(int connectionId, Label msgText, Label connectedLabel, Label errorLabel, RichTextBox msgRoller)
        {
            failedNumber = msgText;
            connectionID = connectionId;
            connectedLabelMsg = connectedLabel;
            errorLabelMsg = errorLabel;
            _msgRoller = msgRoller;
        }

        public void setThreadNum(int tNum)
        {
            Tnum = tNum;
            ThreadPending = tNum;
            time = new long[Tnum];
        }

        public void RegisterWithWebSocketServer(object connectionID)
        {
            try
            {
                writer = new LogWriter();
                StringBuilder sb = new StringBuilder();
                byte[] bytes = new byte[261120];
                byte[] bytes1 = new byte[10240];
                byte[] data = new byte[0];
                threadIndex = (int)connectionID;
                var serverUri = "ws://192.168.1.159/WebSocketTest/Register";
                _websocket = new WebSocket(serverUri);

                //Raghu: By default EnableAutoSendPing will be true
                // _websocket.EnableAutoSendPing = true;

                _websocket.Opened += new EventHandler(websocket_Opened);
                _websocket.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(websocket_Error);
                _websocket.Closed += new EventHandler(websocket_Closed);
                _websocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
                _websocket.DataReceived += new EventHandler<WebSocket4Net.DataReceivedEventArgs>(websocket_DataReceived);
                _websocket.Open();                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _clientsFailed++;

            try
            {
                errorLabelMsg.Text = String.Format("Number of Clients caused Error : {0}", _clientsFailed);
                writer.WriteToLog("Websocket Error occourred in " + connectionID + e.Exception.ToString(), connectionID);
                _msgRoller.AppendText(e.Exception + " From: " + connectionID + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        private void websocket_DataReceived(object sender, WebSocket4Net.DataReceivedEventArgs e)
        {
            writer.WriteToLog("Websocket DataReceived For " + connectionID, connectionID);
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                connectedLabelMsg.Text = String.Format("Number of Clients Connected : {0}", _clientsConnected);
                failedNumber.Text = String.Format("Number of Clients Closed : {0}", _clientsClosed);
                writer.WriteToLog("Message received From websocket server for Connection : " + connectionID, connectionID);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            _clientsClosed++;
            try
            {
                failedNumber.Text = String.Format("Number of Clients Closed : {0}", _clientsClosed);
                connectedLabelMsg.Text = String.Format("Number of Clients Connected : {0}", _clientsConnected);
                writer.WriteToLog("Connection Closed For" + connectionID, connectionID);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            _clientsConnected++;
            try
            {
                connectedLabelMsg.Text = String.Format("Number of Clients Connected : {0}", _clientsConnected);
                failedNumber.Text = String.Format("Number of Clients Closed : {0}", _clientsClosed);
                writer.WriteToLog("Message received From websocket server for Connection : " + connectionID, connectionID);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }

            _websocket.Send("WebSocket Test Data~" + connectionID);
        }
    }

    public class LogWriter
    {
        private static string logFile = "Log";
        public LogWriter() { }

        public void WriteToLog(string message, int connectionID)
        {
            string logDate = DateTime.Now.ToString("yyyy-MM-dd");
            Int32 ID = connectionID;
            string loggingPath = "c:\\LogResults\\" + logDate + "\\";

            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            string logPath = loggingPath + "Connection_" + connectionID + "_" + logFile + ".txt";
            File.AppendAllText(logPath, DateTime.Now + message + Environment.NewLine);

        }
        public string GetPath()
        {
            string logDate = DateTime.Now.ToString("yyyy-MM-dd");
            string loggingPath = "c:\\LogResults\\" + logDate + "\\";

            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            string logPath = loggingPath + "_" + "NumberOfConnectedClients.txt";
            return logPath;
        }

        public void WriteNumberOfConnection(int numberOfConnectedClients)
        {
            File.AppendAllText("c:\\LogResults\\NumberofCount.txt", numberOfConnectedClients.ToString() + Environment.NewLine);
        }
    }


}


