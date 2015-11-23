using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace WebSocketServer
{
    public class TestWebsocketHandler : HttpTaskAsyncHandler
    {
        public override bool IsReusable
        {
            get
            {
                return false;
            }
        }
        static int _count = 0;
        int _clientId = -1;
        public override async Task ProcessRequestAsync(HttpContext httpContext)
        {
            string logStr = this.ToString() + ":" + MethodBase.GetCurrentMethod().Name + " ";
            Trace.WriteLine(logStr + "Enter");
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        if (httpContext.IsWebSocketRequest || httpContext.IsWebSocketRequestUpgrading)
                        {
                            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[512]);
                            httpContext.AcceptWebSocketRequest(async delegate(AspNetWebSocketContext aspNetWebSocketContext)
                            {
                                var socket = aspNetWebSocketContext.WebSocket;
                                var sbMsg = new StringBuilder();
                                //Checks if the connection is not already closed
                                while (socket != null || socket.State != WebSocketState.Closed)
                                {
                                    WebSocketReceiveResult receiveResult = await socket.ReceiveAsync(buffer, CancellationToken.None);

                                    switch (receiveResult.MessageType)
                                    {
                                        case WebSocketMessageType.Text:
                                            string message = Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
                                            sbMsg.Append(message);
                                            if (!receiveResult.EndOfMessage)
                                                continue;
                                            string finalMessage = sbMsg.ToString();
                                            _clientId = int.Parse(finalMessage.Split('~')[1]);
                                            Trace.WriteLine(logStr + "finalMessage: " + finalMessage);
                                            sbMsg.Clear();
                                            var socketSend = socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                            await socketSend;
                                            break;
                                        case WebSocketMessageType.Binary:
                                            Trace.WriteLine(logStr + "binary data received");
                                            break;
                                        case WebSocketMessageType.Close:
                                            Trace.WriteLine(logStr + "msg type close received");
                                            break;
                                    }

                                    switch (socket.State)
                                    {
                                        case WebSocketState.Connecting:
                                            this.OnConnecting();
                                            break;
                                        case WebSocketState.Open:
                                            this.OnOpen();
                                            break;
                                        case WebSocketState.CloseSent:
                                        case WebSocketState.CloseReceived:
                                        case WebSocketState.Closed:
                                            this.OnClosed();
                                            break;
                                        case WebSocketState.Aborted:
                                            this.OnAbort();
                                            break;
                                    }
                                }

                                Trace.WriteLine(logStr + "Socket closed");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message + ex.StackTrace);
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        private void OnAbort()
        {
            string logStr = this.ToString() + ":" + MethodBase.GetCurrentMethod().Name + " ";
            Trace.WriteLine(logStr + "Abort");
        }

        private void OnClosed()
        {
            --_count;
            string logStr = this.ToString() + ":" + MethodBase.GetCurrentMethod().Name + " ";
            Trace.WriteLine(logStr + "OnClosed count:" + _count + " ClientId:" + _clientId);

        }

        private void OnOpen()
        {
            try
            {
                string logStr = this.ToString() + ":" + MethodBase.GetCurrentMethod().Name + " ";
                Trace.WriteLine(logStr + "Open");
                ++_count;
                Trace.WriteLine("Count: " + _count);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        private void OnConnecting()
        {
            string logStr = this.ToString() + ":" + MethodBase.GetCurrentMethod().Name + " ";
            Trace.WriteLine(logStr + "Connecting");
        }
    }
}