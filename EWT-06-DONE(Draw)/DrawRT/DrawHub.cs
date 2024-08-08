using Microsoft.AspNetCore.SignalR;

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class Command
{
    public string Name { get; set; }
    public object[] Param { get; set; }
    public Command(string name, params object[] param) => (Name, Param) = (name, param);
}

public class DrawHub : Hub
{
    private static List<Command> commands = [];

    public async Task SendLine(Point a, Point b, int size, string color)
    {
        commands.Add(new("drawLine", a, b, size, color));
        await Clients.Others.SendAsync("ReceiveLine", a, b, size, color);
    }

    public async Task SendCurve(Point a, Point b, Point c, int size, string color)
    {
        commands.Add(new("drawCurve", a, b, c, size, color));
        await Clients.Others.SendAsync("ReceiveCurve", a, b, c, size, color);
    }

    public async Task SendImage(string url)
    {
        commands.Clear();
        commands.Add(new("drawImage", url));
        await Clients.Others.SendAsync("ReceiveImage", url);
    }

    public async Task SendClear()
    {
        commands.Clear();
        await Clients.Others.SendAsync("ReceiveClear");
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveCommands", commands);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}