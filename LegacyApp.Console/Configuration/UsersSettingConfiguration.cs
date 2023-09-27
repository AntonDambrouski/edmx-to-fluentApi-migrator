using LegacyApp.Console;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Change.Namespace.Generated
{
  internal class UsersSettingConfiguration : EntityTypeConfiguration<UsersSetting>
  {
    public UsersSettingConfiguration()
    {
      ToTable("UsersSettings", "dbo");
      HasKey(e => e.Id);
      Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
      Property(e => e.IsVisible).HasColumnName("IsVisible").HasColumnType("bit").IsRequired();
      Property(e => e.Theme).HasColumnName("Theme").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(20);
      Property(e => e.ProfileType).HasColumnName("ProfileType").HasColumnType("nchar").IsRequired().IsFixedLength().HasMaxLength(20);

      HasRequired(e => e.User).WithOptional(e => e.UsersSetting).WillCascadeOnDelete(false);
    }
  }
}
