﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LegacyApp.Console
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class AppMainEntities1 : DbContext
    {
        public AppMainEntities1()
            : base("name=AppMainEntities1")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<DatedPerson> DatedPersons { get; set; }
        public virtual DbSet<Level> Levels { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UsersSetting> UsersSettings { get; set; }
    }
}