using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Models;

namespace OfflinePaymentLinks.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<KYCInformation> KYC_Information { get; set; }
    public DbSet<PolicyInformation> PolicyInformation { get; set; }
    public DbSet<PinCodeData> PinCodeData { get; set; }
    public DbSet<UrlMapping> UrlMappings { get; set; }
    public DbSet<PrePaymentData> PrePaymentData { get; set; }

    public DbSet<RolePermission> RolePermissions { get; set; }

    public DbSet<NameMatchLog> NameMatchLogs { get; set; }
    public DbSet<Product> Products { get; set; }

    public DbSet<PostPaymentData> PostPaymentData { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //modelBuilder.Entity<KYCInformation>().ToTable("KYC_Information");
        //modelBuilder.Entity<KYCInformation>().HasKey(k => k.KYC_ID);
        modelBuilder.Entity<Product>(e => {
            e.ToTable("Products");
            e.HasKey(p => p.Id);
        });

        modelBuilder.Entity<PolicyInformation>().ToTable("PolicyInformation");
        modelBuilder.Entity<PolicyInformation>().HasKey(p => p.PolicyNumber);

        modelBuilder.Entity<PinCodeData>().ToTable("PinCodeData");
        modelBuilder.Entity<PinCodeData>().HasKey(p => p.PinCode);

        modelBuilder.Entity<KYCInformation>(e =>
        {
            e.ToTable("KYC_Information");
            e.HasKey(k => k.KYC_ID);
            e.Property(k => k.KYC_ID).HasColumnName("KYC_ID");
            // Ignore columns that exist in DB but not in model
            e.Ignore("AdhaarNumber");
            e.Ignore("GSTNumber");
            e.Ignore("DOI");
            e.Ignore("Consent");
            e.Ignore("CreatedDate");
            e.Ignore("StatusModifiedDate");
            e.Ignore("CreateType");
        });

        modelBuilder.Entity<PolicyInformation>()
    .Property(p => p.Amount)
    .HasPrecision(18, 2);

        modelBuilder.Entity<PrePaymentData>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RolePermission>()
    .HasIndex(r => r.RoleId)
    .IsUnique();
    }
}