var builder = WebApplication.CreateBuilder(args);
// Maximum 128KB
builder.Services.AddSignalR(options => {
    options.MaximumReceiveMessageSize = 128 * 1024;
});

var app = builder.Build();
app.UseFileServer();
app.MapHub<DrawHub>("/hub");
app.Run();