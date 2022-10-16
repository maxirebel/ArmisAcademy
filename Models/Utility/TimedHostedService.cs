using ArmisApp.Models.Identity;
using ArmisApp.Models.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArmisApp.Models.Utility
{
    // اجرای کد ها در پس زمینه به صورت زمانبندی شده
    public class TimedHostedService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TimedHostedService> _logger;
        private Timer _timer;

        public TimedHostedService(ILogger<TimedHostedService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _userManager = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromMinutes(15));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);
            PayRepository Rep_Pay = new PayRepository();
            Task task = Rep_Pay.CheckAndPayFinishedSe(_userManager);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
