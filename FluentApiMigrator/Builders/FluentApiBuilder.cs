using FluentApiMigrator.Models;
using System.Data.Entity.Core.Metadata.Edm;
using System.Text;

namespace FluentApiMigrator.Builders;

public class FluentApiBuilder
{
    private readonly string NEW_LINE = Environment.NewLine;
    private const int INDENT_SIZE = 2;
    private const int CLASS_INDENT = 1;
    private const int METHOD_INDENT = 2;

    private readonly HashSet<string> _defaultNamespaces = new()
    {
        "using System;",
        "using System.ComponentModel.DataAnnotations;",
        "using System.ComponentModel.DataAnnotations.Schema;",
        "using System.Data.Entity.ModelConfiguration;"
    };

    private readonly StringBuilder _builder;

    public FluentApiBuilder()
    {
        _builder = new StringBuilder();
    }

    public FluentApiBuilder AddDefaultUsings()
    {
        var namespaces = string.Join(Environment.NewLine, _defaultNamespaces);
        _builder.AppendLine(namespaces).AppendLine();

        return this;
    }

    public FluentApiBuilder AddNamespace(string? namespaceName = null)
    {
        var name = string.IsNullOrEmpty(namespaceName) ? "Change.Namespace.Generated" : namespaceName;
        _builder.AppendLine($"namespace {name}").AppendLine("{");

        return this;
    }

    public FluentApiBuilder StartEntityConfiguration(string entityName)
    {
        // initialize class with constructor and inherit from EntityTypeConfiguration<T> class
        _builder.AppendLine($"{Indent(CLASS_INDENT)}internal class {entityName}Configuration : EntityTypeConfiguration<{entityName}>")
            .AppendLine(Indent(CLASS_INDENT) + "{")
            .AppendLine($"{Indent(METHOD_INDENT)}public {entityName}Configuration()")
            .AppendLine(Indent(METHOD_INDENT) + "{");

        return this;
    }

    public FluentApiBuilder ToTable(string tableName, string schema)
    {
        _builder.AppendLine($"{Indent(METHOD_INDENT + 1)}ToTable(\"{tableName}\", \"{schema}\");");
        return this;
    }

    public FluentApiBuilder HasKey(params string[] primaryKeys)
    {
        if (primaryKeys.Length == 1)
        {
            _builder.AppendLine($"{Indent(METHOD_INDENT + 1)}HasKey(e => e.{primaryKeys.First()});");
        }
        else
        {
            var keys = string.Join(", ", primaryKeys.Select(x => $"e.{x}"));
            _builder.AppendLine($"{Indent(METHOD_INDENT + 1)}HasKey(e => new {{ {keys} }})");
        }

        return this;
    }

    public FluentApiBuilder AddEmptyLine()
    {
        _builder.AppendLine();
        return this;
    }

    public FluentApiBuilder AddProperty(TableColumnDescription description)
    {
        _builder.Append($"{Indent(METHOD_INDENT + 1)}Property(e => e.{description.EntityName})")
            .Append($".HasColumnName(\"{description.TableName}\")")
            .Append($".HasColumnType(\"{description.SqlType}\")")
            .Append(description.IsNullable ? ".IsOptional()" : ".IsRequired()")
            .Append(description.IsFixedLength ?? false ? ".IsFixedLength()" : string.Empty)
            .Append(description.MaxLength > 0 ? $".HasMaxLength({description.MaxLength})" : string.Empty);

        if (description.IsIdentity)
        {
            _builder.Append(".HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)");
        }
        else if (description.IsComputed)
        {
            _builder.Append(".HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)");
        }
        else if (description.IsPrimaryKey)
        {
            _builder.Append(".HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)");
        }

        _builder.AppendLine(";");
        return this;
    }

    public FluentApiBuilder AddRelationship(RelationshipDescription description, List<string> primaryKeys)
    {
        _builder.Append(Indent(METHOD_INDENT + 1) + HasRelationship(description.From.NavigationPropertyName, description.From.RelationshipType))
            .Append(WithRelationship(description, primaryKeys));

        if (!description.IsManyToMany && description.DeleteBehavior == OperationAction.Cascade)
        {
            _builder.Append(".WillCascadeOnDelete(true)");
        }
        else if (!description.IsManyToMany && description.DeleteBehavior == OperationAction.None)
        {
            _builder.Append(".WillCascadeOnDelete(false)");
        }

        _builder.AppendLine(";");
        return this;
    }

    public FluentApiBuilder EndEntityConfiguration()
    {
        _builder.AppendLine(Indent(METHOD_INDENT) + "}")
            .AppendLine(Indent(CLASS_INDENT) + "}")
            .AppendLine("}");

        return this;
    }

    public void Clear()
    {
        _builder.Clear();
    }

    public override string ToString()
    {
        return _builder.ToString();
    }

    private string HasRelationship(string navPropertyName, RelationshipMultiplicity multiplicity) => multiplicity switch
    {
        RelationshipMultiplicity.ZeroOrOne => $"HasOptional(e => e.{navPropertyName})",
        RelationshipMultiplicity.One => $"HasRequired(e => e.{navPropertyName})",
        RelationshipMultiplicity.Many => $"HasMany(e => e.{navPropertyName})",
        _ => throw new ArgumentOutOfRangeException(nameof(multiplicity)),
    };

    private string WithRelationship(RelationshipDescription description, List<string> primaryKeys)
    {
        string hasForeignKey = string.Empty;
        if (!description.IsManyToMany && !description.IsZeroOrOneToOne)
        {
            var foreignKeys = description.ForeignKeys.Except(primaryKeys).ToList();
            if (foreignKeys.Any())
            {
                var propertyExpression = foreignKeys.Count == 1
                    ? $"e => e.{foreignKeys[0]}"
                    : $"e => new {{ {string.Join(" ,", foreignKeys.Select(k => $"e.{k}"))} }}";

                hasForeignKey = $".HasForeignKey({propertyExpression})";
            }
        }

        var withExpression = !string.IsNullOrEmpty(description.To.NavigationPropertyName) ? $"e => e.{description.To.NavigationPropertyName}" : string.Empty;
        switch (description.To.RelationshipType)
        {
            case RelationshipMultiplicity.ZeroOrOne: return $".WithOptional({withExpression}){hasForeignKey}";
            case RelationshipMultiplicity.One: return $".WithRequired({withExpression}){hasForeignKey}";
            case RelationshipMultiplicity.Many:
                // Many to many relationship
                if (description.From.RelationshipType == RelationshipMultiplicity.Many)
                {
                    return $".WithMany({withExpression}).Map(m => {NEW_LINE}" +
                            $"{Indent(METHOD_INDENT + 1)}{{" +
                            NEW_LINE +
                            $"{Indent(4)}m.ToTable(\"{description.JoinTableName}\");{NEW_LINE}" +
                            $"{Indent(4)}m.MapLeftKey(\"{description.From.JoinKeyName}\");{NEW_LINE}" +
                            $"{Indent(4)}m.MapRightKey(\"{description.To.JoinKeyName}\");{NEW_LINE}" +
                            $"{Indent(METHOD_INDENT + 1)}}})";
                }

                return $".WithMany({withExpression}){hasForeignKey}";
            default:
                throw new ArgumentOutOfRangeException(nameof(description.To.RelationshipType));
        }
    }

    private string Indent(int count) => new string(' ', count * INDENT_SIZE);
}
