using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static int count = 0;
    private static List<string> names = new List<string>();
    private static Dictionary<string, string> messages = new Dictionary<string, string>();
    //private static List<Game> games = new List<Game>();

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
        await Clients.All.SendAsync("ReceiveImage", name, url, "caller", messageId);
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
        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();

        switch (page)
        {
            case "chat": await ChatConnected(); break;
            case "list": await ListConnected(); break;
            case "game": await GameConnected(); break;
        }

        await base.OnConnectedAsync();
    }

    private async Task ChatConnected() {
        count++;
        string name = Context.GetHttpContext()!.Request.Query["name"];
        names.Add(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined", names);
        //await base.OnConnectedAsync();
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

    private async Task ChatDisconnected(){
        count--;
        string name = Context.GetHttpContext()!.Request.Query["name"];
        names.Remove(name);
        await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> left", names);
        //await base.OnDisconnectedAsync(exception);
    }

    public async Task RequestGameList()
    {
        await Clients.Caller.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
    }

    // public override async Task OnConnectedAsync()
    // {
    //     count++;
    //     string name = Context.GetHttpContext()!.Request.Query["name"];
    //     names.Add(name);
    //     await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined", names);
    //     await base.OnConnectedAsync();
    // }

    // public override async Task OnDisconnectedAsync(Exception? exception)
    // {
    //     count--;
    //     string name = Context.GetHttpContext()!.Request.Query["name"];
    //     names.Remove(name);
    //     await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> left", names);
    //     await base.OnDisconnectedAsync(exception);
    // }te

    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Game Hub
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    private static List<Game> games = new();

    // ----------------------------------------------------------------------------------------
    // Functions
    // ----------------------------------------------------------------------------------------

    public string Create(string gameType)
    {
        var game = new Game(gameType);
        games.Add(game);
        return game.Id;
    }

    // ----------------------------------------------------------------------------------------
    // Player Ready
    // ----------------------------------------------------------------------------------------
    public async Task PlayerReady(string gameId, string letter, bool isReady)
    {
        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        var player = letter == "A" ? game.PlayerA : game.PlayerB;
        if (player != null)
        {
            player.IsReady = isReady;
        }

        if (game.AreBothPlayersReady())
        {
            await Clients.Group(gameId).SendAsync("Start");
        }
    }


    // ----------------------------------------------------------------------------------------
    // Tug of War Pull
    // ----------------------------------------------------------------------------------------
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
            await Clients.Group(gameId).SendAsync("Win", letter);
        }
        else
        {
            await Clients.Group(gameId).SendAsync("Move", letter, game.RopePosition);
        }
    }

    // ----------------------------------------------------------------------------------------
    // Press Game Run
    // ----------------------------------------------------------------------------------------
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

        // Freeze the opponent
        await Clients.Client(opponent.Id).SendAsync("Freeze");
        await Clients.Client(opponent.Id).SendAsync("ShowFrozenMessage");

        // Delay to simulate the freeze duration
        // await Task.Delay(1000); // Freeze for 1 second

        // // Unfreeze the opponent after 1 second
        // await Clients.Client(opponent.Id).SendAsync("Unfreeze");
    }
    // ----------------------------------------------------------------------------------------
    // Connected
    // ----------------------------------------------------------------------------------------
    // public override async Task OnConnectedAsync()
    // {
    //     string page = Context.GetHttpContext()!.Request.Query["page"].ToString();

    //     switch (page)
    //     {
    //         case "list": await ListConnected(); break;
    //         case "game": await GameConnected(); break;
    //     }

    //     await base.OnConnectedAsync();
    // }

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

        if (game.IsFull && game.AreBothPlayersReady())
        {
            await Clients.Group(gameId).SendAsync("Start");
        }
    }

    // ----------------------------------------------------------------------------------------
    // Disconnected
    // ----------------------------------------------------------------------------------------
    // public override async Task OnDisconnectedAsync(Exception? exception)
    // {
    //     string page = Context.GetHttpContext()!.Request.Query["page"].ToString();
    //     string id = Context.ConnectionId;
    //     string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();

    //     var game = games.Find(g => g.Id == gameId);
    //     if (game == null) return;

    //     Player? leavingPlayer = null;

    //     if (game.PlayerA?.Id == id)
    //     {
    //         leavingPlayer = game.PlayerA;
    //         game.PlayerA = null;
    //     }
    //     else if (game.PlayerB?.Id == id)
    //     {
    //         leavingPlayer = game.PlayerB;
    //         game.PlayerB = null;
    //     }

    //     if (leavingPlayer != null)
    //     {
    //         // If the leaving player is not ready, allow the game to continue waiting for a new player
    //         if (!leavingPlayer.IsReady)
    //         {
    //             await Clients.Group(gameId).SendAsync("PlayerLeft", leavingPlayer.Id);

    //             if (game.IsEmpty)
    //             {
    //                 games.Remove(game);
    //                 await Clients.All.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
    //             }
    //             else
    //             {
    //                 game.IsWaiting = true;
                    
    //                 await Clients.Group(gameId).SendAsync("UpdateGame", game);
    //                 await Clients.All.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
    //             }
    //         }
    //         else
    //         {
    //             // If the player was ready, end the game and notify the remaining player
    //             await Clients.Group(gameId).SendAsync("Left", leavingPlayer.Id);

    //             games.Remove(game);
    //         }
    //     }

    //     await base.OnDisconnectedAsync(exception);
    // }

    private async Task ListDisconnected()
    {
        await Task.CompletedTask;
        
    }

    private async Task GameDisconnected()
    {
        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();
        string id = Context.ConnectionId;
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();

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

        if (leavingPlayer != null)
        {
            // If the leaving player is not ready, allow the game to continue waiting for a new player
            if (!leavingPlayer.IsReady)
            {
                await Clients.Group(gameId).SendAsync("PlayerLeft", leavingPlayer.Id);

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
                await Clients.Group(gameId).SendAsync("Left", leavingPlayer.Id);

                games.Remove(game);
            }
        }
    }

    // End of GameHub -------------------------------------------------------------------------
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
    private const int PRESS_GAME_STEP = 1;
    private const int START_POSITION = 50; // Tug of War middle position
    private const int WIN_POSITION = 0;
    private const int LOSE_POSITION = 100;
    private const int FINISH = 100; // Press Game finish line

    public string Id { get; } = Guid.NewGuid().ToString();
    public Player? PlayerA { get; set; }
    public Player? PlayerB { get; set; }
    public bool IsWaiting { get; set; } = false;
    public bool IsEmpty => PlayerA == null && PlayerB == null;
    public bool IsFull => PlayerA != null && PlayerB != null;
    public int RopePosition { get; private set; } = START_POSITION; // Used in Tug of War
    public string GameType { get; set; }

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
            return true;
        }

        return false;
    }

    // Press Game Logic
    public void Run(string letter)
    {
        var player = letter == "A" ? PlayerA : PlayerB;
        player?.Run();
    }

    public bool IsPressGameWin(string letter)
    {
        var player = letter == "A" ? PlayerA : PlayerB;
        return player?.Count >= FINISH;
    }

    // Check if both players are ready
    public bool AreBothPlayersReady()
    {
        return PlayerA?.IsReady == true && PlayerB?.IsReady == true;
    }
}