using Microsoft.EntityFrameworkCore;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<TicketFile> TicketFiles => Set<TicketFile>();
        public DbSet<TicketFileContent> TicketFileContents => Set<TicketFileContent>();
        public DbSet<TicketComment> TicketComments => Set<TicketComment>();
        public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Category mapping
            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(c => c.CategoryId);

                e.Property(c => c.NamePl).HasMaxLength(100).IsRequired();
                e.Property(c => c.NameEn).HasMaxLength(100).IsRequired();
                e.Property(c => c.IsActive).HasColumnType("boolean").HasDefaultValue(true);

                e.Property(c => c.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
                e.Property(c => c.UpdatedAt).HasColumnType("timestamp with time zone");

                e.HasIndex(c => c.IsActive);
            });

            // Ticket mapping
            modelBuilder.Entity<Ticket>(e =>
            {
                e.Property(p => p.Title).HasMaxLength(120).IsRequired();

                e.Property(p => p.Description).HasMaxLength(5000).IsRequired();

                e.Property(p => p.Priority).HasConversion<byte>().HasColumnType("smallint");

                e.Property(p => p.Status).HasConversion<byte>().HasColumnType("smallint");

                e.Property(p => p.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                e.Property(p => p.UpdatedAt).HasColumnType("timestamp with time zone");

                e.HasIndex(p => new { p.CategoryId, p.Priority });
                e.HasIndex(p => p.CreatedById);
                e.HasIndex(p => p.AssignedToId);

                // Foreign key to Category
                e.HasOne(p => p.Category)
                    .WithMany(c => c.Tickets)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // TODO >  switch from name fields to proper FKs, you can add:
                // e.HasOne(p => p.CreatedBy).WithMany().HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.Restrict);
                // e.HasOne(p => p.AssignedTo).WithMany().HasForeignKey(p => p.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            });

            // User mapping
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.UserId);

                e.Property(u => u.Name).HasMaxLength(100).IsRequired();
                e.Property(u => u.Email).HasMaxLength(255).IsRequired();
                e.HasIndex(u => u.Email).IsUnique();

                e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
                e.Property(u => u.RoleId).HasConversion<byte>().HasColumnType("smallint");

                e.Property(u => u.IsActive).HasColumnType("boolean").HasDefaultValue(true);

                e.HasIndex(u => u.RoleId);
                e.HasIndex(u => u.CategoryId);

                // Foreign key to Category (optional)
                e.HasOne(u => u.Category)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TicketFile mapping
            modelBuilder.Entity<TicketFile>(e =>
            {
                e.ToTable("TicketFiles");
                e.HasKey(x => x.TicketFileId);

                e.Property(x => x.OriginalName).HasMaxLength(255).IsRequired();
                e.Property(x => x.StoredName).HasMaxLength(64).IsRequired();
                e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
                e.Property(x => x.SizeBytes).IsRequired();

                e.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                e.Property(x => x.StoragePath).HasMaxLength(512);
                e.Property(x => x.ChecksumSha256).HasMaxLength(64);

                e.HasIndex(x => x.TicketId);

                e.HasOne(x => x.Ticket)
                    .WithMany(t => t.Files)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.UploaderUser)
                    .WithMany()
                    .HasForeignKey(x => x.UploaderUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TicketFileContent mapping (blob)
            modelBuilder.Entity<TicketFileContent>(e =>
            {
                e.ToTable("TicketFileContents");
                e.HasKey(x => x.TicketFileId);

                // PostgreSQL bytea
                e.Property(x => x.Content).HasColumnType("bytea").IsRequired();

                e.HasOne(x => x.TicketFile)
                    .WithOne(tf => tf.Content)
                    .HasForeignKey<TicketFileContent>(x => x.TicketFileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TicketHistory mapping
            modelBuilder.Entity<TicketHistory>(e =>
            {
                e.ToTable("TicketHistories");
                e.HasKey(x => x.HistoryId);

                e.Property(x => x.ChangeType).HasMaxLength(50).IsRequired();
                e.Property(x => x.OldValue).HasMaxLength(500);
                e.Property(x => x.NewValue).HasMaxLength(500);
                e.Property(x => x.ChangedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

                e.HasIndex(x => new { x.TicketId, x.ChangedAt });
                e.HasIndex(x => x.TicketId);
            });
        }
    }
}