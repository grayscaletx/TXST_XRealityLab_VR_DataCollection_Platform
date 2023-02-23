using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class XRealityLabdbContext : DbContext
    {
        public XRealityLabdbContext()
        {
        }

        public XRealityLabdbContext(DbContextOptions<XRealityLabdbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Lab> Labs { get; set; }
        public virtual DbSet<LabUser> LabUsers { get; set; }
        public virtual DbSet<Organization> Organizations { get; set; }
        public virtual DbSet<OrganizationLab> OrganizationLabs { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<ProjectResearch> ProjectResearches { get; set; }
        public virtual DbSet<ProjectUser> ProjectUsers { get; set; }
        public virtual DbSet<Research> Researches { get; set; }
        public virtual DbSet<ResearchMethod> ResearchMethods { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=xrealitylab.database.windows.net, 1433;Database=XRealityLabdb;User ID=xrealitylab;Password=fy*%trep5w;Integrated Security=False");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<Lab>(entity =>
            {
                entity.ToTable("Lab");

                entity.Property(e => e.LabId).HasColumnName("LabID");

                entity.Property(e => e.Color)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LabDirectorId).HasColumnName("LabDirectorID");

                entity.Property(e => e.Registered).HasColumnType("datetime");

                entity.HasOne(d => d.LabDirector)
                    .WithMany(p => p.Labs)
                    .HasForeignKey(d => d.LabDirectorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Lab_User");
            });

            modelBuilder.Entity<LabUser>(entity =>
            {
                entity.ToTable("LabUser");

                entity.Property(e => e.LabUserId).HasColumnName("LabUserID");

                entity.Property(e => e.LabId).HasColumnName("LabID");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Lab)
                    .WithMany(p => p.LabUsers)
                    .HasForeignKey(d => d.LabId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LabUser_Lab");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.LabUsers)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LabUser_Role");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.LabUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LabUser_User");
            });

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("Organization");

                entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.Color).HasMaxLength(10);

                entity.Property(e => e.Country)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ItdirectorId).HasColumnName("ITDirectorID");

                entity.Property(e => e.PhotoUrl).HasMaxLength(200);

                entity.Property(e => e.Registered).HasColumnType("datetime");

                entity.Property(e => e.State)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Street)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.SubscriptionExpiration).HasColumnType("datetime");

                entity.Property(e => e.SubscriptionStarted).HasColumnType("datetime");

                entity.Property(e => e.Zipcode)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("ZIPCode");

                entity.HasOne(d => d.Itdirector)
                    .WithMany(p => p.Organizations)
                    .HasForeignKey(d => d.ItdirectorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Organization_User");
            });

            modelBuilder.Entity<OrganizationLab>(entity =>
            {
                entity.ToTable("OrganizationLab");

                entity.Property(e => e.OrganizationLabId).HasColumnName("OrganizationLabID");

                entity.Property(e => e.LabId).HasColumnName("LabID");

                entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

                entity.HasOne(d => d.Lab)
                    .WithMany(p => p.OrganizationLabs)
                    .HasForeignKey(d => d.LabId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrganizationLab_Lab");

                entity.HasOne(d => d.Organization)
                    .WithMany(p => p.OrganizationLabs)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrganizationLab_Organization");
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Project");

                entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

                entity.Property(e => e.Color)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PhotoUrl)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Registered).HasColumnType("datetime");
            });

            modelBuilder.Entity<ProjectResearch>(entity =>
            {
                entity.HasKey(e => e.ProjectReseachId);

                entity.ToTable("ProjectResearch");

                entity.Property(e => e.ProjectReseachId)
                    .ValueGeneratedNever()
                    .HasColumnName("ProjectReseachID");

                entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

                entity.Property(e => e.ResearchId).HasColumnName("ResearchID");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectResearches)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectResearch_Project");

                entity.HasOne(d => d.Research)
                    .WithMany(p => p.ProjectResearches)
                    .HasForeignKey(d => d.ResearchId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectResearch_Research");
            });

            modelBuilder.Entity<ProjectUser>(entity =>
            {
                entity.ToTable("ProjectUser");

                entity.Property(e => e.ProjectUserId).HasColumnName("ProjectUserID");

                entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectUsers)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectUser_Project");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.ProjectUsers)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectUser_Role");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ProjectUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectUser_User");
            });

            modelBuilder.Entity<Research>(entity =>
            {
                entity.ToTable("Research");

                entity.Property(e => e.ResearchId).HasColumnName("ResearchID");

                entity.Property(e => e.Registered).HasColumnType("datetime");

                entity.Property(e => e.ResearchMethodId).HasColumnName("ResearchMethodID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<ResearchMethod>(entity =>
            {
                entity.ToTable("ResearchMethod");

                entity.Property(e => e.ResearchMethodId)
                    .ValueGeneratedNever()
                    .HasColumnName("ResearchMethodID");

                entity.Property(e => e.TypeId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("TypeID");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");

                entity.Property(e => e.RoleId)
                    .ValueGeneratedNever()
                    .HasColumnName("RoleID");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Firstname)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.IdentityId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("IdentityID");

                entity.Property(e => e.Lastname)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.PhotoUrl)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.RegistrationDateTime).HasColumnType("datetime");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.Telephone).HasMaxLength(50);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_Role");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
