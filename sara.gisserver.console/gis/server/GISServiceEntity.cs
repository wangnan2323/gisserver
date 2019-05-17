//****************************************
//Copyright@diligentpig, https://geopbs.codeplex.com
//Please using source code under LGPL license.
//****************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.Xml.Linq;
using System.ComponentModel;
using OSGeo.GDAL;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using sara.gisserver.console.gis.datasource;
using System.Net;
using System.Linq.Expressions;

namespace sara.gisserver.console.gis.server
{
    /// <summary>
    /// GIS类Service的实体对象
    /// </summary>
    public class GISServiceEntity
    {
        /// <summary>
        /// 服务名称--图层名称
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 数据源
        /// </summary>
        public DataSourceBase DataSource { get; set; }
        /// <summary>
        /// 是否允许服务器端缓存，memory缓存
        /// </summary>
        public bool AllowMemCache { get; set; }
        /// <summary>
        /// 是否开启客户端缓存
        /// </summary>
        public bool DisableClientCache { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool DisplayNoDataTile { get; set; }
        /// <summary>
        /// 设置tile类型的图形服务采用哪种颜色样式，
        /// </summary>
        public VisualStyle Style { get; set; }
        /// <summary>
        /// arcgis service url
        /// </summary>
        public string UrlArcGIS { get; private set; }
        /// <summary>
        /// ogc wmts url
        /// </summary>
        public string UrlWMTS { get { return UrlArcGIS + "/WMTS"; } }
        /// <summary>
        /// 当前服务的访问情况
        /// </summary>
        public TileLogEntity TitleLog { get; set; }
        /// <summary>
        /// 用于清理memory缓存，服务器端缓存的属性
        /// memcached批量删除方案探讨:http://it.dianping.com/memcached_item_batch_del.htm Key flag 方案
        /// </summary>        
        public string MemcachedValidKey = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="dataSourcePath"></param>
        /// <param name="port"></param>
        /// <param name="strType">DataSourceType enum + custom online maps</param>
        /// <param name="disableClientCache"></param>
        /// <param name="displayNodataTile"></param>
        /// <param name="tilingSchemePath">Set this parameter only when type is ArcGISDynamicMapService||RasterDataset and do not use Google Maps's tiling scheme</param>
        public GISServiceEntity(string serviceName, string dataSourcePath, int port, string strType, bool allowmemcache, bool disableClientCache, bool displayNodataTile, VisualStyle style, string tilingSchemePath)
        {
            ServiceName = serviceName;
            if (!DataSourceBase.IsOnlineMaps(strType))
            {
                DataSourceTypePredefined type = (DataSourceTypePredefined)Enum.Parse(typeof(DataSourceTypePredefined), strType);
                switch (type)
                {
                    case DataSourceTypePredefined.MobileAtlasCreator:
                        DataSource = new DataSourceMAC(dataSourcePath);
                        break;
                    case DataSourceTypePredefined.MBTiles:
                        DataSource = new DataSourceMBTiles(dataSourcePath);
                        break;
                    case DataSourceTypePredefined.ArcGISCache:
                        DataSource = new DataSourceArcGISCache(dataSourcePath);
                        break;
                    case DataSourceTypePredefined.ArcGISTilePackage:
                        DataSource = new DataSourceArcGISTilePackage(dataSourcePath);
                        break;
                    case DataSourceTypePredefined.RasterImage:
                        DataSource = new DataSourceRasterImage(dataSourcePath, tilingSchemePath, ServiceName);
                        break;
                    case DataSourceTypePredefined.ArcGISDynamicMapService:
                        DataSource = new DataSourceArcGISDynamicMapService(dataSourcePath, tilingSchemePath);
                        break;
                    case DataSourceTypePredefined.ArcGISTiledMapService:
                        DataSource = new DataSourceArcGISTiledMapService(dataSourcePath, tilingSchemePath);
                        break;
                    case DataSourceTypePredefined.ArcGISImageService:
                        DataSource = new DataSourceArcGISImageService(dataSourcePath, tilingSchemePath);
                        break;
                    case DataSourceTypePredefined.AutoNaviCache:
                        DataSource = new DataSourceAutoNaviCache(dataSourcePath);
                        break;
                    case DataSourceTypePredefined.OGCWMSService:
                        DataSource = new DataSourceWMSService(dataSourcePath, tilingSchemePath);
                        break;
                    case DataSourceTypePredefined.TianDiTuAnnotation:
                        DataSource = new DataSourceTianDiTuAnno();
                        break;
                    case DataSourceTypePredefined.TianDiTuMap:
                        DataSource = new DataSourceTianDiTuMap();
                        break;
                    default:
                        throw new Exception();
                }
                DataSource.IsOnlineMap = false;
            }
            else
            {
                bool known = false;
                foreach (var map in DataSourceCustomOnlineMaps.CustomOnlineMaps)
                {
                    if (map.Name == strType)
                    {
                        known = true;
                        break;
                    }
                }
                if (!known)
                {
                    throw new Exception("GIS类型【"+strType + "】不是已知的数据类型");
                }
                    
                DataSource = new DataSourceCustomOnlineMaps(strType)
                {
                    IsOnlineMap = true
                };
            }


            Port = port;
            AllowMemCache = allowmemcache;
            DisableClientCache = disableClientCache;
            DisplayNoDataTile = displayNodataTile;
            Style = style;
            TitleLog = new TileLogEntity();
            if (string.IsNullOrEmpty(sara.gisserver.console.gis.Global.IPAddress))
            {
                throw new Exception("创建GIS服务时IP地址不能为空!");
            }
                
            UrlArcGIS = "http://" + sara.gisserver.console.gis.Global.IPAddress + ":" + port.ToString() + "/"+ sara.gisserver.console.gis.Global.ServerName + "/rest/services/" + ServiceName + "/MapServer";
        }

        /// <summary>
        /// 释放数据源资源
        /// </summary>
        public void Dispose()
        {   
            if (DataSource is IDisposable)
            {
                ((IDisposable)DataSource).Dispose();
            }
                
        }

        ~GISServiceEntity()
        {

        }
    }

