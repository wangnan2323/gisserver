using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Data.SQLite;



namespace sara.gisserver.console.doconsole
{
    public class AccessSQLiteData 
    {
        public string _connectionString
        {
            get;
            set;
        }

        public AccessSQLiteData(string databaseconnectstring)
        {
            _connectionString = databaseconnectstring;


            try
            {
                ////这是最重要的一句，需要引用库。
                ExecuteSql("select load_extension('libspatialite-4.dll')");
                //ExecuteSql("select load_extension('mod_spatialite.dll')");
                //ExecuteSql("select load_extension('libgeos-3-3-1.dll')");
                //ExecuteSql("select load_extension('libgeos_c-1.dll')");
            }
            catch (Exception ex)
            {
                //似乎只有第一次打开时需要这个引用库
            }
        }

        

        #region  执行简单SQL语句


        public bool testdatabaseconnect()
        {
            bool b = true;
            SQLiteConnection connection = new SQLiteConnection(_connectionString);
          
            try
            {
               
                connection.Open();
                connection.Close();
                b = true;
            }
            catch
            {
                connection.Close();
                b = false;
            }
            finally
            {
                connection.Close();
            }

            return b;

        }

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public int ExecuteSql(string SQLString)
        {
            int rows = 0;
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
               
                using (SQLiteCommand cmd = new SQLiteCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        rows = cmd.ExecuteNonQuery();
                     

                    }
                    catch (Exception ex)
                    {
                        connection.Close();
                      
                        throw ex;
                    }
                    finally
                    {
                        cmd.Dispose();
                        connection.Close();
                    }
                }
            }
            return rows;
        }




        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="SQLString">计算查询结果语句</param>
        /// <returns>查询结果（object）</returns>
        public object GetSingle(string SQLString)
        {
            object obj = null;
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
               
                using (SQLiteCommand cmd = new SQLiteCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        obj = cmd.ExecuteScalar();                     
                    }
                    catch (Exception e)
                    {
                        cmd.Dispose();
                        connection.Close();
                     
                        throw e;
                    }
                    finally
                    {
                        cmd.Dispose();
                        connection.Close();
                    }
                }
            }

            return obj;
        }



        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="SQLString">查询语句</param>
        /// <returns>DataSet</returns>
        public DataSet Query(string SQLString)
        {
            SQLiteDataAdapter command = null;
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
               
                DataSet ds = new DataSet();
                try
                {
                    connection.Open();
                    command = new SQLiteDataAdapter(SQLString, connection);
                    command.Fill(ds, "ds");
                  
                }
                catch (Exception e)
                {
                                     
                    throw e;
                }
                finally
                {
                    command.Dispose();
                    connection.Close();
                }
                return ds;
            }
        }



        #endregion

 

        
    }



    public class AccessSQLiteDataFactory
    {
        private static readonly object Lock = new object();

       // private static string connectString = string.Format("Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "default.sqlite;Pooling=true;FailIfMissing=false");
        //错误重试次数
        private static int trytimes = 2;

        private static int errcount = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public static object GetSingle(string sqlString,string connectString)
        {
            lock (Lock)
            {
                //实例化数据库连接
                sara.gisserver.console.doconsole.AccessSQLiteData accessdata = new sara.gisserver.console.doconsole.AccessSQLiteData(connectString);

                object obj;
                try
                {
                    obj = accessdata.GetSingle(sqlString);
                }
                catch (Exception e)
                {
                    errcount++;
                    if (errcount <= trytimes && e.Message.IndexOf("SQL logic error or missing database") != -1)
                    {

                        obj = GetSingle(sqlString, connectString);
                    }
                    else
                    {
                        errcount = 0;
                        throw e;
                    }

                }

                return obj;
            }

        }


        /// <summary>
        /// 执行查询语句，返回DataSet
        /// </summary>
        /// <param name="sqlString">查询语句</param>
        /// <returns>DataSet</returns>
        public static DataSet Query(string sqlString, string connectString)
        {
            lock (Lock)
            {
                //实例化数据库连接
                sara.gisserver.console.doconsole.AccessSQLiteData accessdata = new sara.gisserver.console.doconsole.AccessSQLiteData(connectString);
                DataSet ds = new DataSet();
                try
                {
                    ds = accessdata.Query(sqlString);
                }
                catch (Exception e)
                {
                    errcount++;
                    if (errcount <= trytimes && e.Message.IndexOf("SQL logic error or missing database") != -1)
                    {

                        ds = Query(sqlString, connectString);
                    }
                    else
                    {
                        errcount = 0;
                        throw e;
                    }

                }

                return ds;
            }

        }


        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="sqlString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sqlString, string connectString)
        {

            lock (Lock)
            {
                //实例化数据库连接
                sara.gisserver.console.doconsole.AccessSQLiteData accessdata = new sara.gisserver.console.doconsole.AccessSQLiteData(connectString);
                int rows = 0;
                try
                {
                    rows = accessdata.ExecuteSql(sqlString);
                }
                catch (Exception e)
                {
                    errcount++;
                    if (errcount <= trytimes && e.Message.IndexOf("SQL logic error or missing database") != -1)
                    {

                        rows = ExecuteSql(sqlString, connectString);
                    }
                    else
                    {
                        errcount = 0;
                        throw e;
                    }

                }

                return rows;
            }

        }
    }
}
