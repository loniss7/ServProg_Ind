using Microsoft.EntityFrameworkCore;
using ServerProg_Ind.Domain;

namespace ServerProg_Ind.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<AuctionImage> AuctionImages => Set<AuctionImage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Handle)
            .IsUnique();

        modelBuilder.Entity<Auction>()
            .Property(x => x.StartingBid)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Auction>()
            .Property(x => x.MinimumIncrement)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Auction>()
            .Property(x => x.BuyNowPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Auction>()
            .Property(x => x.CurrentBid)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Auction>()
            .Property(x => x.FinalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Bid>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Auction>()
            .HasOne(x => x.Winner)
            .WithMany()
            .HasForeignKey(x => x.WinnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Auction>()
            .HasOne(x => x.Seller)
            .WithMany(x => x.Auctions)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Auction>()
            .HasMany(x => x.Images)
            .WithOne(x => x.Auction)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Auction>()
            .HasMany(x => x.Bids)
            .WithOne(x => x.Auction)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Bid>()
            .HasOne(x => x.Bidder)
            .WithMany(x => x.Bids)
            .HasForeignKey(x => x.BidderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
