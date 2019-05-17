//****************************************
//Copyright@diligentpig, https://geopbs.codeplex.com
//Please using source code under LGPL license.
//****************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Web;
using System.ServiceModel.Description;
using System.ServiceModel;
using sara.gisserver.console.gis.datasource;
using System.Collections.ObjectModel;
using System.Net;
using sara.gisserver.console.gis.util;

namespace sara.gisserver.console.gis.server
{
    public static class ServerManager
    {
        #region ServerEntitiyDic服务器的集合，这种设计可以让程序多开
        /// <summary>
        /// 端口集合，这种设计可以让程序多开
        /// </summary>
        private static Dictionary<int, ServerEntity> _serverEntitiyDic;
        public static Dictionary<int, ServerEntity> ServerEntitiyDic
        {
            get
            {
                if (_serverEntitiyDic == null)
                {
                    _serverEntitiyDic = new Dictionary<int, ServerEntity>();
                }
                return _serverEntitiyDic;
            }
            set
            {
                _serverEntitiyDic = value;
            }
        }
        #endregion



        /// <summary>
        /// 端口下的所有服务
        /// </summary>
        public static MTObservableCollection<GISServiceEntity> ServicesInServer { get; set; }

        /// <summary>
        ///采用Memcached实现服务器端缓存
        /// </summary>
        public static MemCache Memcache { get; set; }

        /// <summary>
        /// 开启某个端口下的服务
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static string StartServer(int port)
        {
            //实例化服务器下的服务，如果已经存在了服务，则不会被清空
            if (ServicesInServer == null)
            {
                ServicesInServer = new MTObservableCollection<GISServiceEntity>();
            }

            //端口和服务器是一对一的关系
            if (!ServerEntitiyDic.ContainsKey(port))
            {
                try
                {
                    //实例化服务器   
                    WebServiceHost server = new WebServiceHost(typeof(ServerProvider), new Uri("http://" + sara.gisserver.console.gis.Global.IPAddress + ":" + port));
                    server.AddServiceEndpoint(typeof(IServerProvider), new WebHttpBinding(), "").Behaviors.Add(new WebHttpBehavior());

                    ServiceDebugBehavior stp = server.Description.Behaviors.Find<ServiceDebugBehavior>();
                    stp.HttpHelpPageEnabled = false;
                    //启动服务器
                    server.Open();
                    //将服务器添加到集合中
                    ServerEntitiyDic.Add(port, new ServerEntity(server, new ServerProvider()));
                   
                    return string.Empty;
                }
                catch (Exception e)
                {
                    //HTTP 无法注册 URL http://+:7777/CalulaterServic/。进程不具有此命名空间的访问权限(有关详细信息，请参阅 http://go.microsoft.com/fwlink/?LinkId=70353)
                    if (e.Message.Contains("http://go.microsoft.com/fwlink/?LinkId=70353"))
                    {
                        return "你的操作系统启动了UAC,请用“以管理员身份运行”运行本程序\r\n" + e.Message + e.StackTrace;
                    }
                    else
                    {
                        return "实例化服务器失败\r\n" + e.Message + e.StackTrace;
                    }
                }
            }
            else
            {
                return "端口【" + port + "】已经在运行!";
            }
        }

