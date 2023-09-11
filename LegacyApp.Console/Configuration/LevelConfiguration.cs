using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Change.Namespace.Generated
{
  internal class LevelConfiguration : EntityTypeConfiguration<Level>
  {
    public LevelConfiguration()
    {
      ToTable("Levels", "dbo");
      HasKey(e => e.Id);
      Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
      Property(e => e.Name).HasColumnName("Name").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(20);
      Property(e => e.RequiredPoints).HasColumnName("RequiredPoints").HasColumnType("bigint").IsRequired();

    }
  }
}
