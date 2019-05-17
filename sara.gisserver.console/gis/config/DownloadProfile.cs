//****************************************
//Copyright@diligentpig, https://geopbs.codeplex.com
//Please using source code under LGPL license.
//****************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using sara.gisserver.console.gis.server;
using System.Data;
using sara.gisserver.console.gis.datasource;
using System.Collections.ObjectModel;
using sara.gisserver.console.gis.util;
using System.Windows;

namespace sara.gisserver.console.config
{

    /// <summary>
    /// If polygon==null, means download extent is rectangle and drawed by using mouse by user; if polygon!=null, means download extent is a polygon by importing a shapefile.
    /// </summary>
    public class DownloadProfile
    {
        public string Name { get; set; }
        public int[] Levels { get; set; }
        public sara.gisserver.console.gis.util.Envelope Envelope { get; set; }
        public sara.gisserver.console.gis.util.Polygon Polygon { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="levels"></param>
        /// <param name="env">wkid of envelope must be 4326</param>
        /// <param name="polygon">If null, means download extent is rectangle and drawed by using mouse by user; if not null, means download extent is a polygon by importing a shapefile.</param>
        public DownloadProfile(string name, int[] levels, sara.gisserver.console.gis.util.Envelope env, sara.gisserver.console.gis.util.Polygon polygon)
        {
            Name = name;
            Levels = levels;
            Envelope = env;
            Polygon = polygon;
        }
    }
}
