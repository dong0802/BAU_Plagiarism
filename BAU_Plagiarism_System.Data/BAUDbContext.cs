using Microsoft.EntityFrameworkCore;
using BAU_Plagiarism_System.Data.Models;

namespace BAU_Plagiarism_System.Data
{
    public class BAUDbContext : DbContext
    {
        public BAUDbContext(DbContextOptions<BAUDbContext> options) : base(options)
        {
        }

        // Legacy table (for backward compatibility)
        public DbSet<SavedDocument> SavedDocuments { get; set; }

        // New Domain Models
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<PlagiarismCheck> PlagiarismChecks { get; set; }
        public DbSet<PlagiarismMatch> PlagiarismMatches { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SavedDocument (Legacy)
            modelBuilder.Entity<SavedDocument>(entity =>
            {
                entity.ToTable("Documents_Legacy");
                entity.HasIndex(e => e.StudentId);
            });

            // Faculty
            modelBuilder.Entity<Faculty>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Name);
            });

            // Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.FacultyId);
                
                entity.HasOne(d => d.Faculty)
                    .WithMany(f => f.Departments)
                    .HasForeignKey(d => d.FacultyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Subject
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.DepartmentId);
                
                entity.HasOne(s => s.Department)
                    .WithMany(d => d.Subjects)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.LecturerId);
                entity.HasIndex(e => e.Role);

                entity.HasOne(u => u.Faculty)
                    .WithMany(f => f.Users)
                    .HasForeignKey(u => u.FacultyId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Document
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SubjectId);
                entity.HasIndex(e => e.DocumentType);
                entity.HasIndex(e => e.UploadDate);

                entity.HasOne(d => d.User)
                    .WithMany(u => u.Documents)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Subject)
                    .WithMany(s => s.Documents)
                    .HasForeignKey(d => d.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // PlagiarismCheck
            modelBuilder.Entity<PlagiarismCheck>(entity =>
            {
                entity.HasIndex(e => e.SourceDocumentId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CheckDate);

                entity.HasOne(p => p.SourceDocument)
                    .WithMany(d => d.PlagiarismChecks)
                    .HasForeignKey(p => p.SourceDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.User)
                    .WithMany(u => u.PlagiarismChecks)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PlagiarismMatch
            modelBuilder.Entity<PlagiarismMatch>(entity =>
            {
                entity.HasIndex(e => e.PlagiarismCheckId);
                entity.HasIndex(e => e.MatchedDocumentId);

                entity.HasOne(m => m.PlagiarismCheck)
                    .WithMany(p => p.Matches)
                    .HasForeignKey(m => m.PlagiarismCheckId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.MatchedDocument)
                    .WithMany()
                    .HasForeignKey(m => m.MatchedDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
