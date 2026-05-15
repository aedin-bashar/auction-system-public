using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<AuctionImage> AuctionImages => Set<AuctionImage>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<AuctionReport> AuctionReports => Set<AuctionReport>();
    public DbSet<AdminTransactionRefund> AdminTransactionRefunds => Set<AdminTransactionRefund>();
    public DbSet<AdminSystemSetting> AdminSystemSettings => Set<AdminSystemSetting>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<UserPassword> UserPasswords => Set<UserPassword>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureAuctions(modelBuilder);
        ConfigureAuctionImages(modelBuilder);
        ConfigureBids(modelBuilder);
        ConfigureAuctionReports(modelBuilder);
        ConfigureAdminTransactionRefunds(modelBuilder);
        ConfigureAdminSystemSettings(modelBuilder);
        ConfigurePaymentMethods(modelBuilder);
        ConfigureUserPasswords(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        user.ToTable("Users");

        user.HasKey(x => x.Id);
        user.Property(x => x.Id).ValueGeneratedNever();

        user.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        user.HasIndex(x => x.Email)
            .IsUnique();

        user.Property(x => x.FullName)
            .HasMaxLength(100)
            .IsRequired();

        user.Property(x => x.PhoneNumber)
            .HasMaxLength(32);

        user.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        user.Property(x => x.IsActive).IsRequired();
        user.Property(x => x.CreatedAtUtc).IsRequired();
        user.Property(x => x.UpdatedAtUtc).IsRequired();
    }

    private static void ConfigureAuctions(ModelBuilder modelBuilder)
    {
        var auction = modelBuilder.Entity<Auction>();

        auction.ToTable("Auctions");

        auction.HasKey(x => x.Id);
        auction.Property(x => x.Id).ValueGeneratedNever();

        auction.Property(x => x.SellerId).IsRequired();

        auction.Property(x => x.Title)
            .HasMaxLength(120)
            .IsRequired();

        auction.Property(x => x.Category)
            .HasMaxLength(50)
            .IsRequired();

        auction.Property(x => x.Description)
            .HasMaxLength(2000);

        auction.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        auction.Property(x => x.StartTimeUtc);
        auction.Property(x => x.EndTimeUtc).IsRequired();
        auction.Property(x => x.EndedAtUtc);

        auction.Property(x => x.CreatedAtUtc).IsRequired();
        auction.Property(x => x.UpdatedAtUtc).IsRequired();

        // Money ValueObject as owned type
        auction.OwnsOne(x => x.StartingPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("StartingAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("StartingCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Bids collection: map the public property but use field access to populate the private backing field.
        auction.HasMany(a => a.Bids)
            .WithOne()
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        auction.HasMany(a => a.Images)
            .WithOne()
            .HasForeignKey(i => i.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure EF uses the private field for the Bids navigation (avoids duplicate mapping).
        auction.Navigation(nameof(Auction.Bids)).UsePropertyAccessMode(PropertyAccessMode.Field);
        auction.Navigation(nameof(Auction.Images)).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureAuctionImages(ModelBuilder modelBuilder)
    {
        var image = modelBuilder.Entity<AuctionImage>();

        image.ToTable("Images");

        image.HasKey(x => x.Id);
        image.Property(x => x.Id).ValueGeneratedNever();
        image.Property(x => x.AuctionId).IsRequired();

        image.Property(x => x.FileName)
            .HasMaxLength(260)
            .IsRequired();

        image.Property(x => x.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        image.Property(x => x.Content)
            .IsRequired();

        image.Property(x => x.SortOrder)
            .IsRequired();

        image.Property(x => x.CreatedAtUtc)
            .IsRequired();

        image.HasIndex(x => x.AuctionId);
        image.HasIndex(x => new { x.AuctionId, x.SortOrder });
    }

    private static void ConfigureBids(ModelBuilder modelBuilder)
    {
        var bid = modelBuilder.Entity<Bid>();

        bid.ToTable("Bids");

        bid.HasKey(x => x.Id);
        bid.Property(x => x.Id).ValueGeneratedNever();

        bid.Property(x => x.AuctionId).IsRequired();
        bid.Property(x => x.BidderId).IsRequired();
        bid.Property(x => x.PlacedAtUtc).IsRequired();

        bid.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Helpful index for auction bid queries
        bid.HasIndex(x => x.AuctionId);
        bid.HasIndex(x => x.BidderId);
        bid.HasIndex(x => x.PlacedAtUtc);
    }

    private static void ConfigureAuctionReports(ModelBuilder modelBuilder)
    {
        var report = modelBuilder.Entity<AuctionReport>();

        report.ToTable("AuctionReports");

        report.HasKey(x => x.Id);
        report.Property(x => x.Id).ValueGeneratedNever();

        report.Property(x => x.AuctionId).IsRequired();
        report.Property(x => x.ReportedByUserId).IsRequired();
        report.Property(x => x.Reason).HasMaxLength(80).IsRequired();
        report.Property(x => x.Details).HasMaxLength(1000);
        report.Property(x => x.Status).HasMaxLength(20).IsRequired();
        report.Property(x => x.ResolutionNote).HasMaxLength(1000);
        report.Property(x => x.CreatedAtUtc).IsRequired();
        report.Property(x => x.UpdatedAtUtc).IsRequired();

        report.HasIndex(x => x.AuctionId);
        report.HasIndex(x => x.ReportedByUserId);
        report.HasIndex(x => x.Status);
    }

    private static void ConfigureAdminTransactionRefunds(ModelBuilder modelBuilder)
    {
        var refund = modelBuilder.Entity<AdminTransactionRefund>();

        refund.ToTable("AdminTransactionRefunds");

        refund.HasKey(x => x.TransactionId);

        refund.Property(x => x.TransactionId).ValueGeneratedNever();
        refund.Property(x => x.RefundedByUserId).IsRequired();
        refund.Property(x => x.Reason).HasMaxLength(500);
        refund.Property(x => x.RefundedAtUtc).IsRequired();
        refund.Property(x => x.CreatedAtUtc).IsRequired();
        refund.Property(x => x.UpdatedAtUtc).IsRequired();

        refund.HasIndex(x => x.RefundedByUserId);
    }

    private static void ConfigureAdminSystemSettings(ModelBuilder modelBuilder)
    {
        var setting = modelBuilder.Entity<AdminSystemSetting>();

        setting.ToTable("AdminSystemSettings");

        setting.HasKey(x => x.Key);

        setting.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();

        setting.Property(x => x.Value)
            .HasMaxLength(2000)
            .IsRequired();

        setting.Property(x => x.UpdatedAtUtc).IsRequired();
        setting.Property(x => x.UpdatedByUserId).IsRequired();

        setting.HasIndex(x => x.UpdatedByUserId);
    }

    private static void ConfigurePaymentMethods(ModelBuilder modelBuilder)
    {
        var paymentMethod = modelBuilder.Entity<PaymentMethod>();

        paymentMethod.ToTable("PaymentMethods");

        paymentMethod.HasKey(x => x.Id);
        paymentMethod.Property(x => x.Id).ValueGeneratedNever();

        paymentMethod.Property(x => x.UserId).IsRequired();

        paymentMethod.Property(x => x.Type)
            .HasMaxLength(32)
            .IsRequired();

        paymentMethod.Property(x => x.Provider)
            .HasMaxLength(64)
            .IsRequired();

        paymentMethod.Property(x => x.Last4)
            .HasMaxLength(4)
            .IsRequired();

        paymentMethod.Property(x => x.ExpiryMonth).IsRequired();
        paymentMethod.Property(x => x.ExpiryYear).IsRequired();

        paymentMethod.Property(x => x.HolderName)
            .HasMaxLength(120);

        paymentMethod.Property(x => x.IsDefault).IsRequired();
        paymentMethod.Property(x => x.CreatedAtUtc).IsRequired();
        paymentMethod.Property(x => x.UpdatedAtUtc).IsRequired();

        paymentMethod.HasIndex(x => x.UserId);
    }

    private static void ConfigureUserPasswords(ModelBuilder modelBuilder)
    {
        var userPassword = modelBuilder.Entity<UserPassword>();

        userPassword.ToTable("UserPasswords");

        userPassword.HasKey(x => x.UserId);

        userPassword.Property(x => x.PasswordHash)
            .HasMaxLength(64)
            .IsRequired();

        userPassword.Property(x => x.Salt)
            .HasMaxLength(32)
            .IsRequired();

        userPassword.Property(x => x.Iterations)
            .IsRequired();

        userPassword.Property(x => x.CreatedAtUtc)
            .IsRequired();

        userPassword.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        userPassword.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserPassword>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
