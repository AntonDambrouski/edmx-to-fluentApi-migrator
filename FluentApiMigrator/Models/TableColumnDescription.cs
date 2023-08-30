namespace FluentApiMigrator.Models;

public class TableColumnDescription
{
    public string Name { get; set; }

    public string DbName { get; set; }

    public int? MaxLength { get; set; }

    public string SqlType { get; set; }

    public bool IsNullable { get; set; }

    public bool IsIdentity { get; set; }

    public bool? IsFixedLength { get; set; }

    public bool? IsUnicode { get; set; }

    public bool IsComputed { get; set; }

    public bool IsPrimaryKey { get; set; }

    public int? Precision { get; set; }

    public int? Scale { get; set; }
    public string ClrType { get; internal set; }
}
