using Microsoft.EntityFrameworkCore;

namespace WanderLost.Server.Controllers
{
    public class MerchantsDbContext : DbContext
    {
        public MerchantsDbContext(DbContextOptions<MerchantsDbContext> options) : base(options)
        {

        }
    }
}
