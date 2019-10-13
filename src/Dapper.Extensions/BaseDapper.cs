﻿using Dapper.Extensions.Caching;
using Dapper.Extensions.MiniProfiler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Dapper.Extensions
{
    public abstract class BaseDapper : IDapper, IDisposable
    {
        public Lazy<IDbConnection> Conn { get; }

        protected IDbTransaction Transaction { get; set; }

        protected IConfiguration Configuration { get; }

        protected abstract IDbConnection CreateConnection(string connectionName);

        protected CacheConfiguration CacheConfiguration { get; }

        private ICacheProvider Cache { get; }

        private ICacheKeyBuilder CacheKeyBuilder { get; }

        private IDbMiniProfiler DbMiniProfiler { get; }

        protected BaseDapper(IServiceProvider serviceProvider, string connectionName = "DefaultConnection")
        {
            Configuration = serviceProvider.GetRequiredService<IConfiguration>();
            Cache = serviceProvider.GetService<ICacheProvider>();
            CacheConfiguration = serviceProvider.GetService<CacheConfiguration>();
            CacheKeyBuilder = serviceProvider.GetService<ICacheKeyBuilder>();
            DbMiniProfiler = serviceProvider.GetService<IDbMiniProfiler>();
            Conn = new Lazy<IDbConnection>(() => CreateConnection(connectionName));
        }

        protected IDbConnection GetConnection(string connectionName, DbProviderFactory factory)
        {
            var connString = Configuration.GetConnectionString(connectionName);
            if (string.IsNullOrWhiteSpace(connString))
                throw new ArgumentNullException(nameof(connString), "The config of " + connectionName + " cannot be null.");
            var conn = factory.CreateConnection();
            if (conn == null)
                throw new ArgumentNullException(nameof(IDbConnection), "Failed to create database connection.");
            conn.ConnectionString = connString;
            conn.Open();
            return DbMiniProfiler == null ? conn : DbMiniProfiler.CreateConnection(conn);
        }


        public virtual async Task<List<T>> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () => (await Conn.Value.QueryAsync<T>(sql, param, Transaction, commandTimeout)).ToList(), sql, param, cacheKey, cacheExpire);
        }

        public virtual List<T> Query<T>(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return CommandExecute(enableCache, () => Conn.Value.Query<T>(sql, param, Transaction, commandTimeout: commandTimeout).ToList(), sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<List<dynamic>> QueryAsync(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () => (await Conn.Value.QueryAsync(sql, param, Transaction, commandTimeout)).ToList(), sql, param, cacheKey, cacheExpire);
        }

        public virtual List<dynamic> Query(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return CommandExecute(enableCache, () => Conn.Value.Query(sql, param, Transaction, commandTimeout: commandTimeout).ToList(), sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () => await Conn.Value.QueryFirstOrDefaultAsync<T>(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual T QueryFirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return CommandExecute(enableCache, () => Conn.Value.QueryFirstOrDefault<T>(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<dynamic> QueryFirstOrDefaultAsync(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () => await Conn.Value.QueryFirstOrDefaultAsync(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual dynamic QueryFirstOrDefault(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return CommandExecute(enableCache, () => Conn.Value.QueryFirstOrDefault(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual dynamic QuerySingleOrDefault(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return CommandExecute(enableCache, () => Conn.Value.QuerySingleOrDefault(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<dynamic> QuerySingleOrDefaultAsync(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () => await Conn.Value.QuerySingleOrDefaultAsync(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual T QuerySingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return CommandExecute(enableCache, () => Conn.Value.QuerySingleOrDefault<T>(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () => await Conn.Value.QuerySingleOrDefaultAsync<T>(sql, param, Transaction, commandTimeout), sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task QueryMultipleAsync(string sql, Action<SqlMapper.GridReader> reader, object param = null, int? commandTimeout = null)
        {
            using (var multi = await Conn.Value.QueryMultipleAsync(sql, param, Transaction, commandTimeout))
            {
                reader(multi);
            }
        }

        public virtual void QueryMultiple(string sql, Action<SqlMapper.GridReader> reader, object param = null, int? commandTimeout = null)
        {
            using (var multi = Conn.Value.QueryMultiple(sql, param, Transaction, commandTimeout))
            {
                reader(multi);
            }
        }

        public virtual async Task<(List<T1> Result1, List<T2> Result2)> QueryMultipleAsync<T1, T2>(string sql,
            object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () =>
            {
                using (var multi = await Conn.Value.QueryMultipleAsync(sql, param, Transaction, commandTimeout))
                {
                    return ((await multi.ReadAsync<T1>()).ToList(), (await multi.ReadAsync<T2>()).ToList());
                }
            }, sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<(List<T1> Result1, List<T2> Result2, List<T3> Result3)> QueryMultipleAsync<T1, T2, T3>(
            string sql,
            object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () =>
            {
                using (var multi = await Conn.Value.QueryMultipleAsync(sql, param, Transaction, commandTimeout))
                {
                    return ((await multi.ReadAsync<T1>()).ToList(), (await multi.ReadAsync<T2>()).ToList(),
                        (await multi.ReadAsync<T3>()).ToList());
                }
            }, sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<(List<T1> Result1, List<T2> Result2, List<T3> Result3, List<T4> Result4)> QueryMultipleAsync<T1, T2, T3, T4>(string sql, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () =>
            {
                using (var multi = await Conn.Value.QueryMultipleAsync(sql, param, Transaction, commandTimeout))
                {
                    return ((await multi.ReadAsync<T1>()).ToList(), (await multi.ReadAsync<T2>()).ToList(),
                        (await multi.ReadAsync<T3>()).ToList(), (await multi.ReadAsync<T4>()).ToList());
                }
            }, sql, param, cacheKey, cacheExpire);
        }

        public virtual async Task<(List<T1> Result1, List<T2> Result2, List<T3> Result3, List<T4> Result4, List<T5> Result5)> QueryMultipleAsync
            <T1, T2, T3, T4, T5>(
                string sql,
                object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            return await CommandExecuteAsync(enableCache, async () =>
            {
                using (var multi = await Conn.Value.QueryMultipleAsync(sql, param, Transaction, commandTimeout))
                {
                    return ((await multi.ReadAsync<T1>()).ToList(), (await multi.ReadAsync<T2>()).ToList(),
                        (await multi.ReadAsync<T3>()).ToList(), (await multi.ReadAsync<T4>()).ToList(), (await multi.ReadAsync<T5>()).ToList());
                }
            }, sql, param, cacheKey, cacheExpire);
        }

        public virtual IDataReader ExecuteReader(string sql, object param = null, int? commandTimeout = null)
        {
            return Conn.Value.ExecuteReader(sql, param, Transaction, commandTimeout);
        }

        public virtual async Task<IDataReader> ExecuteReaderAsync(string sql, object param = null, int? commandTimeout = null)
        {
            return await Conn.Value.ExecuteReaderAsync(sql, param, Transaction, commandTimeout);
        }

        public virtual async Task<PageResult<T>> QueryPageAsync<T>(string countSql, string dataSql, int pageindex, int pageSize, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            if (pageindex < 1)
                throw new ArgumentException("The pageindex cannot be less then 1.");
            if (pageSize < 1)
                throw new ArgumentException("The pageSize cannot be less then 1.");
            var pars = new DynamicParameters();
            if (param != null)
                pars.AddDynamicParams(param);

            pars.AddDynamicParams(new
            {
                TakeStart = (pageindex - 1) * pageSize + 1,
                TakeEnd = pageindex * pageSize,
                Skip = (pageindex - 1) * pageSize,
                Take = pageSize
            });

            var sql = $"{countSql}{(countSql.EndsWith(";") ? "" : ";")}{dataSql}";
            return await CommandExecuteAsync(enableCache, async () =>
            {
                using (var multi = await Conn.Value.QueryMultipleAsync(sql, pars, Transaction, commandTimeout))
                {
                    var count = (await multi.ReadAsync<long>()).FirstOrDefault();
                    var data = (await multi.ReadAsync<T>()).ToList();
                    var result = new PageResult<T>
                    {
                        TotalCount = count,
                        Page = pageindex,
                        PageSize = pageSize,
                        Contents = data
                    };
                    result.TotalPage = result.TotalCount % pageSize == 0
                        ? result.TotalCount / pageSize
                        : result.TotalCount / pageSize + 1;
                    if (result.Page > result.TotalPage)
                        result.Page = result.TotalPage;
                    return result;
                }
            }, sql, param, cacheKey, cacheExpire, pageindex, pageSize);
        }

        public virtual async Task<PageResult<dynamic>> QueryPageAsync(string countSql, string dataSql, int pageindex, int pageSize, object param = null,
            int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            if (pageindex < 1)
                throw new ArgumentException("The pageindex cannot be less then 1.");
            if (pageSize < 1)
                throw new ArgumentException("The pageSize cannot be less then 1.");
            var pars = new DynamicParameters();
            if (param != null)
                pars.AddDynamicParams(param);

            pars.AddDynamicParams(new
            {
                TakeStart = (pageindex - 1) * pageSize + 1,
                TakeEnd = pageindex * pageSize,
                Skip = (pageindex - 1) * pageSize,
                Take = pageSize
            });
            var sql = $"{countSql}{(countSql.EndsWith(";") ? "" : ";")}{dataSql}";
            return await CommandExecuteAsync(enableCache, async () =>
            {
                using (var multi = await Conn.Value.QueryMultipleAsync(sql, pars, Transaction, commandTimeout))
                {
                    var count = (await multi.ReadAsync<long>()).FirstOrDefault();
                    var data = (await multi.ReadAsync()).ToList();
                    var result = new PageResult<dynamic>
                    {
                        TotalCount = count,
                        Page = pageindex,
                        PageSize = pageSize,
                        Contents = data
                    };
                    result.TotalPage = result.TotalCount % pageSize == 0
                        ? result.TotalCount / pageSize
                        : result.TotalCount / pageSize + 1;
                    if (result.Page > result.TotalPage)
                        result.Page = result.TotalPage;
                    return result;
                }
            }, sql, param, cacheKey, cacheExpire, pageindex, pageSize);

        }

        public virtual PageResult<T> QueryPage<T>(string countSql, string dataSql, int pageindex, int pageSize, object param = null, int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            if (pageindex < 1)
                throw new ArgumentException("The pageindex cannot be less then 1.");
            if (pageSize < 1)
                throw new ArgumentException("The pageSize cannot be less then 1.");
            var pars = new DynamicParameters();
            if (param != null)
                pars.AddDynamicParams(param);

            pars.AddDynamicParams(new
            {
                TakeStart = (pageindex - 1) * pageSize + 1,
                TakeEnd = pageindex * pageSize,
                Skip = (pageindex - 1) * pageSize,
                Take = pageSize
            });
            var sql = $"{countSql}{(countSql.EndsWith(";") ? "" : ";")}{dataSql}";
            return CommandExecute(enableCache, () =>
            {
                using (var multi = Conn.Value.QueryMultiple(sql, pars, Transaction, commandTimeout))
                {
                    var count = multi.Read<long>().FirstOrDefault();
                    var data = multi.Read<T>().ToList();
                    var result = new PageResult<T>
                    {
                        TotalCount = count,
                        Page = pageindex,
                        PageSize = pageSize,
                        Contents = data
                    };
                    result.TotalPage = result.TotalCount % pageSize == 0
                        ? result.TotalCount / pageSize
                        : result.TotalCount / pageSize + 1;
                    if (result.Page > result.TotalPage)
                        result.Page = result.TotalPage;
                    return result;
                }
            }, sql, param, cacheKey, cacheExpire, pageindex, pageSize);
        }

        public virtual PageResult<dynamic> QueryPage(string countSql, string dataSql, int pageindex, int pageSize, object param = null,
            int? commandTimeout = null, bool? enableCache = default, TimeSpan? cacheExpire = default, string cacheKey = default)
        {
            if (pageindex < 1)
                throw new ArgumentException("The pageindex cannot be less then 1.");
            if (pageSize < 1)
                throw new ArgumentException("The pageSize cannot be less then 1.");
            var pars = new DynamicParameters();
            if (param != null)
                pars.AddDynamicParams(param);

            pars.AddDynamicParams(new
            {
                TakeStart = (pageindex - 1) * pageSize + 1,
                TakeEnd = pageindex * pageSize,
                Skip = (pageindex - 1) * pageSize,
                Take = pageSize
            });
            var sql = $"{countSql}{(countSql.EndsWith(";") ? "" : ";")}{dataSql}";
            return CommandExecute(enableCache, () =>
            {
                using (var multi = Conn.Value.QueryMultiple(sql, pars, Transaction, commandTimeout))
                {
                    var count = multi.Read<long>().FirstOrDefault();
                    var data = multi.Read().ToList();
                    var result = new PageResult<dynamic>
                    {
                        TotalCount = count,
                        Page = pageindex,
                        PageSize = pageSize,
                        Contents = data
                    };
                    result.TotalPage = result.TotalCount % pageSize == 0
                        ? result.TotalCount / pageSize
                        : result.TotalCount / pageSize + 1;
                    if (result.Page > result.TotalPage)
                        result.Page = result.TotalPage;
                    return result;
                }
            }, sql, param, cacheKey, cacheExpire, pageindex, pageSize);

        }

        public virtual async Task<int> ExecuteAsync(string sql, object param = null, int? commandTimeout = null)
        {
            return await Conn.Value.ExecuteAsync(sql, param, Transaction, commandTimeout);
        }

        public virtual int Execute(string sql, object param = null, int? commandTimeout = null)
        {
            return Conn.Value.Execute(sql, param, Transaction, commandTimeout);
        }


        public virtual async Task<T> ExecuteScalarAsync<T>(string sql, object param = null, int? commandTimeout = null)
        {
            return await Conn.Value.ExecuteScalarAsync<T>(sql, param, Transaction, commandTimeout);
        }

        public virtual T ExecuteScalar<T>(string sql, object param = null, int? commandTimeout = null)
        {
            return Conn.Value.ExecuteScalar<T>(sql, param, Transaction, commandTimeout);
        }

        public virtual IDbTransaction BeginTransaction()
        {
            return Transaction = Conn.Value.BeginTransaction();
        }

        public virtual IDbTransaction BeginTransaction(IsolationLevel level)
        {
            return Transaction = Conn.Value.BeginTransaction(level);
        }

        public virtual void CommitTransaction()
        {
            if (Transaction == null)
                throw new InvalidOperationException("Please call the BeginTransaction method first.");
            Transaction.Commit();
        }

        public virtual void RollbackTransaction()
        {
            if (Transaction == null)
                throw new InvalidOperationException("Please call the BeginTransaction method first.");
            Transaction.Rollback();
        }

        public virtual void Dispose()
        {
            if (!Conn.IsValueCreated) return;
            Transaction?.Dispose();
            Conn?.Value?.Close();
            Conn?.Value?.Dispose();
        }


        #region Cache methods

        protected bool IsEnableCache(bool? enable)
        {
            if (CacheConfiguration == null)
                return false;
            if (enable.HasValue)
                return enable.Value;
            return CacheConfiguration.AllMethodsEnableCache;
        }

        protected T CommandExecute<T>(bool? enableCache, Func<T> execQuery, string sql, object param, string cacheKey, TimeSpan? expire, int? pageIndex = default, int? pageSize = default)
        {
            if (!IsEnableCache(enableCache))
                return execQuery();
            cacheKey = CacheKeyBuilder.Generate(sql, param, cacheKey, pageIndex, pageSize);
            var cache = Cache.TryGet<T>(cacheKey);
            if (cache.HasKey)
                return cache.Value;
            var result = execQuery();
            Cache.TrySet(cacheKey, result, expire ?? CacheConfiguration.Expire);
            return result;
        }

        protected async Task<T> CommandExecuteAsync<T>(bool? enableCache, Func<Task<T>> execQuery, string sql, object param, string cacheKey, TimeSpan? expire, int? pageIndex = default, int? pageSize = default)
        {
            if (!IsEnableCache(enableCache))
                return await execQuery();
            cacheKey = CacheKeyBuilder.Generate(sql, param, cacheKey, pageIndex, pageSize);
            var cache = Cache.TryGet<T>(cacheKey);
            if (cache.HasKey)
                return cache.Value;
            var result = await execQuery();
            Cache.TrySet(cacheKey, result, expire ?? CacheConfiguration.Expire);
            return result;
        }
        #endregion
    }
}