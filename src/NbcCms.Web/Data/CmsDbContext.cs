using Microsoft.EntityFrameworkCore;
using NbcCms.Web.Data.Entities;

namespace NbcCms.Web.Data;

public class CmsDbContext(DbContextOptions<CmsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimItem> ClaimItems => Set<ClaimItem>();
    public DbSet<ClaimStatus> ClaimStatusLookup => Set<ClaimStatus>();
    public DbSet<ClaimCorrection> ClaimCorrections => Set<ClaimCorrection>();
    public DbSet<SupportingDocument> SupportingDocuments => Set<SupportingDocument>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<ArtistBankingDetail> ArtistBankingDetails => Set<ArtistBankingDetail>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OverallAmount> OverallAmounts => Set<OverallAmount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId).HasName("PK_Users");
            e.Property(x => x.UserId).HasColumnName("userID");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(150).IsRequired();
            e.Property(x => x.FirstName).HasColumnName("firstName").HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasColumnName("lastName").HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            e.Property(x => x.ContactNumber).HasColumnName("contactNumber").HasMaxLength(30);
            e.Property(x => x.RoleId).HasColumnName("roleID");
            e.Property(x => x.IsActive).HasColumnName("isActive").HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnName("createdAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            e.Property(x => x.UpdatedBy).HasColumnName("updatedBy");
            e.Property(x => x.IdNumber).HasColumnName("idNumber").HasMaxLength(50);
            e.Property(x => x.Address).HasColumnName("address").HasMaxLength(250);
            e.Property(x => x.CreatedBy).HasColumnName("createdBy");
            e.Property(x => x.PasswordHash).HasColumnName("passwordHash").HasMaxLength(500);
            e.Ignore(x => x.FullName);
            e.HasIndex(x => x.Username).IsUnique().HasDatabaseName("UX_Users_Username");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("userRole");
            e.HasKey(x => x.RoleId).HasName("PK_userRole");
            e.Property(x => x.RoleId).HasColumnName("roleID");
            e.Property(x => x.RoleName).HasColumnName("roleName").HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            e.HasIndex(x => x.RoleName).IsUnique();
        });

        modelBuilder.Entity<ClaimStatus>(e =>
        {
            e.ToTable("claimStatus");
            e.HasKey(x => x.StatusId).HasName("PK_claimStatus");
            e.Property(x => x.StatusId).HasColumnName("statusID");
            e.Property(x => x.StatusName).HasColumnName("statusName").HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            e.HasIndex(x => x.StatusName).IsUnique();
        });

        modelBuilder.Entity<Claim>(e =>
        {
            e.ToTable("claim");
            e.HasKey(x => x.ClaimId).HasName("PK_claim");
            e.Property(x => x.ClaimId).HasColumnName("claimID");
            e.Property(x => x.ClaimantId).HasColumnName("claimantID");
            e.Property(x => x.StatusId).HasColumnName("statusID");
            e.Property(x => x.ClaimDate).HasColumnName("claimDate").HasColumnType("date");
            e.Property(x => x.SubmittedAt).HasColumnName("submittedAt");
            e.Property(x => x.ReviewedBy).HasColumnName("reviewedBy");
            e.Property(x => x.ReviewedAt).HasColumnName("reviewedAt");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            e.Property(x => x.RecordingDate).HasColumnName("recordingDate").HasColumnType("date");
            e.Property(x => x.ProgrammeTitle).HasColumnName("programmeTitle").HasMaxLength(500);
            e.Property(x => x.FeesAmount).HasColumnName("feesAmount").HasPrecision(18, 2);
            e.Property(x => x.RejectionComment).HasColumnName("rejectionComment").HasMaxLength(1000);
            e.Ignore(x => x.Reference);
            e.HasOne(x => x.Claimant).WithMany(u => u.Claims)
                .HasForeignKey(x => x.ClaimantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Status).WithMany(s => s.Claims)
                .HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Reviewer).WithMany()
                .HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.Claim)
                .HasForeignKey(i => i.ClaimId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Corrections).WithOne(c => c.Claim)
                .HasForeignKey(c => c.ClaimId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Documents).WithOne(d => d.Claim)
                .HasForeignKey(d => d.ClaimId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Report).WithOne(r => r.Claim)
                .HasForeignKey<Report>(r => r.ClaimId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimItem>(e =>
        {
            e.ToTable("claimItem");
            e.HasKey(x => x.ClaimItemId).HasName("PK_claimItem");
            e.Property(x => x.ClaimItemId).HasColumnName("claimItemID");
            e.Property(x => x.ClaimId).HasColumnName("claimID");
            e.Property(x => x.RecordingDate).HasColumnName("recordingDate").HasColumnType("date");
            e.Property(x => x.ProgrammeTitle).HasColumnName("programmeTitle").HasMaxLength(250).IsRequired();
            e.Property(x => x.Rate).HasColumnName("rate").HasPrecision(18, 2);
            e.Property(x => x.FeesAmount).HasColumnName("feesAmount").HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasColumnName("createdAt").HasDefaultValueSql("sysdatetime()");
            e.ToTable(t => t.HasCheckConstraint("CK_claimItem_feesAmount", "[feesAmount]>=(0)"));
            e.ToTable(t => t.HasCheckConstraint("CK_claimItem_rate", "[rate]>(0)"));
        });

        modelBuilder.Entity<ClaimCorrection>(e =>
        {
            e.ToTable("claimCorrection");
            e.HasKey(x => x.CorrectionId).HasName("PK_claimCorrection");
            e.Property(x => x.CorrectionId).HasColumnName("correctionID");
            e.Property(x => x.ClaimId).HasColumnName("claimID");
            e.Property(x => x.RequestedBy).HasColumnName("requestedBy");
            e.Property(x => x.CorrectionComment).HasColumnName("correctionComment").IsRequired();
            e.Property(x => x.RequestedAt).HasColumnName("requestedAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.CorrectedAt).HasColumnName("correctedAt");
            e.HasOne(x => x.RequestedByUser).WithMany()
                .HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupportingDocument>(e =>
        {
            e.ToTable("supportingDocument");
            e.HasKey(x => x.DocumentId).HasName("PK_supportingDocument");
            e.Property(x => x.DocumentId).HasColumnName("documentID");
            e.Property(x => x.ClaimId).HasColumnName("claimID");
            e.Property(x => x.UploadedBy).HasColumnName("uploadedBy");
            e.Property(x => x.FileName).HasColumnName("fileName").HasMaxLength(255).IsRequired();
            e.Property(x => x.FileURL).HasColumnName("fileURL").HasMaxLength(500).IsRequired();
            e.Property(x => x.UploadedAt).HasColumnName("uploadedAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.IsActive).HasColumnName("isActive").HasDefaultValue(true);
            e.HasOne(x => x.UploadedByUser).WithMany()
                .HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Report>(e =>
        {
            e.ToTable("report");
            e.HasKey(x => x.ReportId).HasName("PK_report");
            e.Property(x => x.ReportId).HasColumnName("reportID");
            e.Property(x => x.ClaimId).HasColumnName("claimID");
            e.Property(x => x.GeneratedBy).HasColumnName("generatedBy");
            e.Property(x => x.ReportNumber).HasColumnName("reportNumber").HasMaxLength(50).IsRequired();
            e.Property(x => x.FileName).HasColumnName("fileName").HasMaxLength(255);
            e.Property(x => x.FileURL).HasColumnName("fileURL").HasMaxLength(500);
            e.Property(x => x.GeneratedAt).HasColumnName("generatedAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.FileData).HasColumnName("fileData");
            e.HasIndex(x => x.ClaimId).IsUnique();
            e.HasIndex(x => x.ReportNumber).IsUnique();
            e.HasOne(x => x.GeneratedByUser).WithMany()
                .HasForeignKey(x => x.GeneratedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Programme>(e =>
        {
            e.ToTable("programme");
            e.HasKey(x => x.ProgrammeId).HasName("PK_programme");
            e.Property(x => x.ProgrammeId).HasColumnName("programmeID");
            e.Property(x => x.ProgrammeName).HasColumnName("programmeName").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.Property(x => x.IsActive).HasColumnName("isActive").HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasColumnName("createdAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
            e.HasIndex(x => x.ProgrammeName).IsUnique();
        });

        modelBuilder.Entity<ArtistBankingDetail>(e =>
        {
            e.ToTable("artistBankingDetails");
            e.HasKey(x => x.BankingId).HasName("PK_artistBankingDetails");
            e.Property(x => x.BankingId).HasColumnName("bankingID");
            e.Property(x => x.ArtistId).HasColumnName("artistID");
            e.Property(x => x.Bank).HasColumnName("bank").HasMaxLength(100).IsRequired();
            e.Property(x => x.Branch).HasColumnName("branch").HasMaxLength(100).IsRequired();
            e.Property(x => x.BranchCode).HasColumnName("branchCode").HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("createdAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.CreatedBy).HasColumnName("createdBy");
            e.Property(x => x.UpdatedBy).HasColumnName("updatedBy");
            e.HasIndex(x => x.ArtistId).IsUnique().HasDatabaseName("UX_artistBankingDetails_artistID");
            e.HasOne(x => x.Artist).WithOne(u => u.BankingDetail)
                .HasForeignKey<ArtistBankingDetail>(x => x.ArtistId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLog");
            e.HasKey(x => x.AuditId).HasName("PK_AuditLog");
            e.Property(x => x.AuditId).HasColumnName("auditID");
            e.Property(x => x.UserId).HasColumnName("userID");
            e.Property(x => x.TableName).HasColumnName("tableName").HasMaxLength(100).IsRequired();
            e.Property(x => x.RecordID).HasColumnName("recordID");
            e.Property(x => x.ActionType).HasColumnName("actionType").HasMaxLength(20).IsRequired();
            e.Property(x => x.ActionDate).HasColumnName("actionDate").HasDefaultValueSql("sysdatetime()");
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OverallAmount>(e =>
        {
            e.ToTable("overallAmount");
            e.HasKey(x => x.CalculationId).HasName("PK_overallAmount");
            e.Property(x => x.CalculationId).HasColumnName("calculationID");
            e.Property(x => x.ClaimId).HasColumnName("claimID");
            e.Property(x => x.Rate).HasColumnName("rate").HasPrecision(18, 2);
            e.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(10, 2);
            e.Property(x => x.CalculatedAmount).HasColumnName("calculatedAmount").HasPrecision(18, 2);
            e.Property(x => x.CalculatedBy).HasColumnName("calculatedBy");
            e.Property(x => x.CalculatedAt).HasColumnName("calculatedAt").HasDefaultValueSql("sysdatetime()");
            e.Property(x => x.ItemDescription).HasColumnName("itemDescription").HasMaxLength(250).HasDefaultValue("Secretary allocation");
            e.Property(x => x.UpdatedBy).HasColumnName("updatedBy");
            e.Property(x => x.UpdatedAt).HasColumnName("updatedAt").HasDefaultValueSql("sysdatetime()");
            e.HasOne(x => x.Claim).WithMany()
                .HasForeignKey(x => x.ClaimId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