        /// <summary>
        /// 根据端口号和服务名称（图层名称）获取服务的实体对象
        /// </summary>
        /// <param name="port"></param>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static GISServiceEntity GetServiceEntity(int port, string serviceName)
        {
            ServerEntity portEntity;
            if (ServerManager.ServerEntitiyDic.TryGetValue(port, out portEntity) && portEntity.ServerProvider.GISServicesDic.ContainsKey(serviceName))
            {
                return portEntity.ServerProvider.GISServicesDic[serviceName];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 创建地图服务
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="port"></param>
        /// <param name="strType">DataSourceType enum + custom online maps</param>
        /// <param name="dataSorucePath"></param>
        /// <param name="disableClientCache"></param>
        /// <param name="displayNoDataTile"></param>
        /// <param name="style"></param>
        /// <param name="tilingSchemePath">Set this parameter only when type is ArcGISDynamicMapService and do not use Google Maps's tiling scheme</param>
        /// <returns>errors or warnings. string.empty if nothing wrong.</returns>
        public static string CreateGISService(string serviceName, int port, string strType, string dataSorucePath, bool allowMemoryCache, bool disableClientCache, bool displayNoDataTile, VisualStyle style, string tilingSchemePath = null)
        {
            ServerProvider serverProvider = null;
            string errorMessage;
            //验证该端口指向的服务器是否已经启动
            if (!ServerEntitiyDic.ContainsKey(port))
            {
                ///如果服务器启动失败
                errorMessage = StartServer(port);
                if (errorMessage != string.Empty)
                {
                    return errorMessage;
                }                    
            }
            //获取服务器接口对象
            serverProvider = ServerEntitiyDic[port].ServerProvider;

            //服务器对象想服务名称必须唯一
            if (serverProvider.GISServicesDic.ContainsKey(serviceName))
            {
                return "服务名称已经存在!";
            }

            GISServiceEntity gisservice;
            try
            {
                gisservice = new GISServiceEntity(serviceName, dataSorucePath, port, strType, allowMemoryCache, disableClientCache, displayNoDataTile, style, tilingSchemePath);
            }
            catch (Exception ex)//in case of reading conf.xml or conf.cdi file error|| reading a sqlite db error
            {
                
                return "创建服务【" + serviceName + "】 失败!\r\n数据源:【 " + dataSorucePath + "】\r\n\r\n" + ex.Message+ex.StackTrace;
            }
            ///向缓存中增加对象
            serverProvider.GISServicesDic.Add(serviceName, gisservice);
            ServicesInServer.Add(gisservice);
            return string.Empty;
        }

        /// <summary>
        /// 删除地图服务
        /// </summary>
        /// <param name="port"></param>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static string DeleteService(int port, string serviceName)
        {
            if (!ServerEntitiyDic.ContainsKey(port))
            {
                return "端口【 " + port + "】的服务器尚未启动";
            }
                
            if (!ServerEntitiyDic[port].ServerProvider.GISServicesDic.ContainsKey(serviceName))
            {
                return "名称为【 " + serviceName + "】的服务不存在与端口为【" + port + "】的服务器中.";
            }
                
            ServicesInServer.Remove(ServerEntitiyDic[port].ServerProvider.GISServicesDic[serviceName]);
            ServerEntitiyDic[port].ServerProvider.GISServicesDic[serviceName].Dispose();
            ServerEntitiyDic[port].ServerProvider.GISServicesDic.Remove(serviceName);
            return string.Empty;
        }
        /// <summary>
        /// 删除全部的服务器和其下全部服务
        /// </summary>
        public static void DeleteAllServer()
        {
            while (ServicesInServer != null && ServicesInServer.Count > 0)
            {
                DeleteService(ServicesInServer[0].Port, ServicesInServer[0].ServiceName);
            }
            if (ServerEntitiyDic != null)
            {
                List<int> ports = ServerEntitiyDic.Keys.ToList();
                for (int i = 0; i < ports.Count; i++)
                {
                    ServerEntitiyDic[ports[i]].ServerProvider.GISServicesDic.Clear();
                    ServerEntitiyDic[ports[i]].ServerHost.Close();
                    ServerEntitiyDic.Remove(ports[i]);
                }
            }
        }
      
    }

    /// <summary>
    /// 一个服务抢占一个端口，端口的实体对象类
    /// </summary>
    public class ServerEntity
    {
        /// <summary>
        /// 服务器对象
        /// </summary>
        public WebServiceHost ServerHost { get; set; }
        /// <summary>
        /// 服务器接口对象
        /// </summary>
        public ServerProvider ServerProvider { get; set; }
        public ServerEntity(WebServiceHost host, ServerProvider provider)
        {
            ServerHost = host;
            ServerProvider = provider;
        }
    }
}
