using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO.Ports;   //头文件
using System.Data;
using System.ServiceProcess;
namespace sara.gisserver.console
{
    class Program
    {
        static void Main(string[] args)
        {

            Eva.Library.Global.AppRootPath = AppDomain.CurrentDomain.BaseDirectory ;           
            Eva.Library.Global.ConfigFileName = "sara.gisserver.console.config";


            sara.gisserver.console.doconsole.consoleclass c = new doconsole.consoleclass();
            c.doConsole(args);



        }

    }
}

