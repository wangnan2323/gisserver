//****************************************
//Copyright@diligentpig, https://geopbs.codeplex.com
//Please using source code under LGPL license.
//****************************************
using System;
using System.Text;
using sara.gisserver.console.gis.server;
using System.ServiceModel.Web;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.IO;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using Memcached.ClientLibrary;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Zip;
using System.Windows.Threading;
using System.Security.Principal;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

using System.Reflection;

namespace sara.gisserver.console.gis.util
{

    public class MemCache : INotifyPropertyChanged
    {
        //dlls using: 
        //Memcached.ClientLibrary.dll(client):Commons.dll,ICSharpCode.SharpZipLib.dll,log4net.dll
        //memcached.exe(server):msvcr71.dll(when in system32 folder, take no effects)
        //help:"memcached -h"
        // why not velocity: 
        // http://stackoverflow.com/questions/397824/ms-velocity-vs-memcached-for-windows
        // Velocity versus Memcached:http://blog.moxen.us/2010/05/26/velocity-versus-memcached/
        // .NET中使用Memcached的相关资源整理： http://www.cnblogs.com/dudu/archive/2009/07/19/1526407.html
        // memcached+net缓存：http://www.cnblogs.com/wyxy2005/archive/2010/08/23/1806785.html      
        private bool _isActived;//MemCache switch to indicate if this memory cache ability is currently enabled.
        public bool IsActived
        {
            get { return _isActived; }
            set
            {
                _isActived = value;
                NotifyPropertyChanged("IsActived");
                if (MemCache.IsActivedChanged != null)
                    MemCache.IsActivedChanged(this, new IsActivedChangedEventArgs(_isActived));
            }
        }
        public MemcachedClient MC { get; set; }
        public delegate void IsActivedChangedEventHandler(object sender, IsActivedChangedEventArgs e);
        public static event IsActivedChangedEventHandler IsActivedChanged;//raised when IsActived property changed, used for app to change UI. In order to add event listener before ServiceManager.Memcache is initialized(so app UI could be changed when enable memory cache through REST admin API), this must be static.
        public class IsActivedChangedEventArgs : EventArgs
        {
            public bool NewValue { get; set; }
            public IsActivedChangedEventArgs(bool b)
            {
                NewValue = b;
            }
        }

        private SockIOPool _pool;
        private Process _cmdMemcachedProcess;//used for holding the Memcached process, if this process has been closed, the process in taskmanager will be lost.
        private string _memcachedInWinFolder;

