using System.Data;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace Nomenclador.Api.Data;

/// <summary>
/// Tipo personalizado NHibernate para mapear DateOnly ↔ Oracle DATE.
/// </summary>
public sealed class DateOnlyUserType : IUserType
{
    public SqlType[] SqlTypes => [new SqlType(DbType.Date)];
    public Type ReturnedType => typeof(DateOnly);
    public bool IsMutable => false;

    public new bool Equals(object? x, object? y) => object.Equals(x, y);
    public int GetHashCode(object x) => x?.GetHashCode() ?? 0;
    public object DeepCopy(object value) => value;
    public object Replace(object original, object target, object owner) => original;
    public object Assemble(object cached, object owner) => cached;
    public object Disassemble(object value) => value;

    public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var raw = NHibernateUtil.DateTime.NullSafeGet(rs, names, session, owner);
        if (raw is null) return DateOnly.MinValue;
        return DateOnly.FromDateTime((DateTime)raw);
    }

    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        if (value is null or DBNull)
        {
            ((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
        }
        else
        {
            ((IDataParameter)cmd.Parameters[index]).Value =
                ((DateOnly)value).ToDateTime(TimeOnly.MinValue);
        }
    }
}

/// <summary>
/// Tipo personalizado NHibernate para mapear DateOnly? ↔ Oracle DATE (nullable).
/// </summary>
public sealed class NullableDateOnlyUserType : IUserType
{
    public SqlType[] SqlTypes => [new SqlType(DbType.Date)];
    public Type ReturnedType => typeof(DateOnly?);
    public bool IsMutable => false;

    public new bool Equals(object? x, object? y) => object.Equals(x, y);
    public int GetHashCode(object x) => x?.GetHashCode() ?? 0;
    public object DeepCopy(object value) => value;
    public object Replace(object original, object target, object owner) => original;
    public object Assemble(object cached, object owner) => cached;
    public object Disassemble(object value) => value;

    public object? NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var raw = NHibernateUtil.DateTime.NullSafeGet(rs, names, session, owner);
        if (raw is null) return null;
        return DateOnly.FromDateTime((DateTime)raw);
    }

    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        if (value is null or DBNull)
        {
            ((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
        }
        else
        {
            var date = ((DateOnly?)value)!.Value;
            ((IDataParameter)cmd.Parameters[index]).Value =
                date.ToDateTime(TimeOnly.MinValue);
        }
    }
}
