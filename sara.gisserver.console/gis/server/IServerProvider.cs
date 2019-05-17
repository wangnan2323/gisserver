//****************************************
//Copyright@diligentpig, https://geopbs.codeplex.com
//Please using source code under LGPL license.
//****************************************using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;
using sara.gisserver.console.gis.datasource;

namespace sara.gisserver.console.gis.server
{
    //UriTemplate class in WCF REST Service: Part I
    //http://www.c-sharpcorner.com/UploadFile/dhananjaycoder/1431/

    [ServiceContract(Namespace = "")]
    public interface IServerProvider
    {

        /// <summary>
        /// 获取根节点网页
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "/", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream Root();

        /// <summary>
        /// 获取图片文件
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "/images/{imageName}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetImage(string imageName);

        /// <summary>
        /// 获取跨域文件
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "/clientaccesspolicy.xml", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream ClientAccessPolicyFile();

        /// <summary>
        /// 获取跨域文件
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "/crossdomain.xml", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream CrossDomainFile();



        #region geometryServer


        /// <summary>
        /// project
        /// </summary>
        /// <param name="f"></param>
        /// <param name="outSR"></param>
        /// <param name="inSR"></param>
        /// <param name="geometries"></param>
        /// <returns></returns>
        [OperationContract(Name = "GeometryServerProject")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/Utilities/Geometry/GeometryServer/project?f={f}&outSR={outSR}&inSR={inSR}&geometries={geometries}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GeometryServerProject(string f, string outSR, string inSR, string geometries);

        /// <summary>
        /// simplify
        /// </summary>
        /// <param name="f"></param>
        /// <param name="sr"></param>
        /// <param name="geometries"></param>
        /// <returns></returns>
        [OperationContract(Name = "GeometryServerSimplify")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/Utilities/Geometry/GeometryServer/simplify?f={f}&sr={sr}&geometries={geometries}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GeometryServerSimplify(string f, string sr, string geometries);

        /// <summary>
        /// lengths
        /// </summary>
        /// <param name="f"></param>
        /// <param name="polylines"></param>
        /// <param name="sr"></param>
        /// <param name="lengthUnit"></param>
        /// <param name="geodesic"></param>
        /// <returns></returns>
        [OperationContract(Name = "GeometryServerLengths")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/Utilities/Geometry/GeometryServer/lengths?f={f}&polylines={polylines}&sr={sr}&lengthUnit={lengthUnit}&geodesic={geodesic}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GeometryServerLengths(string f, string polylines, string sr, string lengthUnit, string geodesic);

        /// <summary>
        /// areasAndLengths
        /// </summary>
        /// <param name="f"></param>
        /// <param name="polygons"></param>
        /// <param name="sr"></param>
        /// <param name="lengthUnit"></param>
        /// <param name="areaUnit"></param>
        /// <returns></returns>
        [OperationContract(Name = "GeometryServerAreasAndLengths")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/Utilities/Geometry/GeometryServer/areasAndLengths?f={f}&polygons={polygons}&sr={sr}&lengthUnit={lengthUnit}&areaUnit={areaUnit}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GeometryServerAreasAndLengths(string f, string polygons, string sr, string lengthUnit, string areaUnit);

        /// <summary>
        /// union
        /// </summary>
        /// <param name="f"></param>
        /// <param name="outSR"></param>
        /// <param name="inSR"></param>
        /// <param name="geometries"></param>
        /// <returns></returns>
        [OperationContract(Name = "GeometryServerUnion")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/Utilities/Geometry/GeometryServer/union?f={f}&outSR={outSR}&inSR={inSR}&geometries={geometries}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GeometryServerUnion(string f, string outSR, string inSR, string geometries);



        /// <summary>
        /// query
        /// </summary>
        /// <param name="f"></param>
        /// <param name="servicename"></param>
        /// <param name="layerindex"></param>
        /// <param name="outSR"></param>
        /// <param name="inSR"></param>
        /// <param name="geometry"></param>
        /// <param name="geometryType"></param>
        /// <param name="spatialRel"></param>
        /// <param name="returnGeometry"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        [OperationContract(Name = "GeometryServerQuery")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/{layerindex}/query?f={f}&where={where}&returnGeometry={returnGeometry}&spatialRel={spatialRel}&geometry={geometry}&geometryType={geometryType}&inSR={inSR}&outFields={outFields}&outSR={outSR}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GeometryServerQuery(string f, string servicename,string layerindex, string inSR, string geometry,string geometryType,string spatialRel, string where, string returnGeometry, string outSR,string outFields);

        #endregion

        #region arcgis
        [OperationContract(Name = "GenerateServerInfo")]
        [WebGet(UriTemplate = "/saragisserver/rest/info?f={f}&callback={callback}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateArcGISServerInfo(string f, string callback);

        [OperationContract(Name = "GenerateArcGISServerEndpointInfo")]
        [WebGet(UriTemplate = "/saragisserver/rest/services?f={f}&callback={callback}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateArcGISServerEndpointInfo(string f, string callback);


        /// <summary>
        /// arcgis javascript api 将会调用这个请求
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="f"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        [OperationContract(Name = "GenerateArcGISServiceInfo")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer?f={f}&callback={callback}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateArcGISServiceInfo(string serviceName, string f, string callBack);

        /// <summary>
        /// arcgis.com viewer 将会调用这个请求
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="operation"></param>
        /// <param name="f"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        [OperationContract(Name = "GenerateArcGISServiceInfoIsSupportOperation")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/{operation}?f={f}&callback={callback}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateArcGISServiceInfo(string serviceName, string operation, string f, string callBack);

        /// <summary>
        /// 实现arcgis的tile服务
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="level"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/tile/{level}/{row}/{col}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateArcGISTile(string serviceName, string level, string row, string col); 
        #endregion

        #region WMTS
        /// <summary>
        /// WMTS Capabilities resource
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [OperationContract(Name = "GenerateWMTSCapabilitiesRESTful")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/WMTS/{version}/WMTSCapabilities.xml", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateWMTSCapabilitiesRESTful(string serviceName, string version);

        [OperationContract(Name = "GenerateWMTSCapabilitiesRedirect")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/WMTS", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateWMTSCapabilitiesRedirect(string serviceName);

        [OperationContract(Name = "GenerateWMTSCapabilitiesKVP")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/WMTS?service=WMTS&request=GetCapabilities&version={version}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateWMTSCapabilitiesKVP(string serviceName, string version);

        /// <summary>
        /// WMTS Tile resource
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="version"></param>
        /// <param name="style"></param>
        /// <param name="tilematrixset"></param>
        /// <param name="tilematrix"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        [OperationContract(Name = "GenerateWMTSTileRESTful")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/WMTS/tile/{version}/{layer}/{style}/{tilematrixset}/{tilematrix}/{row}/{col}.{format}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateWMTSTileRESTful(string serviceName, string version, string layer, string style, string tilematrixset, string tilematrix, string row, string col, string format);

        [OperationContract(Name = "GenerateWMTSTileKVP")]
        [WebGet(UriTemplate = "/saragisserver/rest/services/{servicename}/MapServer/WMTS?service=WMTS&request=GetTile&version={version}&layer={layer}&style={style}&tileMatrixSet={tilematrixset}&tileMatrix={tilematrix}&tileRow={row}&tileCol={col}&format={format}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GenerateWMTSTileKVP(string serviceName, string version, string layer, string style, string tilematrixset, string tilematrix, string row, string col, string format);
        #endregion
        
        #region test
        [OperationContract]
        [WebGet(UriTemplate = "/saragisserver/rest/services/gettest?name={name}&callback={callback}", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream gettest(string name, string callBack);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/saragisserver/rest/services/posttest", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string posttest(string name); 
        #endregion

        #region admin api
        //WCF实现REST服务:http://www.cnblogs.com/wuhong/archive/2011/01/13/1934492.html
        [OperationContract(Name = "AddPBSService")]
        [WebInvoke(UriTemplate = "/saragisserver/rest/admin/addService", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream AddPBSService(Stream requestBody);
        //[WebInvoke(UriTemplate = "/saragisserver/rest/admin/createservice?name={name}&port={port}&datasourcetype={datasourcetype}&datasourcepath={datasourcepath}&allowmemorycache={allowmemorycache}&disableclientcache={disableclientcache}&displaynodatatile={displaynodatatile}&visualstyle={visualstyle}&tilingschemepath={tilingschemepath}", BodyStyle = WebMessageBodyStyle.Bare)]
        //Stream CreatePBSService(string name, int port, DataSourceType datasourcetype, string datasourcepath, bool allowmemorycache = true, bool disableclientcache = false, bool displaynodatatile = false, VisualStyle visualstyle = VisualStyle.None, string tilingschemepath = null);

        [OperationContract(Name = "DeletePBSService")]
        [WebInvoke(UriTemplate = "/saragisserver/rest/admin/deleteService", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream DeletePBSService(Stream requestBody);

        [OperationContract(Name = "ClearMemcacheByService")]
        [WebInvoke(UriTemplate = "/saragisserver/rest/admin/memCache/clearByService", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream ClearMemcacheByService(Stream requestBody);

        [OperationContract(Name = "EnableMemcache")]
        [WebInvoke(UriTemplate = "/saragisserver/rest/admin/memCache/enable", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream EnableMemcache(Stream requestBody);

        [OperationContract(Name = "DisableMemcache")]
        [WebInvoke(UriTemplate = "/saragisserver/rest/admin/memCache/disable", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream DisableMemcache(Stream requestBody);

        [OperationContract(Name = "ChangeParamsArcGISDynamicMapService")]
        [WebInvoke(UriTemplate = "/saragisserver/rest/admin/ArcGISDynamicMapService/changeParams", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream ChangeArcGISDynamicMapServiceParams(Stream requestBody);
        #endregion
    }
}
