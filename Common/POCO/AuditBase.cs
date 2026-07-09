using MassTransit;
using System;
using LinqToDB.Mapping;
using System.Collections.Generic;
using System.Text;

namespace Interview.Repository.POCO;

public abstract class AAuditBase
{
	[Column( DataType = LinqToDB.DataType.Guid, DbType = "char(36)" )]
	public Guid Id { get; set; } = NewId.NextGuid();

	[Column, NotNull]
	public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;

	// If I had more time you'd have identity information here
}
