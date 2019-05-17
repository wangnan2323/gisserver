using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;



namespace sara.gisserver.console.doconsole
{

    public class ioclass
    {
        public static string logFileFullPath = Eva.Library.Global.AppRootPath + "sara.gisserver.console.log";
        public static string readFile(string fileFullName)
        {
            StreamReader sr = null;
            string content = "";
            try
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(fileFullName);
                sr = new StreamReader(fi.OpenRead());
                content = sr.ReadToEnd();
            }
            catch
            {
                content = null;
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }
            return content;

        }

        public static string writeFile(string fileFullName, string content)
        {
            System.IO.FileStream _fs_log = null;

            System.IO.StreamWriter _sw_log = null;
            try
            {

                if (File.Exists(fileFullName))
                {
                    File.Delete(fileFullName);
                }
                System.IO.FileInfo f = new System.IO.FileInfo(fileFullName);
                DirectoryInfo d = f.Directory;
                if (!d.Exists)
                {
                    Directory.CreateDirectory(d.FullName);
                }               
                File.Create(fileFullName).Close();

                _fs_log = new System.IO.FileStream(fileFullName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);                

                _sw_log = new StreamWriter(_fs_log, System.Text.Encoding.UTF8);

                _sw_log.WriteLine(content);          


                return "";
            }
            catch (Exception ex)
            {
                //throw ex;
                return fileFullName;
            }

            finally
            {
                if (_sw_log != null)
                {
                    _sw_log.Flush();
                    _sw_log.Close();
                }
                if (_fs_log != null)
                {
                    _fs_log.Close();
                }
            }

        }
        public static string writeLog(string content)
        {
            return writeLog(content, "");
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string writeLog( string content,string fileext)
        {

            System.IO.FileStream _fs_log = null;

            System.IO.StreamWriter _sw_log = null;
            try
            {
                string logfile = logFileFullPath;

                if(fileext != "")
                {
                    logfile = logFileFullPath.Replace(".log", "."+ fileext + ".log");
                }

                if (!File.Exists(logfile))
                {

                    System.IO.FileInfo f = new System.IO.FileInfo(logfile);
                    DirectoryInfo d = f.Directory;
                    if (!d.Exists)
                    {
                        Directory.CreateDirectory(d.FullName);
                    }

                    File.Create(logfile).Close();
                }
                _fs_log = new System.IO.FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                _sw_log = new StreamWriter(_fs_log, System.Text.Encoding.UTF8);

                _sw_log.WriteLine(content);
                
                return "";
            }
            catch (Exception ex)
            {
                
                return ex.Message;
            }

            finally
            {
                if (_sw_log != null)
                {
                    _sw_log.Flush();
                    _sw_log.Close();
                }
                if (_fs_log != null)
                {
                    _fs_log.Close();
                }
            }

       
        }


        /// <summary>
        /// 递归拷贝文件夹
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void copyDir(string sourcePath, string targetPath,string ignoreDirPath)
        {

            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }
            Directory.CreateDirectory(targetPath);

         
            try
            {
                string[] labDirs = Directory.GetDirectories(sourcePath);//目录
                string[] labFiles = Directory.GetFiles(sourcePath);//文件
                if (labFiles.Length > 0)
                {
                    for (int i = 0; i < labFiles.Length; i++)
                    {
                        File.Copy(sourcePath + "\\" + Path.GetFileName(labFiles[i]), targetPath + "\\" + Path.GetFileName(labFiles[i]), true);
                    }
                }
                if (labDirs.Length > 0)
                {
                    for (int j = 0; j < labDirs.Length; j++)
                    {
                        string source = sourcePath + "\\" + Path.GetFileName(labDirs[j]);
                     
                        string target = targetPath + "\\" + Path.GetFileName(labDirs[j]);
                    
                        bool isIgnore = false;
                        if (ignoreDirPath == "")
                        {

                        }
                        else
                        {
                            string[] ignoreDirArray = ignoreDirPath.Split('^');

                            for (int ii = 0; ii < ignoreDirArray.Length; ii++)
                            {
                                if (source.Replace("\\\\", "\\").ToLower() == ignoreDirArray[ii].Replace("\\\\", "\\").ToLower())
                                {
                                    isIgnore = true;
                                }
                            }
                        }

                        if (isIgnore == true)
                        {

                        }
                        else
                        {
                            // Directory.GetDirectories(sourcePath + "\\" + Path.GetFileName(labDirs[j]));
                            //递归调用
                            copyDir(source,target , ignoreDirPath);
                        }



                        
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
         
            
        }
    }





}