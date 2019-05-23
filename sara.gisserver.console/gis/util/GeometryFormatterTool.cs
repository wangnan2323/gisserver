using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 识别支持大小写
/// </summary>
namespace sara.gisserver.console.gis.util.geometryFormatterTool
{

    
    //格式枚举
    public enum objectType
    {
        esriGeometries,

        normalGeometries,

        simpleGeometries,

        STGeometries,

        hashTableGeometries
    }


    //图形类型枚举
    public enum geometryType
    {
        line,

        point,

        polygon
    }

    //图形类
    public class geometryEntity
    {
        //构造函数
        public geometryEntity(geometryType type, ArrayList coordinates, string wkid)
        {
            this.type = type;
            this.coordinates = coordinates;
            this.wkid = wkid;
        }
        //图形类别
        public geometryType type { get; set; }
        //坐标系 三维数组
        public ArrayList coordinates { get; set; }
        //wkid
        public string wkid { get; set; }


    }


    public class geometryFormatterTool
    {
        /// <summary>
        /// 将传入的JSONstring型geometry转化为指定格式
        /// </summary>
        /// <param name="inString">JSONstring型geometry</param>
        /// <param name="outSR">如果传入类型为STgeometry则没有wkid参数可供参考，此时可以通过outSR指定输出wkid</param>
        /// <param name="outType">输出的geometry类型</param>
        /// <returns>Hashtable 或 string</returns>
        public static object Transform(string inString, objectType outType, string outSR)
        {
            object ht;
            try
            {
                geometryEntity geo = formatToEntity(inString);
                geo.wkid = outSR;
                ht = formatToGeometry(geo, outType);
            }
            catch (Exception e)
            {
                throw e;
            }

            return ht;
        }
        /// <summary>
        /// 将传入的JSONstring型geometry转化为指定格式
        /// </summary>
        /// <param name="inString">JSONstring型geometry</param>
        /// <param name="outType">输出的geometry类型</param>
        /// <returns>Hashtable 或 string</returns>
        public static object Transform(string inString, objectType outType)
        {
            object ht;
            try
            {
                geometryEntity geo = formatToEntity(inString);
                ht = formatToGeometry(geo, outType);
            }
            catch (Exception e)
            {
                throw e;
            }

            return ht;
        }
        /// <summary>
        /// 将传入的Hashtable转换为指定格式的geometry
        /// </summary>
        /// <param name="inHastTable">传入的Hashtable</param>
        /// <param name="outType">Enum 输出的geometry类型</param>
        /// <returns>Hashtable 或 string</returns>
        public static object Transform(Hashtable inHastTable, objectType outType)
        {
            object ht;
            try
            {
                geometryEntity geo = formatToEntity(inHastTable);
                ht = formatToGeometry(geo, outType);
            }
            catch (Exception e)
            {
                throw e;
            }

            return ht;
        }


