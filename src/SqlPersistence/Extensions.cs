﻿using System;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static class Extensions
{
    public static void AddParameter(this DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public static async Task<DbConnection> OpenConnection(this Func<DbConnection> connectionBuilder)
    {
        var connection = connectionBuilder();
        await connection.OpenAsync();
        return connection;
    }

    public static void AddParameter(this DbCommand command, string name, Version value)
    {
        command.AddParameter(name, value.ToString());
    }

    public static async Task<bool> GetBoolAsync(this DbDataReader reader, int position)
    {
        var type = reader.GetFieldType(position);
        // MySql stores bools as ints
        if (type == typeof(ulong))
        {
            return Convert.ToBoolean(await reader.GetFieldValueAsync<ulong>(position));
        }
        return await reader.GetFieldValueAsync<bool>(position);
    }

    public static async Task<Guid> GetGuidAsync(this DbDataReader reader, int position)
    {
        var type = reader.GetFieldType(position);
        // MySql stores Guids as strings
        if (type == typeof(string))
        {
            return new Guid(await reader.GetFieldValueAsync<string>(position));
        }
        return await reader.GetFieldValueAsync<Guid>(position);
    }

    internal static Func<T, object> GetPropertyAccessor<T>(this Type sagaDataType, string propertyName)
    {
        var propertyInfo = sagaDataType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (propertyInfo == null)
        {
            throw new Exception($"Expected '{sagaDataType.FullName}' to contain a gettable property named '{propertyName}'.");
        }
        return data => propertyInfo.GetValue(data);
    }

    public static Task ExecuteNonQueryEx(this DbCommand command)
    {
        return ExecuteNonQueryEx(command, CancellationToken.None);
    }

    public static async Task ExecuteNonQueryEx(this DbCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            var message = $"Failed to ExecuteNonQuery. CommandText:{Environment.NewLine}{command.CommandText}";
            throw new Exception(message, exception);
        }
    }
}