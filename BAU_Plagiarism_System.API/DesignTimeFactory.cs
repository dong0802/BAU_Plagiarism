using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using BAU_Plagiarism_System.Data;

namespace BAU_Plagiarism_System.API
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BAUDbContext>
    {
        public BAUDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BAUDbContext>();
            // Hardcoded connection string for design time to avoid configuration issues
            optionsBuilder.UseSqlServer("Server=DONG2004;Database=BAU_Plagiarism_DB;User Id=sa;Password=2004;TrustServerCertificate=True;MultipleActiveResultSets=true");

            return new BAUDbContext(optionsBuilder.Options);
        }
    }
}
