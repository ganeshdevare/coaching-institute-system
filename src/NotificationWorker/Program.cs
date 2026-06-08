using SharedKernel;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCoachSharedKernel(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

public sealed class Worker(AppDataStore store, IClock clock, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            while (store.Events.TryDequeue(out var item))
            {
                if (item.Name == EventNames.ReportRequested)
                {
                    var report = store.ReportJobs.Values.FirstOrDefault(x => item.Payload.Contains(x.Id.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (report is not null)
                    {
                        store.ReportJobs[report.Id] = report with
                        {
                            Status = "Completed",
                            ResultPath = $"/reports/{report.Id:N}-{report.ReportType}.xlsx"
                        };
                    }
                }

                var notification = new NotificationRecord(Guid.NewGuid(), item.InstituteId, item.Name, "mock-recipient@coachapp.local", item.Payload, "Sent", clock.UtcNow);
                store.Notifications[notification.Id] = notification;
                logger.LogInformation("Mock notification sent for {EventName} with correlation payload {Payload}", item.Name, item.Payload);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
