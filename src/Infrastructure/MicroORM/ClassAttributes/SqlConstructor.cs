namespace RippleSync.Infrastructure.MicroORM.ClassAttributes;
//CTOR parameter navnene er brugt til at finde sql variablerne.
//Derved kan klassens properties være navngivet hvad som helst.
[AttributeUsage(AttributeTargets.Constructor)]
internal class SqlConstructor : Attribute
{
    internal string? SchemaName { get; }
    internal string? TableName { get; }

    internal SqlConstructor(string? schemaName = null, string? tableName = null)
    {
        SchemaName = schemaName;
        TableName = tableName;
    }
}
