﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NHibernate.Dialect;
using NHibernate.Engine;
using NHibernate.Engine.Transaction;
using NHibernate.Exceptions;

namespace NHibernate.Transaction
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class AdoNetTransactionFactory : ITransactionFactory
	{

		/// <inheritdoc />
		public virtual Task ExecuteWorkInIsolationAsync(ISessionImplementor session, IIsolatedWork work, bool transacted, CancellationToken cancellationToken)
		{
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (work == null)
				throw new ArgumentNullException(nameof(work));
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return InternalExecuteWorkInIsolationAsync();
			async Task InternalExecuteWorkInIsolationAsync()
			{

				DbConnection connection = null;
				DbTransaction trans = null;
				try
				{
					// We make an exception for SQLite and use the session's connection,
					// since SQLite only allows one connection to the database.
					connection = session.Factory.Dialect is SQLiteDialect
						? session.Connection
						: await (session.Factory.ConnectionProvider.GetConnectionAsync(cancellationToken)).ConfigureAwait(false);

					if (transacted)
					{
						trans = connection.BeginTransaction();
					}

					await (work.DoWorkAsync(connection, trans, cancellationToken)).ConfigureAwait(false);

					if (transacted)
					{
						trans.Commit();
					}
				}
				catch (Exception t)
				{
					using (session.BeginContext())
					{
						try
						{
							if (trans != null && connection.State != ConnectionState.Closed)
							{
								trans.Rollback();
							}
						}
						catch (Exception ignore)
						{
							_isolatorLog.Debug(ignore, "Unable to rollback transaction");
						}

						switch (t)
						{
							case HibernateException _:
								throw;
							case DbException _:
								throw ADOExceptionHelper.Convert(session.Factory.SQLExceptionConverter, t,
								                                 "error performing isolated work");
							default:
								throw new HibernateException("error performing isolated work", t);
						}
					}
				}
				finally
				{
					try
					{
						trans?.Dispose();
					}
					catch (Exception ignore)
					{
						_isolatorLog.Warn(ignore, "Unable to dispose transaction");
					}

					if (connection != null && session.Factory.Dialect is SQLiteDialect == false)
						session.Factory.ConnectionProvider.CloseConnection(connection);
				}
			}
		}
	}
}