    /// <summary>
    /// tile类型服务用于记录日志的实体对象，似乎和提升性能有关
    /// </summary>
    public class TileLogEntity : INotifyPropertyChanged
    {
        /// <summary>
        /// how many tiles this service have been output totally
        /// </summary>
        public long OutputTileCountTotal
        {
            get { return _tileCountDynamic + _tileCountMemcached + _tileCountFileCached; }
        }
        private long _tileCountDynamic;
        /// <summary>
        /// how many tiles this service have been output dynamically
        /// </summary>
        public long OutputTileCountDynamic
        {
            get { return _tileCountDynamic; }
            set
            {
                _tileCountDynamic = value;
                NotifyPropertyChanged(p => p.OutputTileCountDynamic);
                NotifyPropertyChanged(p => p.OutputTileCountTotal);
            }
        }
        private long _tileCountMemcached;
        /// <summary>
        /// how many tiles this service have been output from Memcached
        /// </summary>
        public long OutputTileCountMemcached
        {
            get { return _tileCountMemcached; }
            set
            {
                _tileCountMemcached = value;
                NotifyPropertyChanged(p => p.OutputTileCountMemcached);
                NotifyPropertyChanged(p => p.OutputTileCountTotal);
            }
        }
        private long _tileCountFileCached;
        /// <summary>
        /// how many tiles this service have been output from cached file.
        /// </summary>
        public long OutputTileCountFileCache
        {
            get { return _tileCountFileCached; }
            set
            {
                _tileCountFileCached = value;
                NotifyPropertyChanged(p => p.OutputTileCountFileCache);
                NotifyPropertyChanged(p => p.OutputTileCountTotal);
            }
        }
        private double _tileTotalTime;
        /// <summary>
        /// total time count of all the output tiles. in Milliseconds.
        /// </summary>
        public double OutputTileTotalTime
        {
            get { return _tileTotalTime; }
            set
            {
                _tileTotalTime = value;
                NotifyPropertyChanged(p => p.OutputTileTotalTime);
                NotifyPropertyChanged(p => p.SPT);
            }
        }
        /// <summary>
        /// average one tile output time. senconds per tile.
        /// </summary>
        public double SPT
        {
            get
            {
                return _tileTotalTime / OutputTileCountTotal / 1000;
            }
        }
        private List<string> _requestedIPs;
        /// <summary>
        /// all requested client ip
        /// </summary>
        public List<string> RequestedIPs
        {
            get { return _requestedIPs; }
            set
            {
                _requestedIPs = value;
                NotifyPropertyChanged(p => p.RequestedIPs);
            }
        }
        private string _lastRequestClientIP;
        /// <summary>
        /// LastRequestClientIP address
        /// </summary>
        public string LastRequestClientIP
        {
            get
            {
                return _lastRequestClientIP;
            }
            set
            {
                _lastRequestClientIP = value;
                NotifyPropertyChanged(p => p.LastRequestClientIP);
            }
        }
        private int _requestedClientCounts;
        /// <summary>
        /// how many clients does this service has served
        /// </summary>
        public int RequestedClientCounts
        {
            get { return _requestedClientCounts; }
            set
            {
                _requestedClientCounts = value;
                NotifyPropertyChanged(p => p.RequestedClientCounts);
            }
        }

        public TileLogEntity()
        {
            RequestedIPs = new List<string>();
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged<TValue>(Expression<Func<TileLogEntity, TValue>> propertySelector)
        {
            if (PropertyChanged == null)
                return;

            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(memberExpression.Member.Name));
        }
        #endregion
    }

    /// <summary>
    /// 样式的枚举
    /// </summary>
    public enum VisualStyle
    {
        None,
        /// <summary>
        /// 灰度
        /// </summary>
        Gray,
        /// <summary>
        /// 反色
        /// </summary>
        Invert,
        /// <summary>
        /// 怀旧
        /// </summary>
        Tint,
        /// <summary>
        /// 饱和
        /// </summary>
        //Saturation,
        /// <summary>
        /// 浮雕
        /// </summary>
        Embossed
    }
}
