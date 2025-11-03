namespace RippleSync.Infrastructure.JukmanORM.ClassAttributes;
//CTOR parameter navnene er brugt til at finde sql variablerne.
//Derved kan klassens properties være navngivet hvad som helst.
[AttributeUsage(AttributeTargets.Constructor)]
public class SqlConstructor : Attribute
{
    public string? SchemaName { get; }
    public string? TableName { get; }

    public SqlConstructor(string? schemaName = null, string? tableName = null)
    {
        SchemaName = schemaName;
        TableName = tableName;
    }
}
