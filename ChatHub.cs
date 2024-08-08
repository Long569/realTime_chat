using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static int count = 0;
    private static List<string> names = [];

    public async Task SendText(string name, string message)
    {
        await Clients.Caller.SendAsync("ReceiveText", name, message, "caller");
        await Clients.Others.SendAsync("ReceiveText", name, message, "others");
    }

    public async Task SendImage(string name, string url)
    {
        await Clients.Caller.SendAsync("ReceiveImage", name, url, "caller");
        await Clients.Others.SendAsync("ReceiveImage", name, url, "others");
    }

    public async Task SendYouTube(string name, string id)
    {
        await Clients.Caller.SendAsync("ReceiveYouTube", name, id, "caller");
        await Clients.Others.SendAsync("ReceiveYouTube", name, id, "others");
    }

    public async Task SendFile(string name, string url, string filename)
    {
        await Clients.Caller.SendAsync("ReceiveFile", name, url, filename, "caller");
        await Clients.Others.SendAsync("ReceiveFile", name, url, filename, "others");
    }

    public override async Task OnConnectedAsync()
    {
        count++;
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();
        names.Add(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined", names);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        count--;
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();
        names.Remove(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> left", names);
        await base.OnDisconnectedAsync(exception);
    }

}