﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using IRAPBase.Entities;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using IRAPBase.DTO;

namespace IRAPBase
{
    /// <summary>
    /// 所有WebAPI接口类继承此类，目的是为子类提供常用的序列化、验收令牌等便利的方法
    /// </summary>
    public class IRAPBizBase
    {

        private IDbContext _db = null;
        private int _communityID = 0;
        private int _treeID = 0;
        private int _leafID = 0;
        private string _dbName = "";
        private IRAPTreeBase _irapTreeBase = null;

        public IDbContext DB { get { return _db; } }
        public IRAPBizBase()
        {
            _db = DBContextFactory.Instance.CreateContext("IRAPContext");
        }
        /// <summary>
        /// 切换数据库
        /// </summary>
        /// <param name="dbName">数据库名，例如：IRAP,IRAPMDM</param>
        /// <returns>返回数据库上下文</returns>
        public IDbContext UsingContext(string dbName)
        {
            if (_db.DataBase.Connection.Database == dbName)
            {
                return _db;
            }
            else
            {
                if (_db.DataBase.CurrentTransaction != null)
                {
                    _db.DataBase.CurrentTransaction.Rollback();
                }
                _db = DBContextFactory.Instance.CreateContext(dbName + "Context");
            }
            return _db;
        }

        /// <summary>
        /// 创建新的数据库上下文
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public IDbContext CreateDBContext(string dbName)
        {
            return DBContextFactory.Instance.CreateContext(dbName + "Context");
        }
        /// <summary>
        /// 获取一棵树，主数据库用这个类就可以了。
        /// </summary>
        /// <param name="communityID">社区</param>
        /// <param name="treeID">树标识</param>
        /// <param name="leafID">叶标识</param>
        /// <returns></returns>
        public IRAPTreeBase GetIRAPTreeBase(int communityID, int treeID, int leafID)
        {
            _communityID = communityID;
            _treeID = treeID;
            _leafID = leafID;
            _irapTreeBase = new IRAPTreeBase(_db, _communityID, _treeID, _leafID);
            return _irapTreeBase;
        }
        /// <summary>
        /// 使用动态类型返回结果
        /// </summary>
        protected dynamic BackResult = new System.Dynamic.ExpandoObject();
        private LoginEntity logInfo = null;
        #region//序列化相关

        /// <summary>
        /// 把输入参数反序列化为动态类型，仅适用于简单的json（不包括嵌套）复杂的请使用DeserializeObject方法
        /// </summary>
        /// <param name="inParam"></param>
        /// <returns></returns>
        public virtual dynamic GetObjectFromJson(string inParam)
        {
            dynamic d = new System.Dynamic.ExpandoObject();
            // 将JSON字符串反序列化
            JavaScriptSerializer s = new JavaScriptSerializer();
            s.MaxJsonLength = int.MaxValue;
            object resobj = s.DeserializeObject(inParam);
            // 拷贝数据
            IDictionary<string, object> dic = (IDictionary<string, object>)resobj;
            IDictionary<string, object> dicdyn = (IDictionary<string, object>)d;

            foreach (var item in dic)
            {
                dicdyn.Add(item.Key, item.Value);
            }
            return d;
        }


        /// <summary>
        /// 把输入json参数解析为数组
        /// </summary>
        /// <param name="json">输入的json字符串</param>
        /// <returns>返回数组对象</returns>
        public IDictionary<string, object> GetDict(string json)
        {
            // 将JSON字符串反序列化
            JavaScriptSerializer s = new JavaScriptSerializer();
            s.MaxJsonLength = int.MaxValue;
            object resobj = s.DeserializeObject(json);
            // 拷贝数据
            IDictionary<string, object> dic = (IDictionary<string, object>)resobj;
            return dic;
        }

        /// <summary>
        /// 把前台传进来的复杂json（带嵌套的）类型反序列化为复杂对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inparam"></param>
        /// <returns></returns>
        public virtual T DeserializeObject<T>(string inparam)
        {
            return JsonConvert.DeserializeObject<T>(inparam);
        }
        #endregion

        /// <summary>
        /// 把动态类型序列化为json数组
        /// </summary>
        /// <param name="backRes"></param>
        /// <returns></returns>
        public virtual string ToJson(object backRes)
        {
            return JsonConvert.SerializeObject(backRes);
        }
        /// <summary>
        /// 把基类的属性BackResult属性（动态类型）序列化为数组
        /// </summary>
        /// <returns></returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(BackResult);
        }


        public IRAPError VerifyToken(string access_token)
        {
            LoginEntity loginfo = GetLoginInfo(access_token);
            if (loginfo == null)
            {
                return new IRAPError(999999, "验证令牌无效！");
            }
            else
            {
                return new IRAPError(0, "令牌有效！");
            }
        }
        /// <summary>
        /// 根据令牌返回登录信息
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public virtual LoginEntity GetLoginInfo(string access_token)
        {
            if (logInfo == null)
            {

                logInfo = new IRAPLog().GetLogIDByToken(access_token);

            }
            else
            {
                if (logInfo.Access_Token != access_token)
                {
                    logInfo = new IRAPLog().GetLogIDByToken(access_token);
                }
            }
            return logInfo;
        }
        /// <summary>
        /// 根据令牌获取社区标识
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public virtual int GetCommunityID(string access_token)
        {
            if (logInfo == null)
            {
                logInfo = new IRAPLog().GetLogIDByToken(access_token);
            }
            else
            {
                if (logInfo.Access_Token != access_token)
                {
                    logInfo = new IRAPLog().GetLogIDByToken(access_token);
                }
            }
            if (logInfo == null)
            {
                return 0;
            }
            return (int)(logInfo.PartitioningKey / 10000);
        }
        /// <summary>
        /// 获取指定表集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Repository<T> GetRepository<T>() where T : BaseEntity
        {
            return new Repository<T>(_db);

        }
        /// <summary>
        /// 执行个性化sql
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <param name="sqlQueryCommand">原始sql</param>
        /// <param name="parameters">参数值 ，也可以是System.Data.SqlClient.SqlParameter类型</param>
        /// <returns></returns>
        public List<T> SqlQuery<T>(string sqlQueryCommand, params object[] parameters)
        {
            return _db.DataBase.SqlQuery<T>(sqlQueryCommand, parameters).ToList();
        }

