﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drive.DataAccess.Entities;

namespace Drive.DataAccess.Context
{
    public class DriveContext : DbContext
    {
        public DriveContext() : base()
        {
            //to change when DB structure is stable!!!
            Database.SetInitializer<DriveContext>(new DropCreateDatabaseAlways<DriveContext>());
        }

        public DbSet<FolderUnit> Folders { get; set; }
        public DbSet<FileUnit> Files { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Space> Spaces { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataUnit>().
                HasMany<User>(so => so.ReadPermittedUsers).
                WithMany(u => u.ReadPermissionDataUnits).
                Map(
                    m =>
                    {
                        m.MapLeftKey("DataUnitId");
                        m.MapRightKey("UserId");
                        m.ToTable("DataUnitUserReadPermissions");
                    });

            modelBuilder.Entity<DataUnit>().
                HasMany<User>(so => so.ModifyPermittedUsers).
                WithMany(u => u.ModifyPermissionDataUnits).
                Map(
                    m =>
                    {
                        m.MapLeftKey("DataUnitId");
                        m.MapRightKey("UserId");
                        m.ToTable("DataUnitUserModifyPermissions");
                    });

            modelBuilder.Entity<DataUnit>().
                HasMany<Role>(so => so.ReadPermittedRoles).
                WithMany(r => r.ReadPermissionDataUnits).
                Map(
                    m =>
                    {
                        m.MapLeftKey("DataUnitId");
                        m.MapRightKey("RoleId");
                        m.ToTable("DataUnitRoleReadPermissions");
                    });

            modelBuilder.Entity<DataUnit>().
                HasMany<Role>(so => so.MorifyPermittedRoles).
                WithMany(r => r.ModifyPermissionDataUnits).
                Map(
                    m =>
                    {
                        m.MapLeftKey("DataUnitId");
                        m.MapRightKey("RoleId");
                        m.ToTable("DataUnitRoleModifyPermissions");
                    });

            modelBuilder.Entity<User>()
                .HasMany<Space>(s => s.ModifyPermissionSpaces)
                .WithMany(u => u.ModifyPermittedUsers)
                .Map(us =>
                {
                    us.MapLeftKey("UserID");
                    us.MapRightKey("SpaceID");
                    us.ToTable("SpaceUserModifyPermissions");
                });

            modelBuilder.Entity<User>()
                .HasMany<Space>(s => s.ReadPermissionSpaces)
                .WithMany(u => u.ReadPermittedUsers)
                .Map(us =>
                {
                    us.MapRightKey("UserID");
                    us.MapLeftKey("SpaceID");
                    us.ToTable("SpaceUserReadPermissions");
                });
            modelBuilder.Entity<Role>()
                .HasMany<Space>(s => s.ModifyPermissionSpaces)
                .WithMany(r => r.ModifyPermittedRoles)
                .Map(rs =>
                {
                    rs.MapLeftKey("RoleID");
                    rs.MapRightKey("SpaceID");
                    rs.ToTable("SpaceRoleModifyPermissions");
                });

            modelBuilder.Entity<Role>()
                .HasMany<Space>(s => s.ReadPermissionSpaces)
                .WithMany(r => r.ReadPermittedRoles)
                .Map(rs =>
                {
                    rs.MapLeftKey("RoleID");
                    rs.MapRightKey("SpaceID");
                    rs.ToTable("SpaceRoleReadPermissions");
                });

        }


    }
}