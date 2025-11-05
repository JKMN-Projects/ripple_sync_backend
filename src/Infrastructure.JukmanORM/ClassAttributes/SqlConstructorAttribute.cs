namespace RippleSync.Infrastructure.JukmanORM.ClassAttributes;
//CTOR parameter navnene er brugt til at finde sql variablerne.
//Derved kan klassens properties være navngivet hvad som helst.
[AttributeUsage(AttributeTargets.Constructor)]
public class SqlConstructorAttribute : Attribute
{
    public string? SchemaName { get; }
    public string? TableName { get; }

    public SqlConstructorAttribute(string? schemaName = null, string? tableName = null)
    {
        SchemaName = schemaName;
        TableName = tableName;
    }
}