        /// <summary>
        /// 将任何类型geometries字符转换为geometries对象
        /// </summary>
        /// <param name="geostr">任何类型geometries字符</param>
        /// <returns>geometries对象</returns>
        private static geometryEntity formatToEntity(string geostr)
        {

            geostr = geostr.TrimStart('[').TrimEnd(']');
            Hashtable ht = JSON.JsonDecode(geostr) as Hashtable;
            Hashtable lowerht = new Hashtable();
            if (ht != null)
            {
                lowerht = hashTableToLower(ht);
            }
            else
            {
                lowerht = null;
            }
            
            //string t = JSON.JsonEncode(ht);
            //判断数据类型
            objectType intype = objectType.normalGeometries;
            if (lowerht != null && lowerht.ContainsKey("type"))
            {
                intype = objectType.normalGeometries;
            }
            else if (lowerht != null && lowerht.ContainsKey("geometrytype"))
            {
                intype = objectType.esriGeometries;
            }
            else if (lowerht != null && lowerht.ContainsKey("spatialreference"))
            {
                intype = objectType.simpleGeometries;
            }
            else if (lowerht == null && (geostr.ToUpper().IndexOf("POLYGON") != -1 || geostr.ToUpper().IndexOf("POINT") != -1 || geostr.ToUpper().IndexOf("LINESTRING") != -1))
            {
                intype = objectType.STGeometries;
            }
            else
            {
                throw new Exception("无法解析的数据类型");
            }

            //分析各类型坐标系和wkid
            ArrayList coordinates = new ArrayList();
            string wkid = "";
            geometryType geometrytype = geometryType.polygon;
            //解析normalGeometries和esriGeometries和simpleGeometries坐标系
            if (intype == objectType.normalGeometries || intype == objectType.esriGeometries || intype == objectType.simpleGeometries)
            {
                //对esriGeometries进行格式统一
                if (intype == objectType.esriGeometries)
                {
                    lowerht = (lowerht["geometries"] as ArrayList)[0] as Hashtable;
                }

                lowerht = hashTableToLower(lowerht);

                if (lowerht.ContainsKey("paths"))
                {
                    coordinates = (lowerht["paths"] as ArrayList) as ArrayList;
                    geometrytype = geometryType.line;
                }
                else if (lowerht.ContainsKey("rings"))
                {
                    coordinates = (lowerht["rings"] as ArrayList) as ArrayList;
                    geometrytype = geometryType.polygon;
                }
                else if (lowerht.ContainsKey("x") && lowerht.ContainsKey("y"))
                {
                    coordinates = new ArrayList() { lowerht["x"], lowerht["y"] };

                    geometrytype = geometryType.point;
                }
                else
                {
                    throw new Exception("解析normalGeometries或esriGeometries时发生异常，未找到坐标系");
                }
                wkid = ((lowerht["spatialreference"] as Hashtable) as Hashtable)["wkid"].ToString();


            }
            else if (intype == objectType.STGeometries)
            {
                wkid = "";
                //将STGeometries格式化为Hashtable
                if (!geostr.ToUpper().StartsWith("MULTI"))
                {
                    geostr = geostr.ToUpper().Replace("POLYGON", "\"POLYGON\":(").Replace("POINT", "\"POINT\":(").Replace("LINESTRING", "\"LINESTRING\":(");

                }
                else
                {
                    geostr = geostr.ToUpper().Replace("MULTIPOLYGON", "\"MULTIPOLYGON\":(").Replace("MULTILINESTRING", "\"MULTILINESTRING\":(");

                }
                geostr = "{" + geostr.Replace('(', '[').Replace(')', ']').Replace(",", "],[").Replace(" ", ",") + "]}";
                Hashtable htst = JSON.JsonDecode(geostr) as Hashtable;
                if (htst.ContainsKey("POLYGON"))
                {
                    coordinates = (htst["POLYGON"] as ArrayList) as ArrayList;

                    geometrytype = geometryType.polygon;
                }
                else if (htst.ContainsKey("MULTIPOLYGON"))
                {
                    ArrayList list = (htst["MULTIPOLYGON"] as ArrayList) as ArrayList;
                    for (int i = 0; i < list.Count; i++)
                    {
                        coordinates.Add((list[i] as ArrayList)[0]);
                    }

                    geometrytype = geometryType.polygon;
                }
                else if (htst.ContainsKey("LINESTRING"))
                {

                    ArrayList linearray = (htst["LINESTRING"] as ArrayList) as ArrayList;
                    coordinates.Add(linearray);

                    geometrytype = geometryType.line;
                }
                else if (htst.ContainsKey("MULTILINESTRING"))
                {
                    coordinates = (htst["MULTILINESTRING"] as ArrayList) as ArrayList;

                    geometrytype = geometryType.line;
                }
                else if (htst.ContainsKey("POINT"))
                {
                    coordinates = (htst["POINT"] as ArrayList)[0] as ArrayList;

                    geometrytype = geometryType.point;
                }
                else
                {
                    throw new Exception("解析normalGeometries或esriGeometries时发生异常，未找到坐标系");
                }
            }

            return new geometryEntity(geometrytype, coordinates, wkid);
        }