        public IEnumerable SqlQuery(Type t, string sqlQueryCommand, params object[] parameters)
        {
            return _db.DataBase.SqlQuery(t, sqlQueryCommand, parameters);
        }
        /// <summary>  
        /// 执行存储过程（语法与数据库相关，尽量少用，否则不能实现跨数据库）
        /// 参见https://www.cnblogs.com/xchit/p/3334782.html
        /// </summary>  
        /// <param name="commandText">SQL命令</param>  
        /// <param name="parameters">参数可使用System.Data.SqlClient.SqlParameter类型</param>  
        /// <returns>返回参数清单System.Data.SqlClient.SqlParameter []</returns>  
        public Object[] ExecuteSqlNonQuery<T>(string commandText, params Object[] parameters)
        {
            var results = _db.DataBase.SqlQuery<T>(commandText, parameters);
            results.Single();
            return parameters;
        }

        /// <summary>
        /// 执行命令（同步）
        /// </summary>
        /// <param name="sqlCommand">sql语句可带参数</param>
        /// <param name="parameters">参数值，可使用System.Data.SqlClient.SqlParameter类型</param>
        /// <returns>影响的行数</returns>
        public int ExecuteSqlCommand(string sqlCommand, params object[] parameters)
        {
            return _db.DataBase.ExecuteSqlCommand(sqlCommand, parameters);
        }

        /// <summary>
        /// 执行命令（异步）
        /// </summary>
        /// <param name="sqlCommand">sql语句可带参数</param>
        /// <param name="parameters">参数值，可使用System.Data.SqlClient.SqlParameter类型</param>
        public void ExecuteSqlCommandAsync(string sqlCommand, params object[] parameters)
        {
            _db.DataBase.ExecuteSqlCommandAsync(sqlCommand, parameters);
        }


        /// <summary>
        /// 使用原始sql语句生成DataTable（仅支持SQLServer)
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameters">参数列表</param>
        /// <returns>返回DataTable</returns>
        public DataTable GetDataTable(string sql, params object[] parameters)
        {

            SqlConnection conn = new System.Data.SqlClient.SqlConnection();
            conn.ConnectionString = _db.DataBase.Connection.ConnectionString;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = sql;
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(p);
            }
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable table = new DataTable();
            adapter.Fill(table);

            conn.Close();//连接需要关闭
            conn.Dispose();
            return table;
        }


        #region 事务控制
        /// <summary>
        /// 开启一个新事务
        /// </summary>
        public void BeginTransaction()
        {
            if (_db.DataBase.CurrentTransaction != null)
            {
                _db.DataBase.CurrentTransaction.Rollback();
                _db.DataBase.CurrentTransaction.Dispose();
            }
            _db.DataBase.BeginTransaction();
        }
        /// <summary>
        /// 对默认数据库连接进行提交
        /// </summary>
        public void Commit()
        {
            _db.SaveChanges();
            if (_db.DataBase.CurrentTransaction != null)
            {
                _db.DataBase.CurrentTransaction.Commit();
            }
        }

        #endregion


        /// <summary>
        /// 申请序列号
        /// </summary>
        /// <param name="seqName">序列名称</param>
        /// <param name="cnt">申请数量</param>
        /// <returns></returns>
        public long GetSequenceValue(string seqName, int cnt = 1)
        {
            SequenceValueDTO error = IRAPSequence.GetSequence(seqName, cnt);
            if (error.ErrCode != 0)
            {
                throw new Exception($"申请序列号异常：{error.ErrText}");
            }

            return error.SequenceValue;
        }

        /// <summary>
        /// 对异常错误的处理
        /// </summary>
        /// <param name="err">异常</param>
        /// <returns>json字符串，形如：{"ErrCode":9999,"ErrText":"异常错误！"}</returns>
        public string ErrorProcess(Exception err)
        {
            if (_db.DataBase.CurrentTransaction != null)
            {
                _db.DataBase.CurrentTransaction.Rollback();
            }
            BackResult.ErrCode = 9999;
            if (err.InnerException != null)
            {
                BackResult.ErrText = err.InnerException.Message;
            }
            else
            {
                BackResult.ErrText = err.Message;
            }
            return ToJson();
        }


       /// <summary>
       /// 获取数据库时间
       /// </summary>
       /// <returns></returns>
        public DateTime DBNow()
        {
            var now = _db.DataBase.SqlQuery<DateTime?>("select GetDate()").First();
            if (now == null)
            {
                now = new DateTime(1900, 1, 1, 0, 0, 0);
            }
            return now.Value;
            //var now2 = _db.Set<ETreeBizClass>().Select(t => SqlFunctions.GetDate()).FirstOrDefault();
            //if (now2 == null)
            //{
            //    now2 = new DateTime(1900, 1, 1, 0, 0, 0);
            //}
            //return now.Value;
        }




    }
}
