
using WhatsEnvio.Worker.Commons;

var builder = Host.CreateApplicationBuilder(args);

builder.AddTelemetry();

var host = builder.Build();
host.Run();
