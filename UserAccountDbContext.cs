using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Interviews.UserAccounts
{
    public class UserAccountDbContext : DbContext
    {
        public UserAccountDbContext(DbContextOptions<UserAccountDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(user => user.Id);
            modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
            base.OnModelCreating(modelBuilder);
        }
    }

    public class EfUserRepository : IUserRepository
    {
        private readonly UserAccountDbContext _dbContext;

        public EfUserRepository(UserAccountDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<User> GetByEmailAsync(string email)
        {
            return _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email);
        }

        public async Task SaveAsync(User user)
        {
            if (_dbContext.Entry(user).State == EntityState.Detached)
                _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync();
        }
    }
}
