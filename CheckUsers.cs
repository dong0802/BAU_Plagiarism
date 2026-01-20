using BAU_Plagiarism_System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<BAUDbContext>(options =>
    options.UseSqlServer("Server=DONG2004;Database=BAU_Plagiarism_DB;User Id=sa;Password=2004;TrustServerCertificate=True;MultipleActiveResultSets=true"));

using IHost host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BAUDbContext>();
    var users = await context.Users.ToListAsync();
    Console.WriteLine("--- CURRENT USERS IN DB ---");
    foreach (var u in users)
    {
        Console.WriteLine($"ID: {u.Id} | Username: {u.Username} | Email: {u.Email}");
    }
    Console.WriteLine("---------------------------");
}
