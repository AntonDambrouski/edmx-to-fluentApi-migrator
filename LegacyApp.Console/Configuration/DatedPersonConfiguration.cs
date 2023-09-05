using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Change.Namespace.Generated
{
  internal class DatedPersonConfiguration : EntityTypeConfiguration<DatedPerson>
  {
    public DatedPersonConfiguration()
    {

    ToTable("DatedPersons", "dbo");
    Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
    Property(e => e.Name).HasColumnName("Name").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(20);
    Property(e => e.Surname).HasColumnName("Surname").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(20);
    Property(e => e.Age).HasColumnName("Age").HasColumnType("int").IsOptional();
    Property(e => e.Height).HasColumnName("Height").HasColumnType("nchar").IsOptional().IsFixedLength().HasMaxLength(10);
    Property(e => e.Weight).HasColumnName("Weight").HasColumnType("nchar").IsOptional().IsFixedLength().HasMaxLength(10);
    Property(e => e.ZodiacSign).HasColumnName("ZodiacSign").HasColumnType("nchar").IsOptional().IsFixedLength().HasMaxLength(10);
    Property(e => e.UserId).HasColumnName("UserId").HasColumnType("bigint").IsRequired();

    HasRequired(e => e.User).WithMany(e => e.DatedPersons).HasForeignKey(e => e.Id).WillCascadeOnDelete(false);
    }
  }
}
