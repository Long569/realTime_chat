using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static int count = 0;
    private static List<string> names = new List<string>();
    private static Dictionary<string, string> messages = new Dictionary<string, string>();

    public async Task SendText(string name, string message)
    {
        var messageId = Guid.NewGuid().ToString();
        messages[messageId] = message;
        await Clients.Caller.SendAsync("ReceiveText", name, message, "caller", messageId);
        await Clients.Others.SendAsync("ReceiveText", name, message, "others", messageId);
    }

    public async Task EditMessage(string messageId, string newMessage)
    {
        if (messages.ContainsKey(messageId))
        {
            messages[messageId] = newMessage;
            await Clients.All.SendAsync("UpdateMessage", messageId, newMessage);
        }
    }

    public async Task DeleteMessage(string messageId)
    {
        if (messages.ContainsKey(messageId))
        {
            messages.Remove(messageId);
            await Clients.All.SendAsync("RemoveMessage", messageId);
        }
    }

    public async Task SendImage(string name, string url)
    {
        var messageId = Guid.NewGuid().ToString();
        await Clients.Caller.SendAsync("ReceiveImage", name, url, "caller", messageId);
        await Clients.Others.SendAsync("ReceiveImage", name, url, "others", messageId);
    }

    public async Task SendYouTube(string name, string id)
    {
        var messageId = Guid.NewGuid().ToString();
        await Clients.Caller.SendAsync("ReceiveYouTube", name, id, "caller", messageId);
        await Clients.Others.SendAsync("ReceiveYouTube", name, id, "others", messageId);
    }

    public async Task SendFile(string name, string url, string filename)
    {
        var messageId = Guid.NewGuid().ToString();
        await Clients.Caller.SendAsync("ReceiveFile", name, url, filename, "caller", messageId);
        await Clients.Others.SendAsync("ReceiveFile", name, url, filename, "others", messageId);
    }

    public override async Task OnConnectedAsync()
    {
        count++;
        string name = Context.GetHttpContext()!.Request.Query["name"];
        names.Add(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined", names);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        count--;
        string name = Context.GetHttpContext()!.Request.Query["name"];
        names.Remove(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> left", names);
        await base.OnDisconnectedAsync(exception);
    }
}