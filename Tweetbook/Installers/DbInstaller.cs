using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Tweetbook.Data;
using Tweetbook.Services;

namespace Tweetbook.Installers
{
    public class DbInstaller : IInstaller
    {
        public void InstallServices(IServiceCollection services, IConfiguration configuration) {
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));
            var builder = services.AddDefaultIdentity<IdentityUser>();
            builder.AddEntityFrameworkStores<DataContext>();
            services.AddScoped<IPostService, PostService>();
            //services.AddSingleton<IPostService, CosmosPostService>();
        }
    }
}
