using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Bannersoft.WindProxy.Wind;
using Bannersoft.WindProxy.Commons;
using WAPIWrapperCSharp;

namespace Bannersoft.WindProxy.Http
{
    class ProxyServer
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ProxyServer));

        public static readonly int SERVER_PORT = ConfigUtil.getInt("http_port");
        public static readonly int TIME_OUT = 5 * 60 * 1000;
        public static readonly int MAX_CONN = 100;
        public static readonly string CTX_PATH = "/";

        public static readonly string RUL_EDB = "/wind/edb";
        public static readonly string RUL_WSD = "/wind/wsd";
        public static readonly string RUL_WSET = "/wind/wset";
        public static readonly string RUL_WSS = "/wind/wss";

        public ProxyServer()
        {
        }

        public void start()
        {
            //tcp/ip socket (ipv4)
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            server.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
            server.Listen(MAX_CONN);
            server.ReceiveTimeout = TIME_OUT;
            server.SendTimeout = TIME_OUT;

            // Our thread that will listen connection requests
            // and create new threads to handle them.
            Thread requestListenerT = new Thread(() =>
            {
                while (true)
                {
                    Socket client = null;
                    try
                    {
                        client = server.Accept();
                        // Create new thread to handle the request and continue to listen the socket.
                        Thread requestHandler = new Thread(() =>
                        {
                            client.ReceiveTimeout = TIME_OUT;
                            client.SendTimeout = TIME_OUT;
                            try
                            {
                                handleTheRequest(client);
                            }
                            catch
                            {
                                try
                                {
                                    sendJson(client, "{ errorCode : " + 500 + " }");
                                    client.Close();
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex);
                                }
                            }
                        });

                        //process request
                        requestHandler.Start();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
            });

            requestListenerT.Start();

            string url = "http://" + SystemUtil.getIPAddress() + ":" + SERVER_PORT;

            Console.WriteLine(" Wind Proxy Server started at " + url);
            Console.WriteLine(" ------------------------");
            Console.WriteLine(" EDB : " + url + "/wind/edb?codes=M0061580,M0061581&startTime=2015-10-01&endTime=2017-03-01&options=Fill=Previous");
            Console.WriteLine(" WSD : " + url + "/wind/wsd?codes=600036.SH&fields=industry_CSRC12,close,open&startTime=2016-12-01&endTime=2017-03-01&options=Fill=Previous");
            Console.WriteLine(" WSS : " + url + "/wind/wss?codes=000008.SZ,000011.SZ,000017.SZ&fields=sec_name,sec_englishname,exch_city,ipo_date");
            Console.WriteLine(" WSET : " + url + "/wind/wset?reportName=tradesuspend&options=startdate=2017-02-25;enddate=2017-03-01");
            Console.WriteLine(" ------------------------\n");
        }

        private void handleTheRequest(Socket client)
        {
            byte[] buffer = new byte[10240]; // 10 kb, just in case
            int receivedCount = client.Receive(buffer); // Receive the request
            string strReceived = Encoding.UTF8.GetString(buffer, 0, receivedCount);

            // Parse method of the request
            string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" "));

            int start = strReceived.IndexOf(httpMethod) + httpMethod.Length + 1;
            int length = strReceived.LastIndexOf("HTTP") - start - 1;

            string url = strReceived.Substring(start, length);
            url = url.Replace("//", "/").Replace("//", "/");
            NameValueCollection param = ParseUrl(url);

            log.Debug(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " \n -----------------------\n" + url);

            WindData wd = null;

            if (param != null && param.Count > 0)
            {
                string codes = param.Get("codes");
                string startTime = param.Get("startTime");
                string endTime = param.Get("endTime");
                string options = param.Get("options");
                string fields = param.Get("fields");
                string reportName = param.Get("reportName");

                if (startTime == null)
                {
                    startTime = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
                }
                if (endTime == null)
                {
                    endTime = DateTime.Now.ToString("yyyy-MM-dd");
                }
                if (options == null)
                {
                    options = "";
                }

                if (url.ToLower().StartsWith(RUL_EDB))
                {
                    wd = WindUtil.getAPI().edb(codes, startTime, endTime, options);
                }
                else if (url.ToLower().StartsWith(RUL_WSD))
                {
                    wd = WindUtil.getAPI().wsd(codes, fields, startTime, endTime, options);
                }
                else if (url.ToLower().StartsWith(RUL_WSET))
                {
                    wd = WindUtil.getAPI().wset(reportName, options);
                }
                else if (url.ToLower().StartsWith(RUL_WSS))
                {
                    wd = WindUtil.getAPI().wss(codes, fields, options);
                }
            }

            if (wd != null)
            {
                sendJson(client, JsonConvert.SerializeObject(wd));
            }
            else
            {
                sendJson(client, "{ errorCode : " + 500 + " }");
            }
        }

        private void sendJson(Socket socket, String json)
        {
            sendResponse(socket, json, "200 OK", "text/json");
            log.Debug("\n response: \n--------------------------------");
            log.Debug(json);
        }


        private void sendResponse(Socket socket, string strContent, string responseCode, string contentType)
        {
            try
            {
                //byte[] bContent = CharEncoder.GetBytes(strContent);
                byte[] bContent = System.Text.Encoding.Default.GetBytes(strContent);
                byte[] bHeader = Encoding.UTF8.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Wind Proxy Server\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");

                socket.Send(bHeader);
                socket.Send(bContent);
                socket.Close();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }


        /* URL参数解析 */
        public static NameValueCollection ParseUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            NameValueCollection nvc = new NameValueCollection();
            try
            {
                int questionMarkIndex = url.IndexOf('?');

                if (questionMarkIndex == url.Length - 1)
                    return null;
                string ps = url.Substring(questionMarkIndex + 1);

                // 开始分析参数对   
                System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", System.Text.RegularExpressions.RegexOptions.Compiled);
                System.Text.RegularExpressions.MatchCollection mc = re.Matches(ps);

                foreach (System.Text.RegularExpressions.Match m in mc)
                {
                    nvc.Add(m.Result("$2").ToLower(), m.Result("$3"));
                }
            }
            catch { }
            return nvc;
        }
    }
}
