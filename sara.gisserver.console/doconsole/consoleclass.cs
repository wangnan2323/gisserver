using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Globalization;
using Eva.Library.Data;
using System.IO;
using System.Net;
using System.Net.Security;

using System.Net.Sockets;
using System.Web;
using System.Security.Cryptography.X509Certificates;
namespace sara.gisserver.console.doconsole
{
    public class consoleclass
    {

        public consoleclass()
        {

        }

        public void doConsole(string[] args)
        {
            string isQuit = "";
            do
            {
                if (isQuit.ToLower() == "")
                {
                    Console.Write("请输入指令\r\n");
                }
                else
                {
                    string commandName = "";// isQuit.Split(' ')[0].ToString();
                    string commandParameter = "";// isQuit.Split(' ')[1].ToString();

                    if (isQuit.Split(' ').Length == 2)
                    {
                        commandName = isQuit.Split(' ')[0].ToString();
                        commandParameter = isQuit.Split(' ')[1].ToString();
                    }
                    else
                    {
                        commandName = isQuit.ToString();
                    }

                    switch (commandName.ToLower())
                    {
                        case"?":
                            {
                               
                               
                            }
                            break;
                        case "start":
                            {
                                string aa = "C:\\inetpub\\wwwroot\\sara\\sara.gisserver.console\\data\\";
                                if (sara.gisserver.console.gis.server.ServerManager.ServicesInServer == null || (sara.gisserver.console.gis.server.ServerManager.ServicesInServer != null && sara.gisserver.console.gis.server.ServerManager.ServicesInServer.Count == 0))
                                {
                                    sara.gisserver.console.gis.Global.IPAddress = "127.0.0.1";
                                    sara.gisserver.console.gis.server.ServerManager.StartServer(7081);
                                    Console.WriteLine("服务器创建成功");
                                    sara.gisserver.console.gis.server.ServerManager.CreateGISService("tjroad", 7081, "MBTiles", AppDomain.CurrentDomain.BaseDirectory + "\\data\\tjroad.mbtiles", true, false, false, sara.gisserver.console.gis.server.VisualStyle.None);
                                    Console.WriteLine("图形服务创建成功");

                                    sara.gisserver.console.gis.server.ServerManager.CreateGISService("tjwp", 7081, "MBTiles", AppDomain.CurrentDomain.BaseDirectory + "\\data\\tjwp.mbtiles", true, false, false, sara.gisserver.console.gis.server.VisualStyle.None);
                                    Console.WriteLine("图形服务创建成功");

                                    sara.gisserver.console.gis.server.ServerManager.CreateGISService("tjroadtpk", 7081, "ArcGISTilePackage", AppDomain.CurrentDomain.BaseDirectory + "\\data\\tjroadtpk.tpk", true, false, false, sara.gisserver.console.gis.server.VisualStyle.None);
                                    Console.WriteLine("图形服务创建成功");


                                    ////http://162.16.166.2:6080/arcgis/rest/services/tjmap2/MapServer
                                    //sara.gisserver.console.gis.server.ServerManager.CreateGISService("tjmap2", 7081, "ArcGISDynamicMapService",  "http://162.16.166.2:6080/arcgis/rest/services/tjmap2/MapServer", true, false, false, sara.gisserver.console.gis.server.VisualStyle.None);
                                    //Console.WriteLine("图形服务创建成功");

                                    sara.gisserver.console.doconsole.browserclass.OpenChrome("http://" + sara.gisserver.console.gis.Global.IPAddress + ":7081/");
                               
                                }
                            }
                            break;
                        case "testadd":
                            {


                            }
                            break;
                       

                       
                        default:

                            Console.WriteLine("未知指令");
                            break;
                    }



                }

                isQuit = Console.ReadLine();
            }
            while (!isQuit.ToLower().Equals("exit"));
        }

        /*
        public static string CreateServiceByHTTP(string name, int port, string dataSourceType, string dataSorucePath, bool allowMemoryCache, bool disableClientCache, bool displayNoDataTile, VisualStyle style, string tilingSchemePath = null)
        {
            System.Collections.Hashtable ht = new System.Collections.Hashtable();
            ht.Add("name", name);
            ht.Add("port", port);
            ht.Add("dataSourceType", dataSourceType.ToString());
            ht.Add("dataSourcePath", dataSorucePath);
            ht.Add("allowMemoryCache", allowMemoryCache);
            ht.Add("disableClientCache", disableClientCache);
            ht.Add("displayNodataTile", displayNoDataTile);
            ht.Add("visualStyle", style.ToString());
            ht.Add("tilingSchemePath", tilingSchemePath);
            byte[] postData = Encoding.UTF8.GetBytes(sara.gisserver.console.gis.util.JSON.JsonEncode(ht));

            HttpWebRequest myReq = WebRequest.Create("http://192.168.26.128:7080/saragisserver/rest/admin/addService") as HttpWebRequest;
            myReq.Method = "POST";
            string username = "esrichina";
            string password = "esrichina";
            string usernamePassword = username + ":" + password;
            //注意格式 “用户名:密码”，之后Base64编码
            myReq.Headers.Add("Authorization", Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
            myReq.ContentLength = postData.Length;
            using (System.IO.Stream requestStream = myReq.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }
            WebResponse wr = myReq.GetResponse();
            System.IO.Stream receiveStream = wr.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader(receiveStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            receiveStream.Close();
            reader.Close();
            return string.Empty;
        }
        */
    }
}




