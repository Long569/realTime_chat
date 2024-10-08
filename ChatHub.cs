using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    
    private static List<string> names = new List<string>();
    private static Dictionary<string, string> messages = new Dictionary<string, string>();

   public async Task SendText(string name, string message)
{
    var messageId = Guid.NewGuid().ToString();
    var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
    messages[messageId] = message;
    await Clients.Caller.SendAsync("ReceiveText", name, message, "caller", messageId, timestamp);
    await Clients.Others.SendAsync("ReceiveText", name, message, "others", messageId, timestamp);
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
    var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
    await Clients.Caller.SendAsync("ReceiveImage", name, url, "caller", messageId, timestamp);
    await Clients.Others.SendAsync("ReceiveImage", name, url, "others", messageId, timestamp);
}

    public async Task SendYouTube(string name, string id)
    {
        var messageId = Guid.NewGuid().ToString();
        var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        await Clients.Caller.SendAsync("ReceiveYouTube", name, id, "caller", messageId, timestamp);
        await Clients.Others.SendAsync("ReceiveYouTube", name, id, "others", messageId, timestamp);
    }

    public async Task SendFile(string name, string url, string filename)
    {
        var messageId = Guid.NewGuid().ToString();
        var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        await Clients.Caller.SendAsync("ReceiveFile", name, url, filename, "caller", messageId, timestamp);
        await Clients.Others.SendAsync("ReceiveFile", name, url, filename, "others", messageId, timestamp);
    }

    public override async Task OnConnectedAsync()
    {
        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();

        switch (page)
        {
            case "chat": await ChatConnected(); break;
            case "list": await ListConnected(); break;
            case "game": await GameConnected(); break;
        }

        await base.OnConnectedAsync();
    }

    private async Task ChatConnected()
    {
        count++;
        string name = Context.GetHttpContext()!.Request.Query["name"];
        names.Add(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined", names);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();

        switch (page)
        {
            case "chat": await ChatDisconnected(); break;
            case "list": await ListDisconnected(); break;
            case "game": await GameDisconnected(); break;
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task ChatDisconnected()
    {
        count--;
        string name = Context.GetHttpContext()!.Request.Query["name"];
        names.Remove(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> left", names);
    }

    public async Task RequestGameList()
    {
        await Clients.Caller.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Game Hub
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    private static List<Game> games = new();

    public string Create(string gameType)
    {
        var game = new Game(gameType);
        games.Add(game);
        return game.Id;
    }

    public async Task PlayerReady(string gameId, string letter, bool isReady)
    {
        var game = games.Find(g => g.Id == gameId);
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        var player = letter == "A" ? game.PlayerA : game.PlayerB;
        if (player != null)
        {
            player.IsReady = isReady;
            var status = isReady ? "Readyed" : "Unreadyed";
            await Clients.Group(gameId).SendAsync("UpdateReady", $"<b>{name}</b>: {status}");
        }

        if (game.AreBothPlayersReady())
        {
            await Clients.Group(gameId).SendAsync("Start");
        }
    }

    public async Task Pull(string letter)
    {
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();
        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        if (game.Pull(letter))
        {
            if (game.IsGameOver)
            {
                await Clients.Group(gameId).SendAsync("GameOver", letter);
            }
            else
            {
                await Clients.Group(gameId).SendAsync("RoundWin", letter, game.ScoreA, game.ScoreB, game.CurrentRound);
                await Clients.Group(gameId).SendAsync("ResetRound", game.RopePosition);
            }
        }
        else
        {
            await Clients.Group(gameId).SendAsync("Move", letter, game.RopePosition);
        }
    }

    public async Task Run(string letter)
    {
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();

        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }
        var player = letter == "A" ? game.PlayerA : game.PlayerB;
        if (player == null) return;
        player.Run();
        await Clients.Group(gameId).SendAsync("Move", letter, player.Count);
        if (player.IsWin)
        {
            await Clients.Group(gameId).SendAsync("Win", letter);
        }
    }

    public async Task FreezeOpponent(string gameId, string letter)
    {
        var game = games.Find(g => g.Id == gameId);
        if (game == null) return;

        var opponent = letter == "A" ? game.PlayerB : game.PlayerA;
        if (opponent == null) return;

        await Clients.Client(opponent.Id).SendAsync("Freeze");
        await Clients.Client(opponent.Id).SendAsync("ShowFrozenMessage");
    }

    private async Task ListConnected()
    {
        await Clients.Caller.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
    }

    private async Task GameConnected()
    {
        string id = Context.ConnectionId;
        string icon = Context.GetHttpContext()!.Request.Query["icon"].ToString();
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();

        var game = games.Find(g => g.Id == gameId);
        if (game == null || game.IsFull)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        var player = new Player(id, icon, name);
        var letter = game.AddPlayer(player);

        await Groups.AddToGroupAsync(id, gameId);
        await Clients.Group(gameId).SendAsync("Ready", letter, game);
        await Clients.All.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
        await Clients.Group(gameId).SendAsync("UpdateGameStatus", $"<b>{name}</b>: joined");

        if (game.IsFull && game.AreBothPlayersReady())
        {
            await Clients.Group(gameId).SendAsync("Start");
        }
    }

    private async Task ListDisconnected()
    {
        await Task.CompletedTask;
    }

    private async Task GameDisconnected()
    {
        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();
        string id = Context.ConnectionId;
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();

        var game = games.Find(g => g.Id == gameId);
        if (game == null) return;

        Player? leavingPlayer = null;

        if (game.PlayerA?.Id == id)
        {
            leavingPlayer = game.PlayerA;
            game.PlayerA = null;
        }
        else if (game.PlayerB?.Id == id)
        {
            leavingPlayer = game.PlayerB;
            game.PlayerB = null;
        }
        await Clients.Group(gameId).SendAsync("UpdateGameStatus", $"<b>{name}</b>: left");

        if (leavingPlayer != null)
        {
            if (!leavingPlayer.IsReady)
            {
                await Clients.Group(gameId).SendAsync("PlayerLeft");

                if (game.IsEmpty)
                {
                    games.Remove(game);
                    await Clients.All.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
                }
                else
                {
                    game.IsWaiting = true;
                    await Clients.Group(gameId).SendAsync("UpdateGame", game);
                    await Clients.All.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
                }
            }
            else
            {
                // If the player was ready, end the game and notify the remaining player
                await Clients.Group(gameId).SendAsync("Left");

                games.Remove(game);
            }
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////////
// Game Hub
////////////////////////////////////////////////////////////////////////////////////////

// ============================================================================================
// Class: Player
// ============================================================================================

public class Player
{
    private const int STEP = 1; // This is the step used in the Press Game
    private const int FINISH = 100;
    public string Id { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public int Count { get; set; } = 0; // Used in Press Game
    public bool IsWin => Count >= FINISH;
    public bool IsReady { get; set; } = false; // New property to track readiness
    public int WinCount { get; set; } = 0;

    public Player(string id, string icon, string name)
    {
        Id = id;
        Icon = icon;
        Name = name;
    }

    // Method used in Press Game to increment the player's progress
    public void Run()
    {
        Count += STEP;
    }
}


// ============================================================================================
// Class: Game
// ============================================================================================

public class Game
{
    private const int TUG_OF_WAR_STEP = 1;
    private const int START_POSITION = 50; // Tug of War middle position
    private const int WIN_POSITION = 0;
    private const int LOSE_POSITION = 100;
    private const int FINISH = 100; // Press Game finish line
    private const int ROUNDS_TO_WIN = 2;

    public string Id { get; } = Guid.NewGuid().ToString();
    public Player? PlayerA { get; set; }
    public Player? PlayerB { get; set; }
    public bool IsWaiting { get; set; } = false;
    public bool IsEmpty => PlayerA == null && PlayerB == null;
    public bool IsFull => PlayerA != null && PlayerB != null;
    public int RopePosition { get; private set; } = START_POSITION; // Used in Tug of War
    public string GameType { get; set; }
    public int ScoreA { get; private set; } = 0; // Player A's score
    public int ScoreB { get; private set; } = 0; // Player B's score
    public int CurrentRound { get; private set; } = 1;
    public bool IsGameOver => ScoreA == ROUNDS_TO_WIN || ScoreB == ROUNDS_TO_WIN;

    public Game(string gameType)
    {
        GameType = gameType;
    }

    public string? AddPlayer(Player player)
    {
        if (PlayerA == null)
        {
            PlayerA = player;
            IsWaiting = true;
            return "A";
        }
        else if (PlayerB == null)
        {
            PlayerB = player;
            IsWaiting = false;
            return "B";
        }

        return null;
    }

    // Tug of War Logic
    public bool Pull(string letter)
    {
        if (letter == "A")
        {
            RopePosition -= TUG_OF_WAR_STEP;
        }
        else if (letter == "B")
        {
            RopePosition += TUG_OF_WAR_STEP;
        }

        if (RopePosition <= WIN_POSITION || RopePosition >= LOSE_POSITION)
        {
            if (letter == "A")
            {
                ScoreA++;
            }
            else
            {
                ScoreB++;
            }

            // Reset position for next round
            RopePosition = START_POSITION;
            CurrentRound++;

            return true;
        }

        return false;
    }

    // Check if both players are ready
    public bool AreBothPlayersReady()
    {
        return PlayerA?.IsReady == true && PlayerB?.IsReady == true;
    }
}}