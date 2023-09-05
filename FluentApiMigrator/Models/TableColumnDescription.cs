namespace FluentApiMigrator.Models;

public class TableColumnDescription
{
    public string EntityName { get; set; }

    public string TableName { get; set; }

    public int? MaxLength { get; set; }

    public string SqlType { get; set; }

    public bool IsNullable { get; set; }

    public bool IsIdentity { get; set; }

    public bool? IsFixedLength { get; set; }

    public bool IsComputed { get; set; }

    public bool IsPrimaryKey { get; set; }
}
