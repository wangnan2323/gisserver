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
   

  
    [Serializable]
    public abstract class Geometry
    {
        public abstract Envelope Extent { get; }
    }
    [Serializable]
    public class Envelope : Geometry
    {
        private Envelope extent;
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }
        public Point UpperLeft
        {
            get
            {
                return new Point(XMin, YMax);
            }
        }
        public Point LowerLeft
        {
            get
            {
                return new Point(XMin, YMin);
            }
        }
        public Point UpperRight
        {
            get
            {
                return new Point(XMax, YMax);
            }
        }
        public Point LowerRight
        {
            get
            {
                return new Point(XMax, YMin);
            }
        }
        public Envelope(double xmin, double ymin, double xmax, double ymax)
        {
            XMin = xmin;
            YMin = ymin;
            XMax = xmax;
            YMax = ymax;
        }
        public Envelope()
        {

        }

        public Envelope Union(Envelope newExtent)
        {
            return new Envelope(Math.Min(this.XMin, newExtent.XMin),
                Math.Min(this.YMin, newExtent.YMin),
                Math.Max(this.XMax, newExtent.XMax),
                Math.Max(this.YMax, newExtent.YMax));
        }
        public bool ContainsPoint(Point p)
        {
            return p.X > XMin && p.X < XMax && p.Y > YMin && p.Y < YMax;
        }
        public override Envelope Extent
        {
            get
            {
                if (extent == null)
                    extent = this;
                return extent;
            }
        }

        public Polygon ToPolygon()
        {
            PointCollection pc = new PointCollection();
            pc.Add(this.LowerLeft);
            pc.Add(this.LowerRight);
            pc.Add(this.UpperRight);
            pc.Add(this.UpperLeft);
            pc.Add(this.LowerLeft);
            Polygon p = new Polygon();
            p.Rings.Add(pc);
            return p;
        }
    }
    [Serializable]
    public class Point : Geometry
    {
        private Envelope extent;
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
        public double X { get; set; }
        public double Y { get; set; }
        public override Envelope Extent
        {
            get
            {
                if (extent == null)
                    extent = new Envelope(this.X, this.Y, this.X, this.Y);
                return extent;
            }
        }
    }
    [Serializable]
    public class PointCollection : ObservableCollection<Point>
    {
        public Envelope Extent
        {
            get
            {
                Envelope extent = null;
                foreach (Point point in this)
                {
                    if (point != null)
                    {
                        if (extent == null)
                        {
                            extent = point.Extent;
                        }
                        else
                        {
                            extent = extent.Union(point.Extent);
                        }
                    }
                }
                return extent;
            }
        }
    }
    [Serializable]
    public class Polygon : Geometry
    {
        private Envelope extent;
        public ObservableCollection<PointCollection> Rings { get; set; }
        public Polygon()
        {
            Rings = new ObservableCollection<PointCollection>();
        }
        public override Envelope Extent
        {
            get
            {
                if (this.extent == null)
                {
                    foreach (PointCollection points in this.Rings)
                    {
                        if (this.extent == null)
                        {
                            this.extent = points.Extent;
                        }
                        else
                        {
                            this.extent = this.Extent.Union(points.Extent);
                        }
                    }
                }
                return this.extent;
            }
        }
        /// <summary>
        /// Determining if a point lies on the interior of this polygon.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool ContainsPoint(Point p)
        {
            Polygon polygon = this;
            bool result = false;
            int counter = 0;
            int i;
            double xinters;
            Point p1, p2;
            foreach (PointCollection pc in polygon.Rings)
            {
                int N = pc.Count;

                p1 = pc[0];
                for (i = 1; i <= N; i++)
                {
                    p2 = pc[i % N];
                    if (p.Y > Math.Min(p1.Y, p2.Y))
                    {
                        if (p.Y <= Math.Max(p1.Y, p2.Y))
                        {
                            if (p.X <= Math.Max(p1.X, p2.X))
                            {
                                if (p1.Y != p2.Y)
                                {
                                    xinters = (p.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;
                                    if (p1.X == p2.X || p.X <= xinters)
                                        counter++;
                                }
                            }
                        }
                    }
                    p1 = p2;
                }

                if (counter % 2 == 0)
                    result = false;
                else
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// /// Determines if the two polygons supplied intersect each other, by checking if either polygon has points which are contained in the other.(It doesn't detect body-only intersections, but is sufficient in most cases.)
        /// http://wikicode.wikidot.com/check-for-polygon-polygon-intersection
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public bool IsIntersectsWithPolygon(Polygon poly)
        {
            foreach (PointCollection ring in this.Rings)
            {
                for (int i = 0; i < ring.Count; i++)
                {
                    if (poly.ContainsPoint(ring[i]))
                        return true;
                }
            }
            foreach (PointCollection ring in poly.Rings)
            {
                for (int i = 0; i < ring.Count; i++)
                {
                    if (this.ContainsPoint(ring[i]))
                        return true;
                }
            }
            return false;
        }
    }



}
