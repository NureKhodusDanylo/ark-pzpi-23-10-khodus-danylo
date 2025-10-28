using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using Entities.Models;

namespace Infrastructure
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Robot> Robots { get; set; }
        public DbSet<Node> Nodes { get; set; }
        public DbSet<Partner> Partners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SentOrders)
                .WithOne(o => o.Sender)
                .HasForeignKey(o => o.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReceivedOrders)
                .WithOne(o => o.Recipient)
                .HasForeignKey(o => o.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.AssignedRobot)
                .WithMany(r => r.ActiveOrders)
                .HasForeignKey(o => o.RobotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.PickupNode)
                .WithMany()
                .HasForeignKey(o => o.PickupNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.DropoffNode)
                .WithMany()
                .HasForeignKey(o => o.DropoffNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Robot>()
               .HasOne(r => r.CurrentNode)
               .WithMany()
               .HasForeignKey(r => r.CurrentNodeId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Robot>()
               .HasOne(r => r.TargetNode)
               .WithMany()
               .HasForeignKey(r => r.TargetNodeId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

            // Add unique constraint for robot serial number
            modelBuilder.Entity<Robot>()
                .HasIndex(r => r.SerialNumber)
                .IsUnique();

            // User's personal node relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.PersonalNode)
                .WithMany()
                .HasForeignKey(u => u.PersonalNodeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}