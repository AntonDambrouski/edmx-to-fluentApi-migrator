using LegacyApp.Console;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Change.Namespace.Generated
{
  internal class UserConfiguration : EntityTypeConfiguration<User>
  {
    public UserConfiguration()
    {
      ToTable("Users", "dbo");
      HasKey(e => e.Id);
      Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
      Property(e => e.Name).HasColumnName("Name").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(20);
      Property(e => e.Surname).HasColumnName("Surname").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(20);
      Property(e => e.Age).HasColumnName("Age").HasColumnType("int").IsRequired();
      Property(e => e.PhoneNumber).HasColumnName("PhoneNumber").HasColumnType("nchar").IsOptional().IsFixedLength().HasMaxLength(30);
      Property(e => e.Email).HasColumnName("Email").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(100);

      HasMany(e => e.DatedPersons).WithRequired(e => e.User).HasForeignKey(e => e.UserId).WillCascadeOnDelete(false);
      HasMany(e => e.Levels).WithMany().Map(m => 
      {
        m.ToTable("UsersAndLevels");
        m.MapLeftKey("LevelId");
        m.MapRightKey("UserId");
      });
      HasOptional(e => e.UsersSetting).WithRequired(e => e.User).WillCascadeOnDelete(false);
    }
  }
}
