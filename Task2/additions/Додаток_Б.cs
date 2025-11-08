//Скрипт створення БД:

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using Entities.Models;
using FileEntity = Entities.Models.File;

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
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<AdminKey> AdminKeys { get; set; }

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

            // File relationships
            modelBuilder.Entity<FileEntity>()
                .HasOne(f => f.Order)
                .WithMany(o => o.Images)
                .HasForeignKey(f => f.OrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FileEntity>()
                .HasOne(f => f.User)
                .WithOne(u => u.ProfilePhoto)
                .HasForeignKey<User>(u => u.ProfilePhotoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // AdminKey relationships
            modelBuilder.Entity<AdminKey>()
                .HasOne(ak => ak.UsedByUser)
                .WithMany()
                .HasForeignKey(ak => ak.UsedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminKey>()
                .HasOne(ak => ak.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(ak => ak.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add unique index for KeyCode
            modelBuilder.Entity<AdminKey>()
                .HasIndex(ak => ak.KeyCode)
                .IsUnique();
        }
    }
}

//Репозиторії:
using Application.Abstractions.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

/// <summary>
/// Repository implementation for AdminKey entity
/// </summary>
public class AdminKeyRepository : GenericRepository<AdminKey>, IAdminKeyRepository
{
    public AdminKeyRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<AdminKey?> GetByIdAsync(int id)
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Include(ak => ak.UsedByUser)
            .FirstOrDefaultAsync(ak => ak.Id == id);
    }

    public async Task<AdminKey?> GetByKeyCodeAsync(string keyCode)
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Include(ak => ak.UsedByUser)
            .FirstOrDefaultAsync(ak => ak.KeyCode == keyCode);
    }

    public async Task<IEnumerable<AdminKey>> GetAllAsync()
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Include(ak => ak.UsedByUser)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AdminKey>> GetUnusedKeysAsync()
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Where(ak => !ak.IsUsed)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AdminKey>> GetKeysByAdminAsync(int adminId)
    {
        return await _context.AdminKeys
            .Include(ak => ak.UsedByUser)
            .Where(ak => ak.CreatedByAdminId == adminId)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsKeyValidAsync(string keyCode)
    {
        var key = await _context.AdminKeys
            .FirstOrDefaultAsync(ak => ak.KeyCode == keyCode);

        if (key == null || key.IsUsed)
            return false;

        // Check if key is expired
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    public async Task AddAsync(AdminKey adminKey)
    {
        await _context.AdminKeys.AddAsync(adminKey);
    }

    public async Task UpdateAsync(AdminKey adminKey)
    {
        _context.AdminKeys.Update(adminKey);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using FileEntity = Entities.Models.File;

namespace Infrastructure.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly MyDbContext _context;

        public FileRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<FileEntity?> GetByIdAsync(int fileId)
        {
            return await _context.Files.FindAsync(fileId);
        }

        public async Task<IEnumerable<FileEntity>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Files
                .Where(f => f.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<FileEntity?> GetUserProfilePhotoAsync(int userId)
        {
            return await _context.Files
                .FirstOrDefaultAsync(f => f.UserId == userId);
        }

        public async Task AddAsync(FileEntity file)
        {
            await _context.Files.AddAsync(file);
        }

        public async Task DeleteAsync(int fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file != null)
            {
                _context.Files.Remove(file);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
using Entities.Interfaces;

namespace Infrastructure.Repository
{
    public abstract class GenericRepository<T> where T : class, IDbEntity
    {
        protected MyDbContext _context;
        public GenericRepository(MyDbContext context)
        {
            _context = context;
        }
        public virtual async Task AddAsync(T? entity)
        {
            if (entity != null)
            {
                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();
            }
        }
        public virtual async Task<T?> FindAsync(ulong Id)
        {
            return await _context.FindAsync<T>(Id);
        }
        public virtual async Task RemoveAsync(T? entity)
        {
            T? result = entity == null ? null : _context.Remove(entity).Entity;
            if (result != null)
                await _context.SaveChangesAsync();
        }
        public virtual async Task RemoveByIdAsync(ulong Id)
        {
            T? entity = await FindAsync(Id);
            await RemoveAsync(entity);
        }
        public virtual async Task Update(T? entity)
        {
            if (entity != null)
            {
                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
        }
        public virtual async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class NodeRepository : GenericRepository<Node>, INodeRepository
    {
        private readonly MyDbContext _context;

        public NodeRepository(MyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Node?> GetByIdAsync(int nodeId)
        {
            return await _context.Nodes.FindAsync(nodeId);
        }

        public async Task<IEnumerable<Node>> GetAllAsync()
        {
            return await _context.Nodes
                .OrderBy(n => n.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Node>> GetByTypeAsync(NodeType type)
        {
            return await _context.Nodes
                .Where(n => n.Type == type)
                .OrderBy(n => n.Name)
                .ToListAsync();
        }

        public async Task<Node?> FindNearestNodeAsync(double latitude, double longitude, NodeType? type = null)
        {
            var query = _context.Nodes.AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(n => n.Type == type.Value);
            }

            // Calculate distance using Haversine formula approximation
            // For simplicity, using Pythagorean theorem (works for short distances)
            var nodes = await query.ToListAsync();

            return nodes
                .OrderBy(n => Math.Pow(n.Latitude - latitude, 2) + Math.Pow(n.Longitude - longitude, 2))
                .FirstOrDefault();
        }

        public async Task UpdateAsync(Node node)
        {
            _context.Nodes.Update(node);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int nodeId)
        {
            var node = await _context.Nodes.FindAsync(nodeId);
            if (node != null)
            {
                _context.Nodes.Remove(node);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int nodeId)
        {
            return await _context.Nodes.AnyAsync(n => n.Id == nodeId);
        }
    }
}
using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly MyDbContext _context;

        public OrderRepository(MyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetBySenderIdAsync(int senderId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.SenderId == senderId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByRecipientIdAsync(int recipientId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.RecipientId == recipientId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.SenderId == userId || o.RecipientId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(o => o.Id == orderId);
        }
    }
}
using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class RobotRepository : GenericRepository<Robot>, IRobotRepository
    {
        private readonly MyDbContext _context;

        public RobotRepository(MyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Robot?> GetByIdAsync(int robotId)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .FirstOrDefaultAsync(r => r.Id == robotId);
        }

        public async Task<IEnumerable<Robot>> GetAllAsync()
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetByStatusAsync(RobotStatus status)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r => r.Status == status)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetByTypeAsync(RobotType type)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r => r.Type == type)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetByTypeAndStatusAsync(RobotType type, RobotStatus status)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r => r.Type == type && r.Status == status)
                .OrderByDescending(r => r.BatteryLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetAvailableRobotsAsync()
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r => r.Status == RobotStatus.Idle && r.BatteryLevel > 20)
                .OrderByDescending(r => r.BatteryLevel)
                .ToListAsync();
        }

        public async Task UpdateAsync(Robot robot)
        {
            _context.Robots.Update(robot);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int robotId)
        {
            var robot = await _context.Robots.FindAsync(robotId);
            if (robot != null)
            {
                _context.Robots.Remove(robot);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int robotId)
        {
            return await _context.Robots.AnyAsync(r => r.Id == robotId);
        }

        public async Task<Robot?> GetBySerialNumberAsync(string serialNumber)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.TargetNode)
                .Include(r => r.ActiveOrders)
                .FirstOrDefaultAsync(r => r.SerialNumber == serialNumber);
        }

        public async Task<bool> SerialNumberExistsAsync(string serialNumber)
        {
            return await _context.Robots.AnyAsync(r => r.SerialNumber == serialNumber);
        }
    }
}
using Application.Abstractions.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repository
{
    public class UserRepository(MyDbContext _context) : GenericRepository<User>(_context), IUserRepository
    {
        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.SentOrders)
                .Include(u => u.ReceivedOrders)
                .Include(u => u.PersonalNode)
                .Include(u => u.ProfilePhoto)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetByIdMainAsync(ulong userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.SentOrders)
                .Include(u => u.ReceivedOrders)
                .Include(u => u.PersonalNode)
                .Include(u => u.ProfilePhoto)
                .ToListAsync();
        }
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Users.AnyAsync(u => u.UserName == name);
        }
        public async Task<bool> IsPasswordValidNasmeAsync(string name, string passwordHash)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == name);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return false;

            return user.PasswordHash == passwordHash;
        }
        public async Task<bool> IsPasswordValidByEmailAsync(string email, string passwordHash)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return false;

            return user.PasswordHash == passwordHash;
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string query)
        {
            var lowerQuery = query.ToLower();

            return await _context.Users
                .Include(u => u.PersonalNode)
                .Where(u =>
                    u.UserName.ToLower().Contains(lowerQuery) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(query)) ||
                    (u.PersonalNode != null && u.PersonalNode.Name.ToLower().Contains(lowerQuery))
                )
                .ToListAsync();
        }
    }
}
