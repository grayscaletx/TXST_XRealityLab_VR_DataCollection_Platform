using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

        public virtual DbSet<Lab> Lab { get; set; }
        public virtual DbSet<LabUser> LabUser { get; set; }
        public virtual DbSet<Organization> Organization { get; set; }
        public virtual DbSet<OrganizationLab> OrganizationLab { get; set; }
        public virtual DbSet<Participant> Participant { get; set; }
        public virtual DbSet<Project> Project { get; set; }
        public virtual DbSet<ProjectResearch> ProjectResearch { get; set; }
        public virtual DbSet<ProjectSession> ProjectSession { get; set; }
        public virtual DbSet<ProjectUser> ProjectUser { get; set; }
        public virtual DbSet<Research> Research { get; set; }
        public virtual DbSet<ResearchMethod> ResearchMethod { get; set; }
        public virtual DbSet<Role> Role { get; set; }
        public virtual DbSet<Session> Session { get; set; }
        public virtual DbSet<SessionParticipant> SessionParticipant { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=xrealitylab.database.windows.net, 1433;Database=XRealityLabdb;User ID=xrealitylab;Password=fy*%trep5w;Integrated Security=False");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lab>(entity =>
            {
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

                entity.Property(e => e.PhotoUrl)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Registered).HasColumnType("datetime");

                entity.HasOne(d => d.LabDirector)
                    .WithMany(p => p.Lab)
                    .HasForeignKey(d => d.LabDirectorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Lab_User");
            });

            modelBuilder.Entity<LabUser>(entity =>
            {
                entity.Property(e => e.LabUserId).HasColumnName("LabUserID");

                entity.Property(e => e.LabId).HasColumnName("LabID");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Lab)
                    .WithMany(p => p.LabUser)
                    .HasForeignKey(d => d.LabId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LabUser_Lab");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.LabUser)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LabUser_Role");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.LabUser)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LabUser_User");
            });

            modelBuilder.Entity<Organization>(entity =>
            {
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
                    .HasColumnName("ZIPCode")
                    .HasMaxLength(10);

                entity.HasOne(d => d.Itdirector)
                    .WithMany(p => p.Organization)
                    .HasForeignKey(d => d.ItdirectorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Organization_User");
            });

            modelBuilder.Entity<OrganizationLab>(entity =>
            {
                entity.Property(e => e.OrganizationLabId).HasColumnName("OrganizationLabID");

                entity.Property(e => e.LabId).HasColumnName("LabID");

                entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

                entity.HasOne(d => d.Lab)
                    .WithMany(p => p.OrganizationLab)
                    .HasForeignKey(d => d.LabId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrganizationLab_Lab");

                entity.HasOne(d => d.Organization)
                    .WithMany(p => p.OrganizationLab)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrganizationLab_Organization");
            });

            modelBuilder.Entity<Participant>(entity =>
            {
                entity.Property(e => e.ParticipantId).HasColumnName("ParticipantID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Registered).HasColumnType("datetime");
            });

            modelBuilder.Entity<Project>(entity =>
            {
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

                entity.Property(e => e.ProjectReseachId).HasColumnName("ProjectReseachID");

                entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

                entity.Property(e => e.ResearchId).HasColumnName("ResearchID");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectResearch)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectResearch_Project");

                entity.HasOne(d => d.Research)
                    .WithMany(p => p.ProjectResearch)
                    .HasForeignKey(d => d.ResearchId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectResearch_Research");
            });

            modelBuilder.Entity<ProjectSession>(entity =>
            {
                entity.Property(e => e.ProjectSessionId).HasColumnName("ProjectSessionID");

                entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

                entity.Property(e => e.SessionId).HasColumnName("SessionID");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectSession)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectSession_Project");

                entity.HasOne(d => d.Session)
                    .WithMany(p => p.ProjectSession)
                    .HasForeignKey(d => d.SessionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectSession_Session");
            });

            modelBuilder.Entity<ProjectUser>(entity =>
            {
                entity.Property(e => e.ProjectUserId).HasColumnName("ProjectUserID");

                entity.Property(e => e.ProjectId).HasColumnName("ProjectID");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectUser)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectUser_Project");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.ProjectUser)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectUser_Role");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ProjectUser)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectUser_User");
            });

            modelBuilder.Entity<Research>(entity =>
            {
                entity.Property(e => e.ResearchId).HasColumnName("ResearchID");

                entity.Property(e => e.Registered).HasColumnType("datetime");

                entity.Property(e => e.ResearchMethodId).HasColumnName("ResearchMethodID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<ResearchMethod>(entity =>
            {
                entity.Property(e => e.ResearchMethodId).HasColumnName("ResearchMethodID");

                entity.Property(e => e.TypeId)
                    .IsRequired()
                    .HasColumnName("TypeID")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.RoleId)
                    .HasColumnName("RoleID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.Property(e => e.SessionId).HasColumnName("SessionID");

                entity.Property(e => e.Description).HasMaxLength(250);

                entity.Property(e => e.Registered).HasColumnType("datetime");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<SessionParticipant>(entity =>
            {
                entity.Property(e => e.SessionParticipantId).HasColumnName("SessionParticipantID");

                entity.Property(e => e.ParticipantId).HasColumnName("ParticipantID");

                entity.Property(e => e.SessionId).HasColumnName("SessionID");

                entity.HasOne(d => d.Participant)
                    .WithMany(p => p.SessionParticipant)
                    .HasForeignKey(d => d.ParticipantId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SessionParticipant_Participant");

                entity.HasOne(d => d.Session)
                    .WithMany(p => p.SessionParticipant)
                    .HasForeignKey(d => d.SessionId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SessionParticipant_Session");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Firstname)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.IdentityId)
                    .IsRequired()
                    .HasColumnName("IdentityID")
                    .HasMaxLength(50);

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
                    .WithMany(p => p.User)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_Role");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