        //max memory to use for memcached items in megabytes, default is 64 MB
        public MemCache(int memorySize)
        {
            //the goal is copy memcached.exe to %windir% folder, then from there, install memcached windows service so that the executable path of the service is %windir%\memcached.exe. Thus, PBS folder could be moved to anywhere else.
            string strCmdResult;
            try
            {
                //check if memcached.exe,msvcr71.dll exists in PBS folder
                if (!File.Exists("memcached.exe"))
                    throw new Exception("memcached.exe doesn't exists!");
                if (!File.Exists("msvcr71.dll"))
                    throw new Exception("msvcr71.dll doesn't exists!");
                _memcachedInWinFolder = Environment.ExpandEnvironmentVariables("%SystemRoot%") + "\\memcached.exe";
                //check if memcached.exe,pthreadGC2.dll exists in windows path
                if (!File.Exists(_memcachedInWinFolder))
                    File.Copy("memcached.exe", _memcachedInWinFolder, true);
                if (!File.Exists(Environment.ExpandEnvironmentVariables("%SystemRoot%") + "\\msvcr71.dll"))
                    File.Copy("msvcr71.dll", Environment.ExpandEnvironmentVariables("%SystemRoot%") + "\\msvcr71.dll", true);
                //if windows service exists, check if the exe path of the service is in windows directory
                if (Utility.IsWindowsServiceExisted("memcached Server"))
                {
                    Utility.Cmd("sc qc \"memcached Server\"", false, out strCmdResult);
                    if (!strCmdResult.Contains("\\Windows\\"))
                        Utility.Cmd("sc delete \"memcached Server\"", false, out strCmdResult);//try to uninstall windows service
                }
                //check if windows service exists
                if (!Utility.IsWindowsServiceExisted("memcached Server"))
                {
                    Utility.Cmd(_memcachedInWinFolder + " -d install", false, out strCmdResult);//install memcached windows service
                    //google:使用cmd命令手动、自动启动和禁用服务
                    Utility.Cmd("sc config \"memcached Server\" start= demand", false, out strCmdResult);//set to 手动启动
                }
                Utility.Cmd(_memcachedInWinFolder + " -d stop", false, out strCmdResult);
                _cmdMemcachedProcess = Utility.Cmd(_memcachedInWinFolder + " -m " + memorySize + " -d start", true, out strCmdResult);
                string[] serverlist = { "127.0.0.1:11211" };

                // initialize the pool for memcache servers
                _pool = SockIOPool.GetInstance();
                _pool.SetServers(serverlist);

                _pool.InitConnections = 3;
                _pool.MinConnections = 3;
                _pool.MaxConnections = 5;

                _pool.SocketConnectTimeout = 1000;
                _pool.SocketTimeout = 3000;

                _pool.MaintenanceSleep = 30;
                _pool.Failover = true;

                _pool.Nagle = false;
                _pool.Initialize();
                if (_pool.GetConnection(serverlist[0]) == null)
                {
                    _pool.Shutdown();
                    Utility.Cmd("sc delete \"memcached Server\"", false, out strCmdResult);//try to uninstall windows service
                    throw new Exception("Can not managed to run 'memcached -d start' on your machine.");
                }

                MC = new MemcachedClient()
                {
                    EnableCompression = false
                };
                //try to cache one object in memory to ensure memcached ability can be used
                if (!MC.Set("test", DateTime.Now.ToString()))
                {
                    throw new Exception("Can't managed to set key-value in memched!");
                }
                IsActived = true;
            }
            catch (Exception e)
            {
                IsActived = false;
                Shutdown();//important
                //copy error:"Access to the path 'C:\Windows\system32\memcached.exe' is denied."
                if (e.Message.Contains("Access") && e.Message.Contains("denied"))
                    throw new Exception("Copy memcached.exe to '%windir%' failed.\r\nPlease reopen PortableBasemapServer by right clicking and select 'Run as Administrator'.");
                throw new Exception(e.Message);
            }
        }

        //make memory cache of a specified service unavailable(not really delete them)
        public string InvalidateServiceMemcache(int port, string servicename)
        {
            /// memcached批量删除方案探讨:http://it.dianping.com/memcached_item_batch_del.htm Key flag 方案
            if (!ServerManager.ServerEntitiyDic.ContainsKey(port))
                return "The specified port does not exist in PBS.";
            else if (!ServerManager.ServerEntitiyDic[port].ServerProvider.GISServicesDic.ContainsKey(servicename))
                return "The specified service does not exist on port " + port + ".";
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.Year);
                sb.Append(DateTime.Now.Month);
                sb.Append(DateTime.Now.Day);
                sb.Append(DateTime.Now.Hour);
                sb.Append(DateTime.Now.Minute);
                sb.Append(DateTime.Now.Second);
                ServerManager.ServerEntitiyDic[port].ServerProvider.GISServicesDic[servicename].MemcachedValidKey = sb.ToString();
                return string.Empty;
            }
        }

        public void FlushAll()
        {
            MC.FlushAll();//just make all the items in memcached unavailable by expiring them
        }

        public void Shutdown()
        {
            if (_pool != null)
                _pool.Shutdown();//very important. otherwise, the app will not be termitated after closing the window
            _pool = null;
            //after leave this function, _cmdMemcachedProcess will be automatic exited by app lifecycle
            if (_cmdMemcachedProcess != null)
            {
                _cmdMemcachedProcess.StandardInput.WriteLine(_memcachedInWinFolder + " -d stop");
                System.Threading.Thread.Sleep(250);
                _cmdMemcachedProcess.StandardInput.WriteLine("exit");
                System.Threading.Thread.Sleep(250);
                _cmdMemcachedProcess.Close();
            }
            _cmdMemcachedProcess = null;
            IsActived = false;
            if (MC != null)
            { MC.FlushAll(); MC = null; }
        }

        ~MemCache()
        {
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

   
}
