﻿using System;
using System.Data;
using System.Threading;
using Dapper;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.MySql
{
    public class MySqlFetchedMessage : IFetchedMessage
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;
        private readonly Timer _timer;
        private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMinutes(1);
        private readonly object _lockObject = new object();

        public MySqlFetchedMessage(int messageId,
            MessageType type,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            MessageId = messageId;
            MessageType = type;
            _connection = connection;
            _transaction = transaction;
            _timer = new Timer(ExecuteKeepAliveQuery, null, KeepAliveInterval, KeepAliveInterval);
        }

        public int MessageId { get; }

        public MessageType MessageType { get; }

        public void RemoveFromQueue()
        {
            lock (_lockObject)
            {
                _transaction.Commit();
            }
        }

        public void Requeue()
        {
            lock (_lockObject)
            {
                _transaction.Rollback();
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _timer?.Dispose();
                _transaction.Dispose();
                _connection.Dispose();
            }
        }

        private void ExecuteKeepAliveQuery(object obj)
        {
            lock (_lockObject)
            {
                try
                {
                    _connection?.Execute("SELECT 1", _transaction);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}