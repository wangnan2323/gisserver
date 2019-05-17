using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara.gisserver.console.gis.util
{
    public class GeometryFormatterTool
    {
        //格式枚举
        public enum layout
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
        public class geometry
        {
            //构造函数
            public geometry(geometryType type, string[] coordinates,string wkid)
            {
                this.type = type;
                this.coordinates = coordinates;
                this.wkid = wkid;
            }
            //图形类别
            geometryType type { get; set; }
            //坐标系 三维数组
            string[] coordinates { get; set; }
            //wkid
            string wkid { get; set; }


        }

        public object Transform(string str,string outSR, layout layout)
        {
            geometry geo = formatToEntity(str);

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


        public geometry formatToEntity(string geostr)
        {
            //判断数据类型
            layout intype;
            if (geostr.IndexOf("type")!=-1) intype = layout.normalGeometries;
        }
    }
}
