using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceAPI.Infrastructure.Data;

namespace FinanceCoreLedger.BackgroundServices
{
    public class DataRetentionWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataRetentionWorker> _logger;
        private readonly TimeSpan _executionInterval = TimeSpan.FromHours(24);

        public DataRetentionWorker(IServiceProvider serviceProvider, ILogger<DataRetentionWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Retention Worker inicializado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>(); 
                        var limitDate = DateTime.UtcNow.AddDays(-30);

                        // Executa limpeza de registros de telemetria
                        int deletedRows = await dbContext.Database.ExecuteSqlRawAsync(
                            "DELETE FROM clicklog WHERE created_at < {0}", 
                            limitDate, 
                            stoppingToken
                        );

                        _logger.LogInformation($"Limpeza de retenção concluída. Registros removidos: {deletedRows}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro na rotina de retenção de dados.");
                }

                await Task.Delay(_executionInterval, stoppingToken);
            }
        }
    }
}
