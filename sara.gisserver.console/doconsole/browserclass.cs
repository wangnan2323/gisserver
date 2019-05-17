﻿using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Win32;

namespace sara.gisserver.console.doconsole
{
  public  class browserclass
    {

        /// <summary>
        /// 调用系统浏览器打开网页
        /// http://m.jb51.net/article/44622.htm
        /// http://www.2cto.com/kf/201412/365633.html
        /// </summary>
        /// <param name="url">打开网页的链接</param>
        public static void OpenChrome(string url)
        {
            try
            {
                // 64位注册表路径
                var openKey = @"SOFTWARE\Wow6432Node\Google\Chrome";
                if (IntPtr.Size == 4)
                {
                    // 32位注册表路径
                    openKey = @"SOFTWARE\Google\Chrome";
                }
                RegistryKey appPath = Registry.LocalMachine.OpenSubKey(openKey);
                // 谷歌浏览器就用谷歌打开，没找到就用系统默认的浏览器
                // 谷歌卸载了，注册表还没有清空，程序会返回一个"系统找不到指定的文件。"的bug
                if (appPath != null)
                {
                    var result = Process.Start("chrome.exe", url);
                    if (result == null)
                    {
                        OpenIe(url);
                    }
                }
                else
                {
                    OpenDefault(url);
                }
            }
            catch
            {
                OpenDefault(url);
            }
        }

        /// <summary>
        /// 用IE打开浏览器
        /// </summary>
        /// <param name="url"></param>
        public static void OpenIe(string url)
        {
            try
            {
                Process.Start("iexplore.exe", url);
            }
            catch (Exception ex)
            {
             
                // IE浏览器路径安装：C:\Program Files\Internet Explorer
                // at System.Diagnostics.process.StartWithshellExecuteEx(ProcessStartInfo startInfo)注意这个错误
                try
                {
                    if (File.Exists(@"C:\Program Files\Internet Explorer\iexplore.exe"))
                    {
                        ProcessStartInfo processStartInfo = new ProcessStartInfo
                        {
                            FileName = @"C:\Program Files\Internet Explorer\iexplore.exe",
                            Arguments = url,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        Process.Start(processStartInfo);
                    }
                    else
                    {
                        if (File.Exists(@"C:\Program Files (x86)\Internet Explorer\iexplore.exe"))
                        {
                            ProcessStartInfo processStartInfo = new ProcessStartInfo
                            {
                                FileName = @"C:\Program Files (x86)\Internet Explorer\iexplore.exe",
                                Arguments = url,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            Process.Start(processStartInfo);
                        }
                        else
                        {

                            OpenDefault(url);
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw exception;
                }
            }
        }

        /// <summary>
        /// 打开系统默认浏览器（用户自己设置了默认浏览器）
        /// </summary>
        /// <param name="url"></param>
        public static void OpenDefault(string url)
        {
            try
            {
                // 方法1
                //从注册表中读取默认浏览器可执行文件路径
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"http\shell\open\command\");
                if (key != null)
                {
                    string s = key.GetValue("").ToString();
                    //s就是你的默认浏览器，不过后面带了参数，把它截去，不过需要注意的是：不同的浏览器后面的参数不一样！
                    //"D:\Program Files (x86)\Google\Chrome\Application\chrome.exe" -- "%1"
                    var lastIndex = s.IndexOf(".exe", StringComparison.Ordinal);
                    var path = s.Substring(1, lastIndex + 3);
                    var result = Process.Start(path, url);
                    if (result == null)
                    {
                        // 方法2
                        // 调用系统默认的浏览器 
                        var result1 = Process.Start("explorer.exe", url);
                        if (result1 == null)
                        {
                            // 方法3
                            Process.Start(url);
                        }
                    }
                }
                else
                {
                    // 方法2
                    // 调用系统默认的浏览器 
                    var result1 = Process.Start("explorer.exe", url);
                    if (result1 == null)
                    {
                        // 方法3
                        Process.Start(url);
                    }
                }
            }
            catch
            {
                OpenIe(url);
            }
        }
    }
}
