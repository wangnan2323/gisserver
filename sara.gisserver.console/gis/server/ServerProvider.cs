//****************************************
//Copyright@diligentpig, https://geopbs.codeplex.com
//Please using source code under LGPL license.
//****************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Web;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using sara.gisserver.console.gis.datasource;
using System.Collections;
using sara.gisserver.console.gis.util;
using System.Xml.Linq;
using System.Linq;
using System.Data;
using sara.gisserver.console.doconsole;

namespace sara.gisserver.console.gis.server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    [ServiceThrottling(10000, 10000, 10000)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class ServerProvider : IServerProvider
    {
        private string connectString = string.Format("Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "default.sqlite;Pooling=true;FailIfMissing=false");
        /// <summary>
        /// 当前服务器下全部服务的集合
        /// </summary>
        public Dictionary<string, GISServiceEntity> GISServicesDic { get; set; }


        /// <summary>
        /// 构造函数
        /// </summary>
        public ServerProvider()
        {
            GISServicesDic = new Dictionary<string, GISServiceEntity>();
            ServerEntity serverEntity;
            int requestPort = Utility.GetRequestPortNumber();
            if (ServerManager.ServerEntitiyDic.TryGetValue(requestPort, out serverEntity))
            {
                GISServicesDic = serverEntity.ServerProvider.GISServicesDic;
            }
            if (requestPort != -1 && !ServerManager.ServerEntitiyDic.ContainsKey(requestPort))
            {
                throw new WebFaultException<string>("请求的端口号不存在于服务中. This can be caused by setting a url rewrite/revers proxy incorrectly.", HttpStatusCode.BadRequest);

            }

        }
        
        /// <summary>
        /// root
        /// </summary>
        /// <returns></returns>
        public Stream Root()
        {

            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html; charset=utf-8";
            string str = sara.gisserver.console.doconsole.ioclass.readFile(AppDomain.CurrentDomain.BaseDirectory + "html\\root.html");

            return StreamFromPlainText(str);
        }

        public Stream GetImage(string imageName)
        {

            FileStream fs = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "images\\" + imageName); //OpenRead
            int filelength = 0;
            filelength = (int)fs.Length; //获得文件长度 
            byte[] image = new byte[filelength]; //建立一个字节数组 
            fs.Read(image, 0, filelength); //按字节流读取 
            System.Drawing.Image result = System.Drawing.Image.FromStream(fs);
            fs.Close();

            return new MemoryStream(image);
        }


        #region 跨域文件
        /// <summary>
        /// 跨域文件
        /// </summary>
        /// <returns></returns>
        public Stream ClientAccessPolicyFile()
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            string str = @"<?xml version=""1.0"" encoding=""utf-8"" ?><access-policy><cross-domain-access><policy><allow-from http-request-headers=""*""><domain uri=""*""/><domain uri=""http://*""/></allow-from><grant-to><resource path=""/"" include-subpaths=""true""/></grant-to></policy></cross-domain-access></access-policy>";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            return new MemoryStream(bytes);
        }

        /// <summary>
        /// 跨域文件
        /// </summary>
        /// <returns></returns>
        public Stream CrossDomainFile()
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            string str = @"<?xml version=""1.0"" ?><cross-domain-policy><allow-access-from domain=""*""/><site-control permitted-cross-domain-policies=""all""/><allow-http-request-headers-from domain=""*"" headers=""*""/></cross-domain-policy>";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            return new MemoryStream(bytes);
        }
        #endregion

        #region esri空间操作
        /// <summary>
        /// 变换坐标系
        /// </summary>
        /// <param name="f"></param>
        /// <param name="outSR"></param>
        /// <param name="inSR"></param>
        /// <param name="geometries"></param>
        /// <returns></returns>
        public Stream GeometryServerProject(string f, string outSR, string inSR, string geometries)
        {
            
            //转换为ST类型坐标系
            string STstr = TransformToST(geometries);

            string sql = "select ST_AsText(ST_Transform(ST_GeomFromText(" + STstr + "," + inSR + ")," + outSR + ")) as res";
            //string sql = "select ST_AsText(ST_Simplify(ST_GeomFromText('POLYGON((12996138.984346 4788910.669415,12996635.82503 4788222.73616,12996138.984346 4787916.988047,12996138.984346 4788910.669415))',3857),0.1)) as res";
            //转换wkid后的ST类型坐标系
            string newgeometries = sara.gisserver.console.doconsole.AccessSQLiteDataFactory.GetSingle(sql, connectString).ToString();

            //转换wkid后的geometries坐标系
            string resultgeometries = TransformToGeometries(newgeometries, outSR, "1");

            return StreamFromPlainText(resultgeometries);
        }

        /// <summary>
        /// 简单化
        /// </summary>
        /// <param name="f"></param>
        /// <param name="sr"></param>
        /// <param name="geometries"></param>
        /// <returns></returns>
        public Stream GeometryServerSimplify(string f, string sr, string geometries)
        {

            ////转换为ST类型坐标系
            //string STstr = TransformToST(geometries);
            ////ST类型坐标系简单化
            //string sql1 = "select ST_AsText(ST_Simplify(ST_GeomFromText(" + STstr + "," + sr + "),0.1)) as res";
            ////等待
            ////string resultST = sara.gisserver.console.doconsole.AccessSQLiteDataFactory.GetSingle(sql1,connectString).ToString();

            ////临时使用 
            //string resultST = "POLYGON((12996138.984346 4788910.669415, 12996635.82503 4788222.73616, 12996138.984346 4787916.988047, 12996138.984346 4788910.669415))";
            ////简单化后的Geometries
            //string resultgeometries = TransformToGeometries(resultST, sr,"1");
            return StreamFromPlainText(geometries);
        }

        /// <summary>
        /// line测量长度
        /// </summary>
        /// <param name="f"></param>
        /// <param name="polylines"></param>
        /// <param name="sr"></param>
        /// <param name="lengthUnit"></param>
        /// <param name="geodesic"></param>
        /// <returns></returns>
        public Stream GeometryServerLengths(string f, string polylines, string sr, string lengthUnit, string geodesic)
        {
            //转换为ST类型坐标系
            string STstr = TransformToST(polylines);            
            
            //线
            string sqllength = "select ST_Length(ST_GeomFromText(" + STstr + "," + sr + ")) as length";


            string length = sara.gisserver.console.doconsole.AccessSQLiteDataFactory.GetSingle(sqllength,connectString).ToString();

                
            

            string result = "{\"lengths\":[" + length + "]}";
            return StreamFromPlainText(result);
        }

        /// <summary>
        /// 面图形测量长度和面积
        /// </summary>
        /// <param name="f"></param>
        /// <param name="polygons"></param>
        /// <param name="sr"></param>
        /// <param name="lengthUnit"></param>
        /// <param name="areaUnit"></param>
        /// <returns></returns>
        public Stream GeometryServerAreasAndLengths(string f, string polygons, string sr, string lengthUnit, string areaUnit)
        {
            //转换为ST类型坐标系
            string STstr = TransformToST(polygons);


                //面
                string sqlarea = "select ST_Area(ST_GeomFromText(" + STstr + "," + sr + ")) as area";
                string sqllength = "select ST_Length(ST_GeomFromText(" + STstr.Replace("POLYGON((", "LINESTRING(").Replace("))", ")") + "," + sr + ")) as length";

            string area = sara.gisserver.console.doconsole.AccessSQLiteDataFactory.GetSingle(sqlarea,connectString).ToString();
            string length = sara.gisserver.console.doconsole.AccessSQLiteDataFactory.GetSingle(sqllength,connectString).ToString();


  

            string result = "{\"areas\":["+area+"],\"lengths\":["+length+"]}";
            return StreamFromPlainText(result);
        }

        /// <summary>
        /// 合并图形
        /// </summary>
        /// <param name="f"></param>
        /// <param name="outSR"></param>
        /// <param name="inSR"></param>
        /// <param name="geometries"></param>
        /// <returns></returns>
        public Stream GeometryServerUnion(string f, string outSR, string inSR, string geometries)
        {
            //结果字符串
            string resultgeometries = "";

            return StreamFromPlainText(resultgeometries);
        }

        /// <summary>
        /// 空间查询
        /// </summary>
        /// <param name="f"></param>
        /// <param name="servicename"></param>
        /// <param name="layerindex"></param>
        /// <param name="inSR"></param>
        /// <param name="geometry"></param>
        /// <param name="geometryType"></param>
        /// <param name="spatialRel"></param>
        /// <param name="where"></param>
        /// <param name="returnGeometry"></param>
        /// <param name="outSR"></param>
        /// <param name="outFields"></param>
        /// <returns></returns>
        public Stream GeometryServerQuery(string f, string servicename, string layerindex, string inSR, string geometry, string geometryType, string spatialRel, string where, string returnGeometry, string outSR, string outFields)
        {
           
            //转换为ST类型坐标系

            string stString = TransformToST(geometry);

            string sql = "";
            sql += " select asText(ST_Transform(SHAPE, " + outSR + ")) as geo,name,objectid from 水域 as a";
            sql += " where ST_Intersects(";
            sql += " ST_GeomFromText(" + stString + ", " + inSR + "),";
            sql += " ST_Transform(SHAPE, " + inSR + "))= 1";
            DataSet ds = sara.gisserver.console.doconsole.AccessSQLiteDataFactory.Query(sql,connectString);
            //ArrayList arr = new ArrayList();

            //for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            //{
            //    Hashtable ht = new Hashtable();
            //    ht["objectid"] = ds.Tables[0].Rows[i]["objectid"].ToString();
            //    ht["name"] = ds.Tables[0].Rows[i]["name"].ToString();
            //    ht["geo"] = TransformToGeometries(ds.Tables[0].Rows[i]["geo"].ToString(), outSR, "2");
            //    arr.Add(ht);
            //}

            Hashtable ht = new Hashtable();
            ht["displayFieldName"] = "YSDM";

            Hashtable ht_fieldAliases = new Hashtable();
            ht_fieldAliases["SHP_ID"] = "SHP_ID";
            ht["fieldAliases"] = ht_fieldAliases;

            ht["geometryType"] = "esriGeometryPolygon";

            Hashtable ht_spatialReference = new Hashtable();
            ht_spatialReference["wkid"] = outSR;
            ht_spatialReference["latestWkid"] = outSR;
            ht["spatialReference"] = ht_spatialReference;

            ArrayList arr_fields = new ArrayList();
            Hashtable ht_field1 = new Hashtable();
            ht_field1["name"] = "SHP_ID";
            ht_field1["type"] = "esriFieldTypeString";
            ht_field1["alias"] = "SHP_ID";
            ht_field1["length"] = "100";
            arr_fields.Add(ht_field1);
            ht["fields"] = arr_fields;

            ArrayList arr_features = new ArrayList();

            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                //Hashtable ht = new Hashtable();
                //ht["objectid"] = ds.Tables[0].Rows[i]["objectid"].ToString();
                //ht["name"] = ds.Tables[0].Rows[i]["name"].ToString();
                //ht["geo"] = TransformToGeometries(ds.Tables[0].Rows[i]["geo"].ToString(), outSR, "2");
                //arr.Add(ht);
                Hashtable ht_feature = new Hashtable();

                Hashtable ht_feature_attributes = new Hashtable();
                ht_feature_attributes["SHP_ID"] = ds.Tables[0].Rows[i]["objectid"].ToString();
                ht_feature["attributes"] = ht_feature_attributes;

                Hashtable ht_feature_geometry = new Hashtable();

                ht_feature_geometry["type"] = "polygon";
                ArrayList arr_feature_geometry_ring1 = new ArrayList();
                ArrayList arr_feature_geometry_ring2 = new ArrayList();
                string ringsString = ds.Tables[0].Rows[i]["geo"].ToString().Replace("MULTIPOLYGON(((", "").Replace(")))", "");
                string[] ringArr = ringsString.Split(',');

                for (int ii = 0; ii < ringArr.Length; ii++)
                {
                    ArrayList arr_feature_geometry_ring3 = new ArrayList();
                    string[] aa = ringArr[ii].Trim().Split(' ');
                    arr_feature_geometry_ring3.Add(aa[0]);
                    arr_feature_geometry_ring3.Add(aa[1]);
                    arr_feature_geometry_ring2.Add(arr_feature_geometry_ring3);
                }


                arr_feature_geometry_ring1.Add(arr_feature_geometry_ring2);
                ht_feature_geometry["rings"] = arr_feature_geometry_ring1;
                ht_feature_geometry["_ring"] = 0;

                Hashtable ht_feature_geometry_spatialReference = new Hashtable();
                ht_feature_geometry_spatialReference["wkid"] = outSR;
                ht_feature_geometry_spatialReference["latestWkid"] = outSR;
                ht_feature_geometry["spatialReference"] = ht_feature_geometry_spatialReference;

                ht_feature["geometry"] = ht_feature_geometry;

                arr_features.Add(ht_feature);

            }          
            
         
            ht["features"] = arr_features;


            #region 模板
            /*
               {
                   "displayFieldName": "YSDM",
                   "fieldAliases": {
                       "SHP_ID": "SHP_ID"
                   },
                   "geometryType": "esriGeometryPolygon",
                   "spatialReference": {
                       "wkid": 4548,
                       "latestWkid": 4548
                   },
                   "fields": [{
                       "name": "SHP_ID",
                       "type": "esriFieldTypeString",
                       "alias": "SHP_ID",
                       "length": 100
                   }],
                   "features": [
                   {
                       "geometry": 
                       {
                           "type": "polygon",
                           "rings": [
                               [
                                   [506979.10030000005, 4363427.672499999],
                                   [506980.6673999997, 4363426.726199999],
                                   [506982.3375000004, 4363425.6261]

                               ]
                           ],
                           "_ring": 0,
                           "spatialReference": {
                               "wkid": 4548,
                               "latestWkid": 4548
                           }
                       },
                       "attributes": {
                           "SHP_ID": "200123696"
                       }
                   }, 
                   {
                       "geometry": {
                           "type": "polygon",
                           "rings": [
                               [
                                   [506245.11060000025, 4363479.249500001],
                                   [506245.1163999997, 4363479.252],
                                   [506245.16220000014, 4363479.2721]

                               ]
                           ],
                           "_ring": 0,
                           "spatialReference": {
                               "wkid": 4548,
                               "latestWkid": 4548
                           }
                       },
                       "attributes": {
                           "SHP_ID": "200087619"
                       }
                   }]
               }
                            */ 
            #endregion


            string str = JSON.JsonEncode(ht);


            return StreamFromPlainText(str);

        }
        #endregion

        #region arcgis



        /// <summary>
        /// 读取服务信息用的方法--似乎是arcgisjsapi调用时自动调用的，但是干什么用不知道
        /// </summary>
        /// <param name="f"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Stream GenerateArcGISServerInfo(string f, string callback)
        {
            string str = @"{
 ""currentVersion"": 1.0.0,
 ""fullVersion"": ""1.0.0"",
 ""soapUrl"": ""http://127.0.0.1:7080"",
 ""secureSoapUrl"": ""null"",
 ""authInfo"": {
  ""isTokenBasedSecurity"": true,
  ""tokenServicesUrl"": ""http://127.0.0.1:7080"",
  ""shortLivedTokenValidity"": 60
 }
}";
            if (f != null && f.ToLower() == "pjson")
            {
                str = str.Replace("\r\n", "").Replace("\n", "").Replace(" ", "");
            }
            if (callback != null)
            {
                str = callback + "alert('" + str + "');";
            }
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            return StreamFromPlainText(str, true);
        }

        /// <summary>
        /// 似乎是调用服务结束时调用一下？？？
        /// </summary>
        /// <param name="f"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public Stream GenerateArcGISServerEndpointInfo(string f, string callback)
        {
            string folders = string.Empty;
            string services = string.Empty;
            foreach (KeyValuePair<string, GISServiceEntity> kvp in GISServicesDic)
            {
                services += "{\"name\":\"" + kvp.Key + "\",\"type\":\"MapServer\"},\r\n";
            }
            if (!string.IsNullOrEmpty(services))
                services = services.Remove(services.Length - 3);
            string str = @"{""currentVersion"" : 10.01, 
  ""folders"" : [
    " + folders + @"
  ], 
  ""services"" : [
    " + services + @"
  ]
}";
            if (f != null && f.ToLower() == "pjson")
                str = str.Replace("\r\n", "").Replace("\n", "").Replace(" ", "");

            if (callback != null)
            {
                str = callback + "(" + str + ");";
            }
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            return StreamFromPlainText(str, true);
        }

        //public Stream GenerateArcGISServerEndpointInfo1(string f, string callback)
        //{
        //    return GenerateArcGISServerEndpointInfo(f, callback);
        //}

        public Stream GenerateArcGISServiceInfo(string serviceName, string f, string callBack)
        {
            if (GISServicesDic != null)
            {
                string str = string.Empty;
                if (GISServicesDic.ContainsKey(serviceName))
                {
                    WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
                    if (f == "json")
                    {
                        str = GISServicesDic[serviceName].DataSource.TilingScheme.RestResponseArcGISJson;
                    }
                    else if (f == "pjson")
                    {
                        str = GISServicesDic[serviceName].DataSource.TilingScheme.RestResponseArcGISPJson;
                    }
                    else if (f == "jsapi")
                    {
                        WebOperationContext.Current.OutgoingResponse.ContentType = "text/html; charset=utf-8";
                        #region jsapi
                        str = @"<!DOCTYPE html PUBLIC ""-<html>
<head>
<meta http-equiv=""X-UA-Compatible"" content=""IE=7"" />
  <title>ArcGIS JavaScript API: " + GISServicesDic[serviceName].ServiceName + @"</title>
  <link href='http://services.arcgisonline.com/ArcGIS/rest/ESRI.ArcGIS.Rest.css' rel='stylesheet' type='text/css'>
<style type=""text/css"">
  @import ""http://serverapi.arcgisonline.com/jsapi/arcgis/2.8/js/dojo/dijit/themes/tundra/tundra.css"";
html, body { height: 100%; width: 100%; margin: 0; padding: 0; }
      .tundra .dijitSplitContainer-dijitContentPane, .tundra .dijitBorderContainer-dijitContentPane#navtable { 
        PADDING-BOTTOM: 5px; MARGIN: 0px 0px 3px; PADDING-TOP: 0px; BORDER-BOTTOM: #000 1px solid; BORDER-TOP: #000 1px solid; BACKGROUND-COLOR: #E5EFF7;
      }
      .tundra .dijitSplitContainer-dijitContentPane, .tundra .dijitBorderContainer-dijitContentPane#map {
        overflow:hidden; border:solid 1px black; padding: 0;
      }
      #breadcrumbs {
        PADDING-RIGHT: 0px; PADDING-LEFT: 11px; FONT-SIZE: 0.8em; FONT-WEIGHT: bold; PADDING-BOTTOM: 5px; MARGIN: 0px 0px 3px; PADDING-TOP: 0px;
      }
      #help {
        PADDING-RIGHT: 11px; PADDING-LEFT: 0px; FONT-SIZE: 0.70em; PADDING-BOTTOM: 5px; MARGIN: 0px 0px 3px; PADDING-TOP: 3px; 
</style>
<script type=""text/javascript"" src=""http://serverapi.arcgisonline.com/jsapi/arcgis?v=2.8""></script>
<script type=""text/javascript"">
  dojo.require(""esri.map"");
  dojo.require(""dijit.layout.ContentPane"");
  dojo.require(""dijit.layout.BorderContainer"");
  var map;
  function Init() {
    dojo.style(dojo.byId(""map""), { width: dojo.contentBox(""map"").w + ""px"", height: (esri.documentBox.h - dojo.contentBox(""navTable"").h - 40) + ""px"" });
    map = new esri.Map(""map"");
    var layer = new esri.layers.ArcGISTiledMapServiceLayer(""" + GISServicesDic[serviceName].UrlArcGIS + @""");
    map.addLayer(layer);
var resizeTimer;
                            dojo.connect(map, 'onLoad', function(theMap) {
                              dojo.connect(dijit.byId('map'), 'resize', function() {
                                clearTimeout(resizeTimer);
                                resizeTimer = setTimeout(function() {
                                  map.resize();
                                  map.reposition();
                                 }, 500);
                               });
                             });
  }
  dojo.addOnLoad(Init);
</script>
</head>
<body class=""tundra"">
<table style=""width:100%"">
<tr>
<td>
<table id=""navTable"" width=""100%"">
<tbody>
<tr valign=""top"">
<td id=""breadcrumbs"">
ArcGIS JavaScript API: " + GISServicesDic[serviceName].ServiceName + @"
</td>
<td align=""right"" id=""help"">
Built using the  <a href=""http://resources.esri.com/arcgisserver/apis/javascript/arcgis"">ArcGIS JavaScript API</a>
</td>
</tr>
</tbody>
</table>
</td>
</tr>
</table>
<div id=""map"" style=""margin:auto;width:97%;border:1px solid #000;""></div>
</body>
</html>
";
                        #endregion
                    }
                    else
                    {
                        str = "只支持 json/pjson/jsapi 三个参数!比如: http://hostname:port/saragisserver/rest/servicename/MapServer?f=json||pjson||jsapi";
                    }
                    if (callBack != null)
                    {
                        str = callBack + "(" + str + ");";
                    }
                }
                else
                {
                    str = serviceName + " 服务名不存在!";
                }
                return StreamFromPlainText(str);
            }
            return null;
        }
        public Stream GenerateArcGISServiceInfo(string serviceName, string operation, string f, string callBack)
        {
            if (GISServicesDic != null)
            {
                string str = string.Empty;
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = serviceName;
                if (GISServicesDic.ContainsKey(serviceName))
                {
                    str = @"{""error"":{""code"":400,""message"":""Unable to complete  operation."",""details"":[""" + operation + @" operation not supported on this service""]}}";
                    if (callBack != null)
                    {
                        str = callBack + "(" + str + ");";
                    }
                }
                else
                {
                    str = serviceName + " 服务名不存在!";
                }
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
                return new MemoryStream(bytes);
            }
            return null;
        }

        #region 实现arcgis的title服务

        /// <summary>
        /// 实现tile服务
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="level"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public Stream GenerateArcGISTile(string serviceName, string level, string row, string col)
        {
            #region loginfo
            string ip = Utility.GetRequestIPAddress();
            if (!GISServicesDic[serviceName].TitleLog.RequestedIPs.Contains(ip))
            {
                GISServicesDic[serviceName].TitleLog.RequestedIPs.Add(ip);
            }
            GISServicesDic[serviceName].TitleLog.RequestedClientCounts = GISServicesDic[serviceName].TitleLog.RequestedIPs.Count;
            GISServicesDic[serviceName].TitleLog.LastRequestClientIP = ip;
            #endregion
            if (GISServicesDic.ContainsKey(serviceName))
            {
                string suffix = GISServicesDic[serviceName].DataSource.TilingScheme.CacheTileFormat.ToString().ToUpper().Contains("PNG") ? "png" : "jpg";
                WebOperationContext.Current.OutgoingResponse.ContentType = "image/" + suffix;
                WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = serviceName;
                if (GISServicesDic[serviceName].DisableClientCache)
                {
                    WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
                    WebOperationContext.Current.OutgoingResponse.Headers.Add("Pragma", "no-cache");
                }
                else
                {
                    GenerateArcGISTileCheckEtag(level, row, col);
                    GenerateArcGISTileSetEtag(level, row, col);
                }
                byte[] bytes = Task.Factory.StartNew<byte[]>(delegate () { return GenerateArcGISTileStream(serviceName, level, row, col); }).Result;

                if (GISServicesDic.ContainsKey(serviceName))
                {
                    if (bytes != null)
                    {
                        MemoryStream ms = new MemoryStream(bytes);
                        return ms;
                    }
                    else if (GISServicesDic[serviceName].DisplayNoDataTile)
                    {
                        return this.GetType().Assembly.GetManifestResourceStream("sara.gisserver.console.Assets.missing" + GISServicesDic[serviceName].DataSource.TilingScheme.TileCols + "." + suffix);
                    }
                }
            }
            WebOperationContext.Current.OutgoingResponse.SetStatusAsNotFound("tile not exists");
            return null;
        }

        /// <summary>
        /// 实现tile服务的私有方法
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="level"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private byte[] GenerateArcGISTileStream(string serviceName, string level, string row, string col)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (GISServicesDic.ContainsKey(serviceName))
            {
                byte[] bytes = null;
                TileLoadEventArgs tileLEA = new TileLoadEventArgs()
                {
                    Level = int.Parse(level),
                    Row = int.Parse(row),
                    Column = int.Parse(col)
                };
                string key = GISServicesDic[serviceName].Port + serviceName + level + row + col + "_" + GISServicesDic[serviceName].MemcachedValidKey;
                //如果有缓存就直接走缓存了
                if (ServerManager.Memcache != null && ServerManager.Memcache.IsActived && GISServicesDic[serviceName].AllowMemCache && ServerManager.Memcache.MC.KeyExists(key))
                {
                    bytes = (byte[])ServerManager.Memcache.MC.Get(key);
                    if (bytes != null)
                    {

                        GISServicesDic[serviceName].TitleLog.OutputTileCountMemcached++;
                        GISServicesDic[serviceName].TitleLog.OutputTileTotalTime += sw.Elapsed.TotalMilliseconds;
                        tileLEA.GeneratedMethod = TileGeneratedSource.FromMemcached;
                    }
                }
                //如果是在线地图或者RasterImage则走这段代码
                if (bytes == null && (GISServicesDic[serviceName].DataSource.IsOnlineMap || GISServicesDic[serviceName].DataSource is DataSourceRasterImage))
                {
                    bytes = GISServicesDic[serviceName].DataSource.GetTileBytesFromLocalCache(int.Parse(level), int.Parse(row), int.Parse(col));
                    if (bytes != null)
                    {

                        GISServicesDic[serviceName].TitleLog.OutputTileCountFileCache++;
                        GISServicesDic[serviceName].TitleLog.OutputTileTotalTime += sw.Elapsed.TotalMilliseconds;
                        tileLEA.GeneratedMethod = TileGeneratedSource.FromFileCache;
                    }
                }
                //否则采用GetTileBytes方法来读，由于采用了继承datasource，所以直接进入arcgistilepackage的GetTileBytes方法
                if (bytes == null)
                {
                    bytes = GISServicesDic[serviceName].DataSource.GetTileBytes(int.Parse(level), int.Parse(row), int.Parse(col));
                    GISServicesDic[serviceName].TitleLog.OutputTileCountDynamic++;
                    GISServicesDic[serviceName].TitleLog.OutputTileTotalTime += sw.Elapsed.TotalMilliseconds;
                    tileLEA.GeneratedMethod = TileGeneratedSource.DynamicOutput;
                }
                //设置颜色
                if (GISServicesDic[serviceName].Style != VisualStyle.None)
                {
                    bytes = sara.gisserver.console.gis.util.Utility.MakeShaderEffect(bytes, GISServicesDic[serviceName].Style);
                }

                if (GISServicesDic[serviceName].DataSource.TileLoaded != null)
                {
                    tileLEA.TileBytes = bytes;
                    GISServicesDic[serviceName].DataSource.TileLoaded(GISServicesDic[serviceName].DataSource, tileLEA);
                }
                //写入缓存
                if (ServerManager.Memcache != null && ServerManager.Memcache.IsActived && GISServicesDic[serviceName].AllowMemCache)
                {
                    ServerManager.Memcache.MC.Set(key, bytes);
                }

                sw.Stop();
                return bytes;
            }
            return null;
        }

        private void GenerateArcGISTileCheckEtag(string level, string row, string col)
        {
            if (WebOperationContext.Current.IncomingRequest.Headers[HttpRequestHeader.IfNoneMatch] != null)
            {
                string etag = WebOperationContext.Current.IncomingRequest.Headers[HttpRequestHeader.IfNoneMatch].Split(new string[] { "\"" }, StringSplitOptions.RemoveEmptyEntries)[0]; try
                {
                    string oriEtag = Encoding.UTF8.GetString(Convert.FromBase64String(etag));
                    if (oriEtag == level + row + col)
                    {
                        WebOperationContext.Current.OutgoingResponse.SuppressEntityBody = true; WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotModified;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("检查请求的etag错误.\r\n" + e.Message);
                }
            }
        }
        private void GenerateArcGISTileSetEtag(string level, string row, string col)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.Expires, DateTime.Now.AddHours(24).ToUniversalTime().ToString("r")); string oriEtag = level + row + col;
            string etag = Convert.ToBase64String(Encoding.UTF8.GetBytes(oriEtag));
            WebOperationContext.Current.OutgoingResponse.SetETag(etag);
        }
        #endregion

        #endregion

        #region WMT
        public Stream GenerateWMTSTileRESTful(string serviceName, string version, string layer, string style, string tilematrixset, string tilematrix, string row, string col, string format)
        {
            if (GISServicesDic == null || !GISServicesDic.ContainsKey(serviceName))
                return null;
            string suffix = GISServicesDic[serviceName].DataSource.TilingScheme.CacheTileFormat.ToString().ToUpper().Contains("PNG") ? "png" : "jpg";
            if (!string.Equals(version, "1.0.0") || !string.Equals(serviceName, layer) || !string.Equals(suffix, format))
                return null;
            return GenerateArcGISTile(serviceName, tilematrix, row, col);
        }

        public Stream GenerateWMTSTileKVP(string serviceName, string version, string layer, string style, string tilematrixset, string tilematrix, string row, string col, string format)
        {
            if (GISServicesDic == null || !GISServicesDic.ContainsKey(serviceName))
                return null;
            string suffix = GISServicesDic[serviceName].DataSource.TilingScheme.CacheTileFormat.ToString().ToUpper().Contains("PNG") ? "png" : "jpg";
            if (!string.Equals(version, "1.0.0") || !string.Equals(serviceName, layer) || !string.Equals(suffix, format.Split(new char[] { '/' })[1]))
                return null;
            return GenerateArcGISTile(serviceName, tilematrix, row, col);
        }

        public Stream GenerateWMTSCapabilitiesRESTful(string serviceName, string version)
        {
            if (GISServicesDic == null)
                return null;
            if (!GISServicesDic.ContainsKey(serviceName))
                throw new WebFaultException<string>(string.Format("The '{0}' service does not exist!", serviceName), HttpStatusCode.BadRequest);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = serviceName;
            string result;
            string key = GISServicesDic[serviceName].Port + serviceName + "WMTSCapabilities" + "_" + GISServicesDic[serviceName].MemcachedValidKey;
            string wmtsVersion = "1.0.0";
            if (!string.Equals(version, wmtsVersion))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
                result = @"<?xml version=""1.0"" encoding=""utf-8"" ?> 
<result>
Invalid version!
</result>";
            }
            else
            {
                string tileMatrixSetName = "default028mm";
                string tileFormat = GISServicesDic[serviceName].DataSource.TilingScheme.CacheTileFormat.ToString().ToLower().Contains("png") ? "png" : "jpg";
                GISServiceEntity service = GISServicesDic[serviceName];
                if (ServerManager.Memcache != null && ServerManager.Memcache.IsActived && GISServicesDic[serviceName].AllowMemCache && ServerManager.Memcache.MC.KeyExists(key))
                    return new MemoryStream((byte[])ServerManager.Memcache.MC.Get(key));
                double INCHES_PER_METER = 39.37;
                double PIXEL_SIZE = 0.00028; double DPI_WMTS = 1.0 / INCHES_PER_METER / PIXEL_SIZE; Envelope wgs84boundingbox = null;
                if (Math.Abs(service.DataSource.TilingScheme.TileOrigin.X) < 600)
                {
                    wgs84boundingbox = service.DataSource.TilingScheme.FullExtent;
                }
                else if (service.DataSource.TilingScheme.WKID == 102100 || service.DataSource.TilingScheme.WKID == 102113 || service.DataSource.TilingScheme.WKID == 3857)
                {
                    Point geoLowerLeft = Utility.WebMercatorToGeographic(service.DataSource.TilingScheme.FullExtent.LowerLeft);
                    Point geoUpperRight = Utility.WebMercatorToGeographic(service.DataSource.TilingScheme.FullExtent.UpperRight);

                    wgs84boundingbox = new Envelope(geoLowerLeft.X, geoLowerLeft.Y, geoUpperRight.X, geoUpperRight.Y);
                }
                bool isGoogleMapsCompatible = (service.DataSource.TilingScheme.WKID == 102100 || service.DataSource.TilingScheme.WKID == 102113 || service.DataSource.TilingScheme.WKID == 3857) && service.DataSource.TilingScheme.TileCols == 256 && service.DataSource.TilingScheme.TileRows == 256 && service.DataSource.TilingScheme.DPI == 96;
                XNamespace def = "http://www.opengis.net/wmts/1.0";
                XNamespace ows = "http://www.opengis.net/ows/1.1";
                XNamespace xlink = "http://www.w3.org/1999/xlink";
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
                XNamespace gml = "http://www.opengis.net/gml";
                XNamespace schemaLocation = "http://www.opengis.net/wmts/1.0 http://schemas.opengis.net/wmts/1.0/wmtsGetCapabilities_response.xsd";
                XElement root = new XElement(def + "Capabilities",
                        new XAttribute("xmlns", def),
                        new XAttribute(XNamespace.Xmlns + "ows", ows),
                        new XAttribute(XNamespace.Xmlns + "xlink", xlink),
                        new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                        new XAttribute(XNamespace.Xmlns + "gml", gml),
                        new XAttribute(xsi + "schemaLocation", schemaLocation),
                        new XAttribute("version", wmtsVersion),
                        new XComment(" Service Identification "),
                        new XElement(ows + "ServiceIdentification",
                            new XElement(ows + "Title", serviceName),
                            new XElement(ows + "ServiceType", "OGC WMTS"),
                            new XElement(ows + "ServiceTypeVersion", wmtsVersion)),
                        new XElement(ows + "ServiceProvider",
                            new XElement(ows + "ProviderName", serviceName),
                            new XElement(ows + "ProviderSite",
                                new XAttribute(xlink + "href", "https://geopbs.codeplex.com/")),
                            new XElement(ows + "ServiceContact",
                                new XElement(ows + "IndividualName", "diligentpig"))),
                        new XComment(" Operations Metadata "),
                        new XElement(ows + "OperationsMetadata",
                            new XElement(ows + "Operation",
                                new XAttribute("name", "GetCapabilities"),
                                new XElement(ows + "DCP",
                                    new XElement(ows + "HTTP",
                                        new XElement(ows + "Get",
                                            new XAttribute(xlink + "href", string.Format("{0}/WMTS/{1}/WMTSCapabilities.xml", service.UrlArcGIS, wmtsVersion)),
                                            new XElement(ows + "Constraint",
                                                new XAttribute("name", "GetEncoding"),
                                                new XElement(ows + "AllowedValues",
                                                    new XElement(ows + "Value", "RESTful")))),
                                        new XElement(ows + "Get",
                                            new XAttribute(xlink + "href", string.Format("{0}/WMTS?", service.UrlArcGIS)),
                                            new XElement(ows + "Constraint",
                                                new XAttribute("name", "GetEncoding"),
                                                new XElement(ows + "AllowedValues",
                                                    new XElement(ows + "Value", "KVP"))))
                                                    ))),
                            new XElement(ows + "Operation",
                                new XAttribute("name", "GetTile"),
                                new XElement(ows + "DCP",
                                    new XElement(ows + "HTTP",
                                        new XElement(ows + "Get",
                                            new XAttribute(xlink + "href", string.Format("{0}/WMTS/tile/{1}/", service.UrlArcGIS, wmtsVersion)),
                                            new XElement(ows + "Constraint",
                                                new XAttribute("name", "GetEncoding"),
                                                new XElement(ows + "AllowedValues",
                                                    new XElement(ows + "Value", "RESTful")))),
                                       new XElement(ows + "Get",
                                            new XAttribute(xlink + "href", string.Format("{0}/WMTS?", service.UrlArcGIS)),
                                            new XElement(ows + "Constraint",
                                                new XAttribute("name", "GetEncoding"),
                                                new XElement(ows + "AllowedValues",
                                                    new XElement(ows + "Value", "KVP"))))
                                                    )))),
                        new XElement(def + "Contents",
                            new XComment("Layer"),
                            new XElement(def + "Layer",
                                new XElement(ows + "Title", serviceName),
                                new XElement(ows + "Identifier", serviceName),
                                new XElement(ows + "BoundingBox",
                                    new XAttribute("crs", string.Format("urn:ogc:def:crs:EPSG::{0}", service.DataSource.TilingScheme.WKID)),
                                    new XElement(ows + "LowerCorner", service.DataSource.TilingScheme.FullExtent.LowerLeft.X + " " + service.DataSource.TilingScheme.FullExtent.LowerLeft.Y),
                                    new XElement(ows + "UpperCorner", service.DataSource.TilingScheme.FullExtent.UpperRight.X + " " + service.DataSource.TilingScheme.FullExtent.UpperRight.Y)),
                                                                wgs84boundingbox != null ?
                                new XElement(ows + "WGS84BoundingBox",
                                    new XAttribute("crs", "urn:ogc:def:crs:OGC:2:84"),
                                    new XElement(ows + "LowerCorner", wgs84boundingbox.LowerLeft.X + " " + wgs84boundingbox.LowerLeft.Y),
                                    new XElement(ows + "UpperCorner", wgs84boundingbox.UpperRight.X + " " + wgs84boundingbox.UpperRight.Y)) : null,
                                new XElement(def + "Style",
                                    new XAttribute("isDefault", "true"),
                                    new XElement(ows + "Title", "Default Style"),
                                    new XElement(ows + "Identifier", "default")),
                                new XElement(def + "Format", "image/" + tileFormat),
                                new XElement(def + "TileMatrixSetLink",
                                    new XElement(def + "TileMatrixSet", tileMatrixSetName)),
                                new XElement(def + "TileMatrixSetLink",
                                    new XElement(def + "TileMatrixSet", "nativeTileMatrixSet")),
                                                                isGoogleMapsCompatible ?
                                new XElement(def + "TileMatrixSetLink",
                                    new XElement(def + "TileMatrixSet", "GoogleMapsCompatible")) : null,
                                new XElement(def + "ResourceURL",
                                    new XAttribute("format", "image/" + tileFormat),
                                    new XAttribute("resourceType", "tile"),
                                    new XAttribute("template", string.Format("{0}/WMTS/tile/{1}/{2}/{{Style}}/{{TileMatrixSet}}/{{TileMatrix}}/{{TileRow}}/{{TileCol}}.{3}", service.UrlArcGIS, wmtsVersion, serviceName, tileFormat)))),
                            new XComment("TileMatrixSet"),
                                                        new XElement(def + "TileMatrixSet",
                                new XElement(ows + "Title", "Default TileMatrix using 0.28mm"),
                                new XElement(ows + "Abstract", "The tile matrix set that has scale values calculated based on the dpi defined by OGC specification (dpi assumes 0.28mm as the physical distance of a pixel)."),
                                new XElement(ows + "Identifier", tileMatrixSetName),
                                                    new XElement(ows + "SupportedCRS", string.Format("urn:ogc:def:crs:EPSG::{0}", service.DataSource.TilingScheme.WKID)),
                                from lod in service.DataSource.TilingScheme.LODs
                                let coords = getBoundaryTileCoords(service.DataSource.TilingScheme, lod)
                                select new XElement(def + "TileMatrix",
                                    new XElement(ows + "Identifier", lod.LevelID),
                                                                                                                                                new XElement(def + "ScaleDenominator", (lod.Scale * 25.4) / (0.28 * service.DataSource.TilingScheme.DPI)),
                                                                        new XElement(def + "TopLeftCorner", UseLatLon(service.DataSource.TilingScheme.WKID) ? service.DataSource.TilingScheme.TileOrigin.Y + " " + service.DataSource.TilingScheme.TileOrigin.X : service.DataSource.TilingScheme.TileOrigin.X + " " + service.DataSource.TilingScheme.TileOrigin.Y),
                                    new XElement(def + "TileWidth", service.DataSource.TilingScheme.TileCols),
                                    new XElement(def + "TileHeight", service.DataSource.TilingScheme.TileRows),
                                                                        new XElement(def + "MatrixWidth", coords[3] - coords[1] + 1),
                                    new XElement(def + "MatrixHeight", coords[2] - coords[0] + 1))),
                                                        new XElement(def + "TileMatrixSet",
                                new XElement(ows + "Title", "Native TiledMapService TileMatrixSet"),
                                new XElement(ows + "Abstract", string.Format("the tile matrix set that has scale values calculated based on the dpi defined by ArcGIS Server tiled map service. The current tile dpi is {0}", service.DataSource.TilingScheme.DPI)),
                                new XElement(ows + "Identifier", "nativeTileMatrixSet"),
                                new XElement(ows + "SupportedCRS", string.Format("urn:ogc:def:crs:EPSG::{0}", service.DataSource.TilingScheme.WKID)),
                                from lod in service.DataSource.TilingScheme.LODs
                                let coords = getBoundaryTileCoords(service.DataSource.TilingScheme, lod)
                                select new XElement(def + "TileMatrix",
                                    new XElement(ows + "Identifier", lod.LevelID),
                                    new XElement(def + "ScaleDenominator", lod.Scale),
                                    new XElement(def + "TopLeftCorner", UseLatLon(service.DataSource.TilingScheme.WKID) ? service.DataSource.TilingScheme.TileOrigin.Y + " " + service.DataSource.TilingScheme.TileOrigin.X : service.DataSource.TilingScheme.TileOrigin.X + " " + service.DataSource.TilingScheme.TileOrigin.Y),
                                    new XElement(def + "TileWidth", service.DataSource.TilingScheme.TileCols),
                                    new XElement(def + "TileHeight", service.DataSource.TilingScheme.TileRows),
                                    new XElement(def + "MatrixWidth", coords[3] - coords[1] + 1),
                                    new XElement(def + "MatrixHeight", coords[2] - coords[0] + 1))),
                                                        isGoogleMapsCompatible ?
                            new XElement(def + "TileMatrixSet",
                                new XElement(ows + "Title", "GoogleMapsCompatible"),
                                new XElement(ows + "Abstract", "the wellknown 'GoogleMapsCompatible' tile matrix set defined by OGC WMTS specification"),
                                new XElement(ows + "Identifier", "GoogleMapsCompatible"),
                                new XElement(ows + "SupportedCRS", "urn:ogc:def:crs:EPSG:6.18:3:3857"),
                                new XElement(def + "WellKnownScaleSet", "urn:ogc:def:wkss:OGC:1.0:GoogleMapsCompatible"),
                                from level in new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }
                                select new XElement(def + "TileMatrix",
                                    new XElement(ows + "Identifier", level),
                                    new XElement(def + "ScaleDenominator", 559082264.0287178 / Math.Pow(2, level)),
                                    new XElement(def + "TopLeftCorner", "-20037508.34278925 20037508.34278925"),
                                    new XElement(def + "TileWidth", "256"),
                                    new XElement(def + "TileHeight", "256"),
                                    new XElement(def + "MatrixWidth", Math.Pow(2, level)),
                                    new XElement(def + "MatrixHeight", Math.Pow(2, level)))) : null
                            ),
                        new XElement(def + "ServiceMetadataURL",
                            new XAttribute(xlink + "href", string.Format("{0}/WMTS/{1}/WMTSCapabilities.xml", service.UrlArcGIS, version)))
                        );
                result = root.ToString();
            }
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
            if (ServerManager.Memcache != null && ServerManager.Memcache.IsActived && GISServicesDic[serviceName].AllowMemCache)
                ServerManager.Memcache.MC.Set(key, bytes);
            return new MemoryStream(bytes);
        }

        public Stream GenerateWMTSCapabilitiesRedirect(string serviceName)
        {
            if (GISServicesDic.ContainsKey(serviceName))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Redirect;
                WebOperationContext.Current.OutgoingResponse.Location = GISServicesDic[serviceName].UrlWMTS + "/1.0.0/WMTSCapabilities.xml";
            }
            return null;
        }

        public Stream GenerateWMTSCapabilitiesKVP(string serviceName, string version)
        {
            return GenerateWMTSCapabilitiesRESTful(serviceName, version);
        }

        #region wmts private method
        private static readonly int[,] _latLongCrsRanges = new[,]
                                                       {
                                                                   {4001, 4999},
                                                                   {2044, 2045}, {2081, 2083}, {2085, 2086}, {2093, 2093},
                                                                   {2096, 2098}, {2105, 2132}, {2169, 2170}, {2176, 2180},
                                                                   {2193, 2193}, {2200, 2200}, {2206, 2212}, {2319, 2319},
                                                                   {2320, 2462}, {2523, 2549}, {2551, 2735}, {2738, 2758},
                                                                   {2935, 2941}, {2953, 2953}, {3006, 3030}, {3034, 3035},
                                                                   {3058, 3059}, {3068, 3068}, {3114, 3118}, {3126, 3138},
                                                                   {3300, 3301}, {3328, 3335}, {3346, 3346}, {3350, 3352},
                                                                   {3366, 3366}, {3416, 3416}, {20004, 20032}, {20064, 20092},
                                                                   {21413, 21423}, {21473, 21483}, {21896, 21899}, {22171, 22177},
                                                                   {22181, 22187}, {22191, 22197}, {25884, 25884}, {27205, 27232},
                                                                   {27391, 27398}, {27492, 27492}, {28402, 28432}, {28462, 28492},
                                                                   {30161, 30179}, {30800, 30800}, {31251, 31259}, {31275, 31279},
                                                                   {31281, 31290}, {31466, 31700}
                                                               };
        private static bool UseLatLon(int wkid)
        {
            int length = _latLongCrsRanges.Length / 2;
            for (int count = 0; count < length; count++)
            {
                if (wkid >= _latLongCrsRanges[count, 0] && wkid <= _latLongCrsRanges[count, 1])
                    return true;
            }
            return false;
        }
        private double GetScale(double resolution, double dpi, TilingScheme ts)
        {
            if (Math.Abs(ts.TileOrigin.X) > 600) return resolution * dpi / 2.54 * 100;
            else
            {
                double meanY = (ts.FullExtent.YMax + ts.FullExtent.YMin) / 2;
                double R = 6378137 * Math.Cos(meanY / 180 * 3.14);
                double dgreeResolution = 2 * 3.14 * R / 360;
                return dgreeResolution * resolution * dpi / 2.54 * 100;
            }
        }
        public static int[] getBoundaryTileCoords(TilingScheme tilingScheme, LODInfo lod)
        {
            double x = tilingScheme.TileOrigin.X;
            double y = tilingScheme.TileOrigin.Y;
            int rows = tilingScheme.TileRows;
            int cols = tilingScheme.TileCols;
            double resolution = lod.Resolution;
            double tileMapW = cols * resolution;
            double tileMapH = rows * resolution;

            double exmin = tilingScheme.FullExtent.XMin;
            double eymin = tilingScheme.FullExtent.YMin;
            double exmax = tilingScheme.FullExtent.XMax;
            double eymax = tilingScheme.FullExtent.YMax;

            int startRow = (int)Math.Floor((y - eymax) / tileMapH);
            int startCol = (int)Math.Floor((exmin - x) / tileMapW);
            int endRow = (int)Math.Floor((y - eymin) / tileMapH);
            int endCol = (int)Math.Floor((exmax - x) / tileMapW);
            return new int[] { startRow < 0 ? 0 : startRow, startCol < 0 ? 0 : startCol, endRow < 0 ? 0 : endRow, endCol < 0 ? 0 : endCol };
        }
        #endregion 
        #endregion

        #region test

        public Stream gettest(string name, string callBack)
        {
           
            string str = JSON.JsonEncode(name);
         
            if (callBack != null)
            {
                str = callBack + "(" + str + ");";
            }
            return StreamFromPlainText(str);
        }


        public string posttest(string name)
        {
            return "{cc:dd}";
        }
        #endregion

        #region admin api
        private string AuthenticateAndParseParams(Stream requestBody, out Hashtable ht, bool allowEmptyRequestBody = false)
        {
            ht = null;
            string authResult = string.Empty;
            if (WebOperationContext.Current.IncomingRequest.Method != "POST")
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.MethodNotAllowed;
                authResult = @"{
    ""success"": false,
    ""message"": ""This operation is only supported via POST!""
}";
                return authResult;
            }
            if (WebOperationContext.Current.IncomingRequest.Headers[HttpRequestHeader.Authorization] == null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                authResult = @"{
    ""success"": false,
    ""message"": ""Authorization required!""
}";
                return authResult;
            }
            byte[] bytes = null;
            try
            {
                bytes = Convert.FromBase64String(WebOperationContext.Current.IncomingRequest.Headers[HttpRequestHeader.Authorization]);
            }
            catch (Exception)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                authResult = @"{
    ""success"": false,
    ""message"": ""The autherization header is not base64 encoding""
}";
                return authResult;
            }
            string[] userandpwd = Encoding.UTF8.GetString(bytes).Split(new char[] { ':' });
            if (userandpwd.Length != 2)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                authResult = @"{
    ""success"": false,
    ""message"": ""The autherization header does not match the pattern username:password""
}";
                return authResult;
            }
            if (!sara.gisserver.console.gis.util.Utility.IsUserAdmin(userandpwd[0], userandpwd[1], "localhost"))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                authResult = @"{
    ""success"": false,
    ""message"": ""The user is not an administrator on PBS running machine.""
}";
                return authResult;
            }
            if (allowEmptyRequestBody && requestBody == null)
                return string.Empty;
            if (requestBody == null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                authResult = @"{
    ""success"": false,
    ""message"": ""Request body is empty.""
}";
                return authResult;
            }
            string strRequest;
            using (StreamReader sr = new StreamReader(requestBody))
            {
                strRequest = sr.ReadToEnd();
            }
            object o = JSON.JsonDecode(strRequest) as Hashtable;
            if (o == null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                authResult = @"{
    ""success"": false,
    ""message"": ""Request parameters are not valid json array.""
}";
                return authResult;
            }
            ht = o as Hashtable;
            return string.Empty;
        }

        public Stream AddPBSService(Stream requestBody)
        {
            string result = string.Empty;
            Hashtable htParams = null;
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            result = AuthenticateAndParseParams(requestBody, out htParams);
            if (result != string.Empty)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
            else
            {
                string name, datasourcepath, tilingschemepath;
                int port;
                string strDataSourceType;
                bool allowmemorycache, disableclientcache, displaynodatatile;
                VisualStyle visualstyle;
                #region parsing params
                try
                {
                    if (htParams["name"] == null || htParams["port"] == null || htParams["dataSourceType"] == null || htParams["dataSourcePath"] == null)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                        result = @"{
    ""success"": false,
    ""message"": ""name/port/datasourcetype/datasourcepath can not be null.""
}";
                        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                    }
                    name = htParams["name"].ToString();
                    port = int.Parse(htParams["port"].ToString());
                    strDataSourceType = htParams["dataSourceType"].ToString();
                    datasourcepath = htParams["dataSourcePath"].ToString();
                    allowmemorycache = htParams["allowMemoryCache"] == null ? true : (bool)htParams["allowMemoryCache"];
                    disableclientcache = htParams["disableClientCache"] == null ? false : (bool)htParams["disableClientCache"];
                    displaynodatatile = htParams["displayNodataTile"] == null ? false : (bool)htParams["displayNodataTile"];
                    visualstyle = htParams["visualStyle"] == null ? VisualStyle.None : (VisualStyle)Enum.Parse(typeof(VisualStyle), htParams["visualStyle"].ToString());
                    tilingschemepath = htParams["tilingSchemePath"] == null ? null : htParams["tilingSchemePath"].ToString();
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                    result = @"{
    ""success"": false,
    ""message"": ""request parameters parsing error! " + e.Message + @"""
}";
                    return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                }
                #endregion
                string str = string.Empty;
                str = ServerManager.CreateGISService(name, port, strDataSourceType, datasourcepath, allowmemorycache, disableclientcache, displaynodatatile, visualstyle, tilingschemepath);
                if (str != string.Empty)
                    result = @"{
                    ""success"": false,
                    ""message"": """ + str + @"""
                }";
                else
                    result = @"{
                    ""success"": true,
                    ""message"": " + ServerManager.GetServiceEntity(port, name).DataSource.TilingScheme.RestResponseArcGISJson + @"
                }";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
        }

        public Stream DeletePBSService(Stream requestBody)
        {
            string result = string.Empty;
            Hashtable htParams = null;
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            result = AuthenticateAndParseParams(requestBody, out htParams);
            if (result != string.Empty)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
            else
            {
                string name;
                int port;
                #region parsing params
                try
                {
                    if (htParams["name"] == null || htParams["port"] == null)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                        result = @"{
    ""success"": false,
    ""message"": ""name/port can not be null.""
}";
                        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                    }
                    name = htParams["name"].ToString();
                    port = int.Parse(htParams["port"].ToString());
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                    result = @"{
    ""success"": false,
    ""message"": ""request parameters parsing error! " + e.Message + @"""
}";
                    return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                }
                #endregion
                string str = string.Empty;
                str = ServerManager.DeleteService(port, name);
                if (str != string.Empty)
                    result = @"{
                    ""success"": false,
                    ""message"": """ + str + @"""
                }";
                else
                    result = @"{
                    ""success"": true,
                    ""message"": ""success""
                }";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
        }

        public Stream ClearMemcacheByService(Stream requestBody)
        {
            string result = string.Empty;
            Hashtable htParams = null;
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            result = AuthenticateAndParseParams(requestBody, out htParams);
            if (result != string.Empty)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
            else
            {
                if (ServerManager.Memcache == null)
                {
                    result = @"{
                    ""success"": false,
                    ""message"": ""The MemCache capability has not been started yet.""
                }";
                    return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                }

                string name;
                int port;
                #region parsing params
                try
                {
                    if (htParams["name"] == null || htParams["port"] == null)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                        result = @"{
    ""success"": false,
    ""message"": ""name/port can not be null.""
}";
                        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                    }
                    name = htParams["name"].ToString();
                    port = int.Parse(htParams["port"].ToString());
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                    result = @"{
    ""success"": false,
    ""message"": ""request parameters parsing error! " + e.Message + @"""
}";
                    return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                }
                #endregion
                string str = string.Empty;
                str = ServerManager.Memcache.InvalidateServiceMemcache(port, name);
                if (str != string.Empty)
                    result = @"{
                    ""success"": false,
                    ""message"": """ + str + @"""
                }";
                else
                    result = @"{
                    ""success"": true,
                    ""message"": ""success""
                }";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
        }

        public Stream EnableMemcache(Stream requestBody)
        {
            string result = string.Empty;
            Hashtable htParams = null;
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            result = AuthenticateAndParseParams(requestBody, out htParams, true);
            if (result != string.Empty)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
            else if (ServerManager.Memcache != null && ServerManager.Memcache.IsActived)
            {
                result = @"{
                    ""success"": true,
                    ""message"": ""memory cache is already enabled.""
                }";
                return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
            }
            else
            {
                int memSize = -1;
                #region parsing params
                if (requestBody == null || (requestBody.CanSeek && requestBody.Length == 0))
                    memSize = 64;
                else
                {
                    try
                    {
                        memSize = htParams["memSize"] == null ? 64 : int.Parse(htParams["memSize"].ToString());
                    }
                    catch (Exception e)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                        result = @"{
    ""success"": false,
    ""message"": ""request parameters parsing error! " + e.Message + @"""
}";
                        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                    }
                }
                #endregion
                try
                {
                    if (ServerManager.Memcache == null)
                        ServerManager.Memcache = new MemCache(memSize);
                    else
                        ServerManager.Memcache.IsActived = true;
                    result = @"{
                    ""success"": true,
                    ""message"": ""success""
                }";
                }
                catch (Exception ex)
                {
                    ServerManager.Memcache = null;
                    result = @"{
                    ""success"": false,
                    ""message"": """ + ex.Message + @"""
                }";
                }
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
        }

        public Stream DisableMemcache(Stream requestBody)
        {
            string result = string.Empty;
            Hashtable htParams = null;
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            result = AuthenticateAndParseParams(requestBody, out htParams, true);
            if (result != string.Empty)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
            else if (ServerManager.Memcache == null || (ServerManager.Memcache != null && !ServerManager.Memcache.IsActived))
            {
                result = @"{
                    ""success"": true,
                    ""message"": ""memory cache is already disabled.""
                }";
                return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
            }
            else
            {
                try
                {
                    if (ServerManager.Memcache != null)
                        ServerManager.Memcache.IsActived = false;
                    result = @"{
                    ""success"": true,
                    ""message"": ""success""
                }";
                }
                catch (Exception ex)
                {
                    ServerManager.Memcache = null;
                    result = @"{
                    ""success"": false,
                    ""message"": """ + ex.Message + @"""
                }";
                }
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
        }

        public Stream ChangeArcGISDynamicMapServiceParams(Stream requestBody)
        {
            string result = string.Empty;
            Hashtable htParams = null;
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain;charset=utf-8";
            result = AuthenticateAndParseParams(requestBody, out htParams);
            if (result != string.Empty)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
            else
            {
                int port;
                string name;
                string layers, layerDefs, time, layerTimeOptions;
                layers = layerDefs = time = layerTimeOptions = string.Empty;
                #region parsing params
                try
                {
                    if (htParams["name"] == null || htParams["port"] == null)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                        result = @"{
    ""success"": false,
    ""message"": ""name/port can not be null.""
}";
                        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                    }
                    name = htParams["name"].ToString();
                    port = int.Parse(htParams["port"].ToString());

                    if (htParams["layers"] != null)
                        layers = htParams["layers"].ToString();
                    if (htParams["layerDefs"] != null)
                        layerDefs = htParams["layerDefs"].ToString();
                    if (htParams["time"] != null)
                        time = htParams["time"].ToString();
                    if (htParams["layerTimeOptions"] != null)
                        layerTimeOptions = htParams["layerTimeOptions"].ToString();
                }
                catch (Exception e)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                    result = @"{
    ""success"": false,
    ""message"": ""request parameters parsing error! " + e.Message + @"""
}";
                    return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(result));
                }
                #endregion
                if (ServerManager.GetServiceEntity(port, name) == null)
                {
                    result = @"{
                    ""success"": false,
                    ""message"": ""service dose not exist.""
                }";
                }
                else if (ServerManager.GetServiceEntity(port, name).DataSource.Type != sara.gisserver.console.gis.datasource.DataSourceTypePredefined.ArcGISDynamicMapService.ToString())
                {
                    result = @"{
                    ""success"": false,
                    ""message"": ""service type is not ArcGISDynamicMapService.""
                }";
                }
                else
                {
                    if (layers != string.Empty)
                        (ServerManager.GetServiceEntity(port, name).DataSource as DataSourceArcGISDynamicMapService).exportParam_layers = layers;
                    if (layerDefs != string.Empty)
                        (ServerManager.GetServiceEntity(port, name).DataSource as DataSourceArcGISDynamicMapService).exportParam_layerDefs = layerDefs;
                    if (time != string.Empty)
                        (ServerManager.GetServiceEntity(port, name).DataSource as DataSourceArcGISDynamicMapService).exportParam_time = time;
                    if (layerTimeOptions != string.Empty)
                        (ServerManager.GetServiceEntity(port, name).DataSource as DataSourceArcGISDynamicMapService).exportParam_layerTimeOptions = layerTimeOptions;
                    result = @"{
                    ""success"": true,
                    ""message"": ""success""
                }";
                }
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return new MemoryStream(bytes);
            }
        }
        #endregion


        /// <summary>
        /// 私有方法，将字符串转为Stream
        /// 支持设置客户端缓存、get方法跨域
        /// </summary>
        /// <param name="content"></param>
        /// <param name="disableCache"></param>
        /// <returns></returns>
        private Stream StreamFromPlainText(string content, bool disableCache = false)
        {
            WebOperationContext.Current.OutgoingResponse.Headers["X-Powered-By"] = sara.gisserver.console.gis.Global.ServerName;
            //sk--增加跨域代码支持
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "POST");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "x-requested-with,content-type");
            if (disableCache)
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Pragma", "no-cache");
            }
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes);
        }

        /// <summary>
        /// 将ST类型坐标系转换为Geometries类型
        /// </summary>
        /// <param name="str">ST类型字符串</param>
        /// <param name="outSR">输出wxid</param>
        /// <param name="outSR">输出Geometries类型 1.esri-Geometries  2.normal-Geometries  3.simple-Geometries 默认为2</param>
        /// <returns></returns>
        public string TransformToGeometries(string str, string outSR, string outtype)
        {
            //判断输出类型正确性与默认值
            if (outtype != "1" && outtype != "2" && outtype != "3")
            {
                outtype = "2";
            }

            string geometryType = str.Substring(0, str.IndexOf('('));
            string resultgeometries = str.Substring(str.IndexOf('('));
            //转换为ersi-Geometries
            if (outtype == "1")
            {
                if (geometryType == "POINT")
                {
                    string[] temp = str.Substring(str.IndexOf('(')).Trim('(').Trim(')').Split(' ');

                    resultgeometries = "{\"geometryType\":\"esriGeometryPoint\",\"geometries\":[{\"x\":" + temp[0] + ",\"y\":" + temp[1] + ",\"spatialReference\":{\"wkid\":" + outSR + "}}]}";

                }
                else
                {
                    switch (geometryType)
                    {
                        case "POLYGON":
                        case "MULTIPOLYGON":
                            if (!resultgeometries.StartsWith("((("))
                            {
                                resultgeometries = "(" + resultgeometries + ")";
                            }
                            break;
                        case "LINESTRING":
                        case "MULTILINESTRING":
                            if (resultgeometries.StartsWith("(("))
                            {
                                resultgeometries = "(" + resultgeometries + ")";

                            }
                            else if (resultgeometries.StartsWith("("))
                            {
                                resultgeometries = "((" + resultgeometries + "))";
                            }

                            resultgeometries = resultgeometries.Replace("), (", ")), ((");
                            break;
                    }


                    resultgeometries = resultgeometries.Replace('(', '[').Replace(')', ']').Replace(", ", "],[").Replace("]],[[", "],[").Replace(' ', ',');
                    switch (geometryType)
                    {
                        case "POLYGON":
                        case "MULTIPOLYGON":
                            resultgeometries = "{\"geometryType\":\"esriGeometryPolygon\",\"geometries\":[{\"rings\":" + resultgeometries + ",\"spatialReference\":{\"wkid\":" + outSR + "}}]}";

                            break;
                        case "LINESTRING":
                        case "MULTILINESTRING":
                            resultgeometries = "{\"geometryType\":\"esriGeometryPolyline\",\"geometries\":[{\"paths\":" + resultgeometries + ",\"spatialReference\":{\"wkid\":" + outSR + "}}]}";

                            break;
                    }

                }
            }
            //转换为normal-Geometries
            else if (outtype == "2")
            {
                if (geometryType == "POINT")
                {
                    string[] temp = str.Substring(str.IndexOf('(')).Trim('(').Trim(')').Split(' ');

                    resultgeometries = "{\"type\":\"point\",\"x\":" + temp[0] + ",\"y\":" + temp[1] + ",\"spatialReference\":{\"wkid\":" + outSR + "}}";

                }
                else
                {
                    switch (geometryType)
                    {
                        case "POLYGON":
                        case "MULTIPOLYGON":
                            if (!resultgeometries.StartsWith("((("))
                            {
                                resultgeometries = "(" + resultgeometries + ")";
                            }
                            break;
                        case "LINESTRING":
                        case "MULTILINESTRING":
                            if (resultgeometries.StartsWith("(("))
                            {
                                resultgeometries = "(" + resultgeometries + ")";

                            }
                            else if (resultgeometries.StartsWith("("))
                            {
                                resultgeometries = "((" + resultgeometries + "))";
                            }

                            resultgeometries = resultgeometries.Replace("), (", ")), ((");
                            break;
                    }


                    resultgeometries = resultgeometries.Replace('(', '[').Replace(')', ']').Replace(", ", "],[").Replace("]],[[", "],[").Replace(' ', ',');
                    switch (geometryType)
                    {
                        case "POLYGON":
                        case "MULTIPOLYGON":
                            resultgeometries = "{\"type\":\"polygon\",\"rings\":" + resultgeometries + ",\"_ring\":0,\"spatialReference\":{\"wkid\":" + outSR + "},\"_centroid\":null,\"_extent\":null}";

                            break;
                        case "LINESTRING":
                        case "MULTILINESTRING":
                            resultgeometries = "{\"type\":\"polyline\",\"paths\":" + resultgeometries + ",\"_path\":0,\"spatialReference\":{\"wkid\":" + outSR + "}}";

                            break;
                    }

                }
            }
            //转换为simple-Geometries
            else if (outtype == "3")
            {
                if (geometryType == "POINT")
                {
                    string[] temp = str.Substring(str.IndexOf('(')).Trim('(').Trim(')').Split(' ');

                    resultgeometries = "[{\"x\":" + temp[0] + ",\"y\":" + temp[1] + ",\"spatialReference\":{\"wkid\":" + outSR + "}}]";

                }
                else
                {
                    switch (geometryType)
                    {
                        case "POLYGON":
                        case "MULTIPOLYGON":
                            if (!resultgeometries.StartsWith("((("))
                            {
                                resultgeometries = "(" + resultgeometries + ")";
                            }
                            break;
                        case "LINESTRING":
                        case "MULTILINESTRING":
                            if (resultgeometries.StartsWith("(("))
                            {
                                resultgeometries = "(" + resultgeometries + ")";

                            }
                            else if (resultgeometries.StartsWith("("))
                            {
                                resultgeometries = "((" + resultgeometries + "))";
                            }

                            resultgeometries = resultgeometries.Replace("), (", ")), ((");
                            break;
                    }


                    resultgeometries = resultgeometries.Replace('(', '[').Replace(')', ']').Replace(", ", "],[").Replace("]],[[", "],[").Replace(' ', ',');
                    switch (geometryType)
                    {
                        case "POLYGON":
                        case "MULTIPOLYGON":
                            resultgeometries = "[{\"rings\":" + resultgeometries + ",\"spatialReference\":{\"wkid\":" + outSR + "}}]";

                            break;
                        case "LINESTRING":
                        case "MULTILINESTRING":
                            resultgeometries = "[{\"paths\":" + resultgeometries + ",\"spatialReference\":{\"wkid\":" + outSR + "}}]";

                            break;
                    }

                }
            }
            return resultgeometries;
        }


        /// <summary>
        /// 将Geometries类型坐标系转换为ST类型
        /// </summary>
        /// <param name="str">Geometries类型字符串</param>
        /// <returns></returns>
        public string TransformToST(string geometries)
        {
            geometries = geometries.TrimStart('[').TrimEnd(']');
            //geometries = "{\"type\":\"point\",\"x\":13104520.927276257,\"y\":4824302.82476499,\"spatialReference\":{\"wkid\":3857}}";
            Hashtable ht = JSON.JsonDecode(geometries) as Hashtable;
            //结果字符串
            string resultgeometries = "";
            //esri-geometries转换
            if (ht.ContainsKey("geometryType"))
            {
                //全局变量
                ArrayList geometriesArray = ht["geometries"] as ArrayList;
                Hashtable geometry = geometriesArray[0] as Hashtable;
                ArrayList pointsarray;
                string str = "";


                switch (ht["geometryType"].ToString())
                {
                    //面
                    case "esriGeometryPolygon":

                        pointsarray = (geometry["rings"] as ArrayList) as ArrayList;

                        if (pointsarray.Count > 1)
                        {
                            str = "'MULTIPOLYGON(((";
                        }
                        else
                        {
                            str = "'POLYGON((";
                        }

                        for (int ii = 0; ii < pointsarray.Count; ii++)
                        {
                            ArrayList points = pointsarray[ii] as ArrayList;

                            for (int iii = 0; iii < points.Count; iii++)
                            {
                                if (iii != 0)
                                {
                                    str += ",";
                                }
                                ArrayList point = points[iii] as ArrayList;
                                str += point[0].ToString() + " " + point[1].ToString();
                            }

                            if (pointsarray.Count > 1 && ii != pointsarray.Count - 1)
                            {
                                str += ")),((";
                            }

                        }

                        if (pointsarray.Count > 1)
                        {
                            str += ")))'";
                        }
                        else
                        {
                            str += "))'";
                        }
                        resultgeometries = str;

                        break;
                    //线
                    case "esriGeometryPolyline":

                        pointsarray = (geometry["paths"] as ArrayList) as ArrayList;

                        if (pointsarray.Count > 1)
                        {
                            str = "'MULTILINESTRING((";
                        }
                        else
                        {
                            str = "'LINESTRING(";
                        }
                        for (int ii = 0; ii < pointsarray.Count; ii++)
                        {
                            ArrayList points = pointsarray[ii] as ArrayList;

                            for (int iii = 0; iii < points.Count; iii++)
                            {
                                if (iii != 0)
                                {
                                    str += ",";
                                }
                                ArrayList point = points[iii] as ArrayList;
                                str += point[0].ToString() + " " + point[1].ToString();
                            }

                            if (pointsarray.Count > 1 && ii != pointsarray.Count - 1)
                            {
                                str += "),(";
                            }

                        }

                        if (pointsarray.Count > 1)
                        {
                            str += "))'";
                        }
                        else
                        {
                            str += ")'";
                        }

                        resultgeometries = str;
                        break;
                    //点
                    case "esriGeometryPoint":
                        string x = geometry["x"].ToString();
                        string y = geometry["y"].ToString();
                        resultgeometries = $"'POINT({x} {y})'";
                        break;
                }
            }
            //normal-geometries转换
            else if (ht.ContainsKey("type"))
            {
                ArrayList pointsarray;
                string str = "";

                switch (ht["type"].ToString())
                {
                    //面
                    case "polygon":

                        pointsarray = (ht["rings"] as ArrayList) as ArrayList;

                        if (pointsarray.Count > 1)
                        {
                            str = "'MULTIPOLYGON(((";
                        }
                        else
                        {
                            str = "'POLYGON((";
                        }

                        for (int ii = 0; ii < pointsarray.Count; ii++)
                        {
                            ArrayList points = pointsarray[ii] as ArrayList;

                            for (int iii = 0; iii < points.Count; iii++)
                            {
                                if (iii != 0)
                                {
                                    str += ",";
                                }
                                ArrayList point = points[iii] as ArrayList;
                                str += point[0].ToString() + " " + point[1].ToString();
                            }

                            if (pointsarray.Count > 1 && ii != pointsarray.Count - 1)
                            {
                                str += ")),((";
                            }

                        }

                        if (pointsarray.Count > 1)
                        {
                            str += ")))'";
                        }
                        else
                        {
                            str += "))'";
                        }
                        resultgeometries = str;

                        break;
                    //线
                    case "polyline":

                        pointsarray = (ht["paths"] as ArrayList) as ArrayList;

                        if (pointsarray.Count > 1)
                        {
                            str = "'MULTILINESTRING((";
                        }
                        else
                        {
                            str = "'LINESTRING(";
                        }
                        for (int ii = 0; ii < pointsarray.Count; ii++)
                        {
                            ArrayList points = pointsarray[ii] as ArrayList;

                            for (int iii = 0; iii < points.Count; iii++)
                            {
                                if (iii != 0)
                                {
                                    str += ",";
                                }
                                ArrayList point = points[iii] as ArrayList;
                                str += point[0].ToString() + " " + point[1].ToString();
                            }

                            if (pointsarray.Count > 1 && ii != pointsarray.Count - 1)
                            {
                                str += "),(";
                            }

                        }

                        if (pointsarray.Count > 1)
                        {
                            str += "))'";
                        }
                        else
                        {
                            str += ")'";
                        }

                        resultgeometries = str;
                        break;
                    //点
                    case "point":
                        string x = ht["x"].ToString();
                        string y = ht["y"].ToString();
                        resultgeometries = $"'POINT({x} {y})'";
                        break;
                }
            }
            //simple-geometries转换
            else if (ht.ContainsKey("spatialReference"))
            {
                ArrayList pointsarray;
                string str = "";
                if (ht.ContainsKey("rings"))
                {
                    //面
                    pointsarray = (ht["rings"] as ArrayList) as ArrayList;

                    if (pointsarray.Count > 1)
                    {
                        str = "'MULTIPOLYGON(((";
                    }
                    else
                    {
                        str = "'POLYGON((";
                    }

                    for (int ii = 0; ii < pointsarray.Count; ii++)
                    {
                        ArrayList points = pointsarray[ii] as ArrayList;

                        for (int iii = 0; iii < points.Count; iii++)
                        {
                            if (iii != 0)
                            {
                                str += ",";
                            }
                            ArrayList point = points[iii] as ArrayList;
                            str += point[0].ToString() + " " + point[1].ToString();
                        }

                        if (pointsarray.Count > 1 && ii != pointsarray.Count - 1)
                        {
                            str += ")),((";
                        }

                    }

                    if (pointsarray.Count > 1)
                    {
                        str += ")))'";
                    }
                    else
                    {
                        str += "))'";
                    }
                    resultgeometries = str;
                }
                else if (ht.ContainsKey("paths"))
                {
                    //线
                    pointsarray = (ht["paths"] as ArrayList) as ArrayList;

                    if (pointsarray.Count > 1)
                    {
                        str = "'MULTILINESTRING((";
                    }
                    else
                    {
                        str = "'LINESTRING(";
                    }
                    for (int ii = 0; ii < pointsarray.Count; ii++)
                    {
                        ArrayList points = pointsarray[ii] as ArrayList;

                        for (int iii = 0; iii < points.Count; iii++)
                        {
                            if (iii != 0)
                            {
                                str += ",";
                            }
                            ArrayList point = points[iii] as ArrayList;
                            str += point[0].ToString() + " " + point[1].ToString();
                        }

                        if (pointsarray.Count > 1 && ii != pointsarray.Count - 1)
                        {
                            str += "),(";
                        }

                    }

                    if (pointsarray.Count > 1)
                    {
                        str += "))'";
                    }
                    else
                    {
                        str += ")'";
                    }

                    resultgeometries = str;
                }
                else if (ht.ContainsKey("x") && ht.ContainsKey("y"))
                {
                    string x = ht["x"].ToString();
                    string y = ht["y"].ToString();
                    resultgeometries = $"'POINT({x} {y})'";
                }
            }



            return resultgeometries;

        }

    }
}