        private static Hashtable hashTableToLower(Hashtable ht)
        {
            Hashtable resultht = new Hashtable();
            foreach (DictionaryEntry de in ht) 
            {
                resultht.Add(de.Key.ToString().ToLower(),de.Value);
            }
            return resultht;
        }

        /// <summary>
        /// 将Hashtable转化为geometries对象
        /// </summary>
        /// <param name="table">Hashtable</param>
        /// <returns>geometries</returns>
        private static geometryEntity formatToEntity(Hashtable table)
        {
            ArrayList features = (table["features"] as ArrayList) as ArrayList;
            List<geometryEntity> resultlist = new List<geometryEntity>();
            Hashtable geometryht = (features[0] as Hashtable)["geometry"] as Hashtable;
            string geostr = JSON.JsonEncode(geometryht);
            geometryEntity geo = formatToEntity(geostr);

            return geo;
        }

        /// <summary>
        /// 将geometries对象转化为指定layout的geometry
        /// </summary>
        /// <param name="geometry">geometry实体对象</param>
        /// <param name="layout">Enum 输出geometry类型</param>
        /// <returns>string 或 hashtable</returns>
        private static object formatToGeometry(geometryEntity geometry, objectType layout)
        {
            Hashtable resultht = new Hashtable();
            string resultgeostr = "";
            //转换为esriGeometries
            if (layout == objectType.esriGeometries)
            {
                Hashtable wkidht = new Hashtable();
                wkidht.Add("wkid", geometry.wkid);
                //点
                if (geometry.type == geometryType.point)
                {
                    Hashtable geoht = new Hashtable();
                    geoht.Add("x", geometry.coordinates[0]);
                    geoht.Add("y", geometry.coordinates[1]);
                    geoht.Add("spatialReference", wkidht);
                    ArrayList list = new ArrayList();
                    list.Add(geoht);
                    resultht.Add("geometryType", "esriGeometryPoint");
                    resultht.Add("geometries", list);
                }
                //线  和 面
                else if (geometry.type == geometryType.line || geometry.type == geometryType.polygon)
                {
                    string geotype = "";
                    string geoname = "";
                    if (geometry.type == geometryType.line)
                    {
                        geotype = "esriGeometryPolyline";
                        geoname = "paths";
                    }
                    else
                    {
                        geotype = "esriGeometryPolygon";
                        geoname = "rings";
                    }

                    Hashtable geoht = new Hashtable();
                    geoht.Add(geoname, geometry.coordinates);
                    geoht.Add("spatialReference", wkidht);
                    ArrayList list = new ArrayList();
                    list.Add(geoht);
                    resultht.Add("geometryType", geotype);
                    resultht.Add("geometries", list);
                }

                resultgeostr = JSON.JsonEncode(resultht);
            }
            //转换为normalGeometries 或 simpleGeometries
            else if (layout == objectType.normalGeometries || layout == objectType.simpleGeometries)
            {
                Hashtable wkidht = new Hashtable();
                wkidht.Add("wkid", geometry.wkid);

                //点
                if (geometry.type == geometryType.point)
                {
                    if (layout == objectType.normalGeometries)
                    {
                        resultht.Add("type", "point");
                    }
                    resultht.Add("x", geometry.coordinates[0]);
                    resultht.Add("y", geometry.coordinates[1]);
                    resultht.Add("spatialReference", wkidht);
                }
                //线  和 面
                else if (geometry.type == geometryType.line || geometry.type == geometryType.polygon)
                {
                    string geotype = "";
                    string geoname = "";
                    string _name = "";
                    if (geometry.type == geometryType.line)
                    {
                        geotype = "polyline";
                        geoname = "paths";
                        _name = "_path";
                    }
                    else
                    {
                        geotype = "polygon";
                        geoname = "rings";
                        _name = "_ring";
                    }

                    if (layout == objectType.normalGeometries)
                    {
                        resultht.Add("type", geotype);
                    }
                    resultht.Add(geoname, geometry.coordinates);
                    if (layout == objectType.normalGeometries)
                    {
                        resultht.Add(_name, "0");
                    }
                    resultht.Add("spatialReference", wkidht);
                    if (layout == objectType.normalGeometries && geometry.type == geometryType.polygon)
                    {
                        resultht.Add("_centroid", "null");
                        resultht.Add("_extent", "null");
                    }


                }
                resultgeostr = JSON.JsonEncode(resultht);
            }
            //转化为STGeometries
            else if (layout == objectType.STGeometries)
            {
                //点
                if (geometry.type == geometryType.point)
                {
                    resultgeostr = $"POINT({geometry.coordinates[0]} {geometry.coordinates[1]})";
                }
                //线
                else if (geometry.type == geometryType.line)
                {

                    string pointstr = "";
                    //循环线
                    for (int i = 0; i < geometry.coordinates.Count; i++)
                    {
                        ArrayList linelist = geometry.coordinates[i] as ArrayList;
                        pointstr += "(";
                        //循环线内点
                        for (int ii = 0; ii < linelist.Count; ii++)
                        {

                            pointstr += $"{(linelist[ii] as ArrayList)[0]} {(linelist[ii] as ArrayList)[1]},";

                        }
                        pointstr = pointstr.TrimEnd(',');
                        pointstr += "),";
                    }
                    pointstr = pointstr.TrimEnd(',');

                    if (geometry.coordinates.Count > 1)
                    {
                        resultgeostr = "MULTILINESTRING(" + pointstr + ")";
                    }
                    else
                    {
                        resultgeostr = "LINESTRING" + pointstr;
                    }
                }
                //面
                else if (geometry.type == geometryType.polygon)
                {
                    string pointstr = "";
                    //循环线
                    for (int i = 0; i < geometry.coordinates.Count; i++)
                    {
                        ArrayList linelist = geometry.coordinates[i] as ArrayList;
                        pointstr += "((";
                        //循环线内点
                        for (int ii = 0; ii < linelist.Count; ii++)
                        {

                            pointstr += $"{(linelist[ii] as ArrayList)[0]} {(linelist[ii] as ArrayList)[1]},";

                        }
                        pointstr = pointstr.TrimEnd(',');
                        pointstr += ")),";
                    }
                    pointstr = pointstr.TrimEnd(',');

                    if (geometry.coordinates.Count > 1)
                    {
                        resultgeostr = "MULTIPOLYGON(" + pointstr + ")";
                    }
                    else
                    {
                        resultgeostr = "POLYGON" + pointstr;
                    }
                }
            }
            //转换为hashTableGeometries
            else if (layout == objectType.hashTableGeometries)
            {
                //面 或 线
                if (geometry.type == geometryType.polygon || geometry.type == geometryType.line)
                {
                    string typename = "";
                    string name = "";
                    string _name = "";
                    if (geometry.type == geometryType.polygon)
                    {
                        typename = "polygon";
                        name = "rings";
                        _name = "_ring";
                    }
                    else
                    {
                        typename = "polyline";
                        name = "paths";
                        _name = "_path";
                    }
                    Hashtable wkidht = new Hashtable();
                    wkidht.Add("wkid", geometry.wkid);
                    wkidht.Add("latestWkid", geometry.wkid);

                    Hashtable geoht = new Hashtable();
                    geoht.Add("type", typename);
                    geoht.Add(name, geometry.coordinates);
                    geoht.Add(_name, "0");
                    geoht.Add("spatialReference", wkidht);

                    resultht.Add("geometry", geoht);

                }
                //点
                else if (geometry.type == geometryType.point)
                {
                    Hashtable wkidht = new Hashtable();
                    wkidht.Add("wkid", geometry.wkid);
                    wkidht.Add("latestWkid", geometry.wkid);

                    Hashtable geoht = new Hashtable();
                    geoht.Add("type", "point");
                    geoht.Add("x", geometry.coordinates[0]);
                    geoht.Add("y", geometry.coordinates[1]);
                    geoht.Add("spatialReference", wkidht);

                    resultht.Add("geometry", geoht);
                }



                return resultht;
            }


            return resultgeostr;
        }




}


}
