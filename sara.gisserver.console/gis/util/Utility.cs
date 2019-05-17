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
   
    public static class Utility
    {
  

        


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">full file name including path</param>
        /// <returns></returns>
        public static bool IsValidFilename(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            Regex regEx = new Regex("[\\*\\\\/:?<>|\"]");

            return !regEx.IsMatch(Path.GetFileName(path));
        }

        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }

        public static Point GeographicToWebMercator(Point p)
        {
            double x = p.X;
            double y = p.Y;
            if ((y < -90.0) || (y > 90.0))
            {
                throw new ArgumentException("Point does not fall within a valid range of a geographic coordinate system.");
            }
            double num = x * 0.017453292519943295;
            double xx = 6378137.0 * num;
            double a = y * 0.017453292519943295;
            return new Point(xx, 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a))));
        }

        public static Point WebMercatorToGeographic(Point p)
        {
            double originShift = 2 * Math.PI * 6378137 / 2.0;
            double lon = (p.X / originShift) * 180.0;
            double lat = (p.Y / originShift) * 180.0;

            lat = 180 / Math.PI * (2 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);
            return new Point(lon, lat);
        }

        /// <summary>
        /// 获取端口号
        /// </summary>
        /// <returns></returns>
        public static int GetRequestPortNumber()
        {
            if (WebOperationContext.Current != null)
            {
                string host = WebOperationContext.Current.IncomingRequest.Headers["HOST"];//127.0.0.1:8000
                return host.Split(new char[] { ':' }).Length == 1 ? 80 : int.Parse(host.Split(new char[] { ':' })[1]);
            }
            return -1;
        }

        public static string GetRequestIPAddress()
        {
            OperationContext context = OperationContext.Current;
            MessageProperties messageProperties = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpointProperty =
              messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            return endpointProperty.Address;
        }

        /// <summary>
        /// MBTile implement Tiled Map Service Specification. http://wiki.osgeo.org/wiki/Tile_Map_Service_Specification
        /// Used to convert row/col number in TMS to row/col number in Google/ArcGIS.
        /// TMS starts (0,0) from left bottom corner, while Google/ArcGIS/etc. starts (0,0) from left top corner.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public static void ConvertGoogleTileToTMSTile(int level, int row, int col, out int outRow, out int outCol)
        {
            outCol = col;
            outRow = ((int)(Math.Pow(2.0, (double)level) - 1.0)) - row;
        }

        public static void ConvertTMSTileToGoogleTile(int level, int row, int col, out int outRow, out int outCol)
        {
            outCol = col;
            outRow = ((int)(Math.Pow(2.0, (double)level) - 1.0)) - row;
        }
        /// <summary>
        /// calculate the bounding box of a tile or a bundle.
        /// </summary>
        /// <param name="tileOrigin">tiling scheme origin</param>
        /// <param name="resolution">the resolution of the level which the tile blongs</param>
        /// <param name="tileRows">the pixel count of row in the tile</param>
        /// <param name="tileCols">the pixel count of column in the tile</param>
        /// <param name="row">the row number of the tile</param>
        /// <param name="col">the column number of the tile</param>
        /// <param name="xmin"></param>
        /// <param name="ymin"></param>
        /// <param name="xmax"></param>
        /// <param name="ymax"></param>
        public static void CalculateBBox(Point tileOrigin, double resolution, int tileRows, int tileCols, int row, int col, out double xmin, out double ymin, out double xmax, out double ymax)
        {
            //calculate the bbox
            xmin = tileOrigin.X + resolution * tileCols * col;
            ymin = tileOrigin.Y - resolution * tileRows * (row + 1);
            xmax = tileOrigin.X + resolution * tileCols * (col + 1);
            ymax = tileOrigin.Y - resolution * tileRows * row;
        }

        /// <summary>
        /// using for bing maps
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string TileXYToQuadKey(int tileX, int tileY, int level)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = level; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);//掩码，最高位设为1，其他位设为0
                if ((tileX & mask) != 0)//与运算取得tileX的最高位，若为1，则加1
                {
                    digit++;
                }
                if ((tileY & mask) != 0)//与运算取得tileY的最高位，若为1，则加2
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);//也即2*y+x
            }
            return quadKey.ToString();
        }

        public static byte[] MakeGrayUsingWPF(byte[] tileBytes)
        {
            using (MemoryStream ms = new MemoryStream(tileBytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                //bitmap.DecodePixelWidth = 200;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                FormatConvertedBitmap fcb = new FormatConvertedBitmap();
                fcb.BeginInit();
                fcb.Source = bitmap;
                fcb.DestinationFormat = PixelFormats.Gray8;
                fcb.DestinationPalette = BitmapPalettes.WebPaletteTransparent;
                fcb.AlphaThreshold = 0.5;
                fcb.EndInit();
                //ref: http://192.168.0.106:8000/saragisserver/rest/services/BingMapsRoad/MapServer/tile/14/6208/13491
                //gray4pngencoder=14.5kb
                //gray8pngencoder=21.5kb
                //gray16pngencoder=36.9kb
                //gray8jpegencoder=22.9kb
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                BitmapFrame frame = BitmapFrame.Create(fcb);
                encoder.Frames.Add(frame);
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
        }

        public static byte[] MakeGrayUsingGDI(byte[] tileBytes)
        {
            using (MemoryStream ms = new MemoryStream(tileBytes))
            {
                Bitmap gdiBitmap = new Bitmap(ms);//default is PixelFormat.Format8bppIndexed
                //create a blank bitmap the same size as original
                Bitmap newBitmap = new Bitmap(gdiBitmap.Width, gdiBitmap.Height);

                //get a graphics object from the new image
                Graphics g = Graphics.FromImage(newBitmap);

                //create the grayscale ColorMatrix
                //Invert matrix is found by googleing:invert colormatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
      {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
      });

                //create some image attributes
                ImageAttributes attributes = new ImageAttributes();
                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                //draw the original image on the new image
                //using the grayscale color matrix
                g.DrawImage(gdiBitmap, new Rectangle(0, 0, gdiBitmap.Width, gdiBitmap.Height),
                   0, 0, gdiBitmap.Width, gdiBitmap.Height, GraphicsUnit.Pixel, attributes);

                //dispose the Graphics object
                g.Dispose();
                BitmapSource bitmapSource = ConvertToBitmapSource(newBitmap);

                //http://192.168.100.109:8000/saragisserver/rest/services/GoogleMapsRoad/MapServer/tile/11/775/1686
                //pngencoder 79.5kb
                //jpegencoder 32kb
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 75;
                BitmapFrame frame = BitmapFrame.Create(bitmapSource);
                encoder.Frames.Add(frame);
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// C# Tutorial - Convert a Color Image to Grayscale
        /// ref: http://www.switchonthecode.com/tutorials/csharp-tutorial-convert-a-color-image-to-grayscale
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static byte[] MakeInvertUsingGDI(byte[] tileBytes)
        {
            using (MemoryStream ms = new MemoryStream(tileBytes))
            {
                Bitmap gdiBitmap = new Bitmap(ms);//default is PixelFormat.Format8bppIndexed
                //create a blank bitmap the same size as original
                Bitmap newBitmap = new Bitmap(gdiBitmap.Width, gdiBitmap.Height);
                //get a graphics object from the new image
                Graphics g = Graphics.FromImage(newBitmap);

                //create the grayscale ColorMatrix
                //Invert matrix is found by googleing:invert colormatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
{
   new float[] {-1, 0, 0, 0, 0},
   new float[] {0, -1, 0, 0, 0},
   new float[] {0, 0, -1, 0, 0},
   new float[] {0, 0, 0, 1, 0},
   new float[] {1, 1, 1, 0, 1}
});

                //create some image attributes
                ImageAttributes attributes = new ImageAttributes();
                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                //draw the original image on the new image
                //using the grayscale color matrix
                g.DrawImage(gdiBitmap, new Rectangle(0, 0, gdiBitmap.Width, gdiBitmap.Height),
                   0, 0, gdiBitmap.Width, gdiBitmap.Height, GraphicsUnit.Pixel, attributes);

                //dispose the Graphics object
                g.Dispose();

                BitmapSource bitmapSource = ConvertToBitmapSource(newBitmap);
                gdiBitmap.Dispose();
                newBitmap.Dispose();
                //http://192.168.100.109:8000/saragisserver/rest/services/GoogleMapsRoad/MapServer/tile/11/775/1686
                //pngencoder 79.5kb
                //jpegencoder 32kb
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 75;
                BitmapFrame frame = BitmapFrame.Create(bitmapSource);
                encoder.Frames.Add(frame);
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Getting Started with Shader Effects in WPF:http://www.codeproject.com/KB/WPF/WPF_shader_effects.aspx
        /// gpu enabled
        /// </summary>
        /// <param name="tileBytes"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static byte[] MakeShaderEffect(byte[] tileBytes, VisualStyle style)
        {
            if (tileBytes == null)
                return null;
            using (MemoryStream ms = new MemoryStream(tileBytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                //bitmap.DecodePixelWidth = 200;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(bitmap, new Rect(new System.Windows.Size(bitmap.PixelWidth, bitmap.PixelHeight)));
                }
                switch (style)
                {
                    case VisualStyle.None:
                        break;
                    case VisualStyle.Gray:
                        drawingVisual.Effect = new sara.gisserver.console.gis.shaders.MonochromeEffect();
                        break;
                    case VisualStyle.Invert:
                        drawingVisual.Effect = new sara.gisserver.console.gis.shaders.InvertColorEffect();
                        break;
                    case VisualStyle.Tint:
                        drawingVisual.Effect = new sara.gisserver.console.gis.shaders.TintShaderEffect();
                        break;
                    //case VisualStyle.Saturation:
                    //    drawingVisual.Effect = new sara.gisserver.console.gis.shaders.SaturationEffect();
                    //    break;
                    case VisualStyle.Embossed:
                        drawingVisual.Effect = new sara.gisserver.console.gis.shaders.EmbossedEffect(3.5);
                        break;
                    default:
                        break;
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, System.Windows.Media.PixelFormats.Default);
                rtb.Render(drawingVisual);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                BitmapFrame frame = BitmapFrame.Create(rtb);
                encoder.Frames.Add(frame);
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// http://www.netframeworkdev.com/windows-presentation-foundation-wpf/invert-background-brush-86966.shtml
        /// Invert Background Brush
        /// </summary>
        /// <param name="gdiPlusBitmap"></param>
        /// <returns></returns>
        public static BitmapSource ConvertToBitmapSource(Bitmap gdiPlusBitmap)
        {
            IntPtr hBitmap = gdiPlusBitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                //ref:Bitmap to BitmapSource http://www.codeproject.com/KB/WPF/BitmapToBitmapSource.aspx
                DeleteObject(hBitmap);//very important to avoid memory leak
            }
        }

        /// <summary>
        /// C#对Windows服务操作(注册安装服务,卸载服务,启动停止服务,判断服务存在)
        /// http://blog.csdn.net/hejialin666/article/details/5657695
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static bool IsWindowsServiceExisted(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController s in services)
            {
                if (s.ServiceName == serviceName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// determine if a windows service is running.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsWindowsServiceStarted(string name)
        {
            ServiceController[] service = ServiceController.GetServices();
            bool isStart = false;
            for (int i = 0; i < service.Length; i++)
            {
                if (service[i].DisplayName.ToUpper().Contains(name.ToUpper()))
                {
                    if (service[i].Status == ServiceControllerStatus.Running)
                    {
                        isStart = true;
                        break;
                    }
                }
            }
            return isStart;
        }

        /// <summary>
        /// Run cmd command in code
        /// C#中一种执行命令行或DOS内部命令的方法:http://www.cppblog.com/andxie99/archive/2006/12/09/16200.html
        /// </summary>
        /// <param name="strIp">cmd line input</param>
        /// <param name="returnProcess">wether return the Cmd Process for holding it for longer using</param>
        /// <param name="sleepTime">main thread sleep time to execute the command</param>
        /// <returns></returns>
        public static Process Cmd(string strCmd, bool returnProcess, out string strResult, int sleepTime = 250)
        {
            // 实例一个Process类,启动一个独立进程
            Process p = new Process();

            // 设定程序名
            p.StartInfo.FileName = "cmd.exe";
            // 关闭Shell的使用
            p.StartInfo.UseShellExecute = false;
            // 重定向标准输入
            p.StartInfo.RedirectStandardInput = true;
            // 重定向标准输出
            p.StartInfo.RedirectStandardOutput = true;
            //重定向错误输出
            p.StartInfo.RedirectStandardError = true;
            // 设置不显示窗口
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            p.StandardInput.WriteLine(strCmd);
            System.Threading.Thread.Sleep(sleepTime);
            if (returnProcess == false)
            {
                p.StandardInput.WriteLine("exit");
                System.Threading.Thread.Sleep(sleepTime);
                strResult = p.StandardOutput.ReadToEnd();//output information
                p.Close();
                return null;
            }
            else
            {
                strResult = "";//StandardOutput.ReadToEnd() must be after p.StandardInput.WriteLine("exit")
                return p;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <param name="entryName">the specified filename in zip file to be retrieved, may contain directory components separated by slashes ('/'). http://www.icsharpcode.net/CodeReader/SharpZipLib/031/ZipZipFile.cs.html</param>
        /// <returns></returns>
        public static byte[] GetEntryBytesFromZIPFile(string zipFilePath, string entryName)
        {
            //Using SharpZipLib to unzip specific files?http://stackoverflow.com/questions/328343/using-sharpziplib-to-unzip-specific-files
            if (!System.IO.File.Exists(zipFilePath))
            {
                return null;
            }
            using (FileStream fs = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
            {
                ZipFile zf = new ZipFile(fs);
                ZipEntry ze = zf.GetEntry(entryName);
                if (ze == null)
                    return null;
                //try
                //{
                using (Stream stream = zf.GetInputStream(ze))
                {
                    return StreamToBytes(stream);
                }
                //}
                //finally
                //{
                //    zf.Close();
                //}
            }
        }

        public static byte[] StreamToBytes(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static byte[] SerializeObject(object data)
        {
            if (data == null)
                return null;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            MemoryStream rems = new MemoryStream();
            formatter.Serialize(rems, data);
            return rems.GetBuffer();
        }
        public static object DeserializeObject(byte[] data)
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            MemoryStream rems = new MemoryStream(data);
            data = null;
            return formatter.Deserialize(rems);
        }

        public static bool Is64bitOS()
        {
            //How to detect Windows 64 bit platform with .net? http://stackoverflow.com/questions/336633/how-to-detect-windows-64-bit-platform-with-net
            //How to check whether the system is 32 bit or 64 bit ?:http://social.msdn.microsoft.com/Forums/da-DK/csharpgeneral/thread/24792cdc-2d8e-454b-9c68-31a19892ca53
            return Environment.Is64BitOperatingSystem;
        }

        //应对32位程序在64位系统上访问注册表和文件自动转向问题:http://www.cnblogs.com/FlyingBread/archive/2007/01/21/624291.html
        /// <summary>
        /// Disable system32 directory redirect on 64bit system
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);
        /// <summary>
        /// Enable system32 directory redirect on 64bit system
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);
        /// <summary>
        /// look msdn: Bitmap.GetHbitmap()
        /// </summary>
        /// <param name="hObject"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        #region UserValidation
        //How to check if a given user is local admin or not:http://social.msdn.microsoft.com/Forums/en-US/netfxbcl/thread/e799c2f4-4ada-477b-8f98-05bafb0225f0
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private extern static bool CloseHandle(IntPtr handle);

        public static bool IsUserAdmin(String user, String password, String domain)
        {
            IntPtr userToken = IntPtr.Zero;
            try
            {
                bool retVal = LogonUser(user, domain, password, 2, 0, ref userToken);

                if (!retVal)
                {
                    //throw new Exception("The user name and password does not exist in " + domain);
                    return false;
                }
                return IsUserTokenAdmin(userToken);
            }
            finally
            {
                CloseHandle(userToken);
            }
        }

        private static bool IsUserTokenAdmin(IntPtr userToken)
        {
            using (WindowsIdentity user = new WindowsIdentity(userToken))
            {
                WindowsPrincipal principal = new WindowsPrincipal(user);

                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        #endregion        
    }

   
}
