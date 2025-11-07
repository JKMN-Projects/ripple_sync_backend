using System.Reflection;

namespace RippleSync.Infrastructure.JukmanORM.Exceptions;
public class ExceptionFactory
{
    public static void ThrowRepositoryException(Type currentClass, MethodBase? currentMethod, Exception? otherException = null, string currentQuery = "", object? param = null)
    {
        QueryException? queryException = null;

        if (otherException is QueryException)
            queryException = otherException as QueryException;

        var className = currentClass.FullName != null ? $"in class: {currentClass.FullName}" : "class name is unknown";

        var methodName = currentMethod?.DeclaringType != null ? $"in method: {currentMethod.DeclaringType.Name}" : "method name is unknown";


        var query = queryException != null ? queryException.FailedQuery : currentQuery;
        var fullQuery = queryException != null ? InsertParametersIntoQuery(queryException.FailedQuery, queryException.Param) : InsertParametersIntoQuery(currentQuery, param);

        var queryMsg = $"\n\tFailed query: {fullQuery}";

        if (string.IsNullOrWhiteSpace(query))
            queryMsg = "- No query provided";

        Exception? innerException = queryException ?? otherException;

        throw new RepositoryException($"Exception encountered - {className}, {methodName} {queryMsg}", innerException);
    }

    /// <summary>
    /// Inserts parameter values into a query string for logging purposes
    /// </summary>
    /// <param name="query">The SQL query with parameter placeholders</param>
    /// <param name="param">The parameters object</param>
    /// <returns>Query string with parameter values substituted</returns>
    private static string InsertParametersIntoQuery(string query, object? param)
    {
        if (string.IsNullOrWhiteSpace(query) || param == null)
            return query;

        var result = query;
        var properties = param.GetType().GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(param);
            var formattedValue = FormatParameterValue(value);
            result = result.Replace($"@{property.Name}", formattedValue);
        }

        return result;
    }

    /// <summary>
    /// Formats parameter values for display in logs
    /// </summary>
    /// <param name="value">The parameter value</param>
    /// <returns>Formatted string representation</returns>
    private static string FormatParameterValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''", StringComparison.InvariantCultureIgnoreCase)}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            bool b => b.ToString().ToUpperInvariant(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
