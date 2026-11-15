using MirrorImaged.World;

namespace MirrorImaged.Natives;

// Running the AI scripts of the computer players: the script the game names (common.ai and the script together, as JASS made into Lua in an
// environment of its own, so two players' scripts keep their globals apart) runs in a thread of its own for its player, `Sleep` as its wait,
// the AI natives acting for that player while it runs (decisions D361).
public sealed partial class GameNatives
{
    /// <summary>Makes the script (a name such as "human.ai", from the map or the install) ready to run and gives the global function that starts it; null when there is none.</summary>
    public Func<string, string?>? AiLoader { get; init; }

    /// <summary>Starts the AI script for the player in a thread: it runs until its first Sleep now, and goes on a game timer at a time.</summary>
    void StartAi(player? p, string? script)
    {
        if (p is null || script is null || Threads is null || AiLoader?.Invoke(script) is not { } main) return;
        var outer = AiFor;
        AiFor = p;
        IScriptThread? thread;
        float wait;
        try
        {
            _sleep = 0.1f;
            thread = Threads.Start(main);
            wait = _sleep;
        }
        finally { AiFor = outer; }
        if (thread is not null) ResumeAi(p, thread, wait);
    }

    // The thread goes on after its sleep, for its player, and sleeps again until it ends
    void ResumeAi(player p, IScriptThread thread, float wait) =>
        _game.At(_game.Step + Math.Max(1, Game.Steps(Math.Max(wait, 0.01f))), () =>
        {
            var outer = AiFor;
            AiFor = p;
            bool again;
            float next;
            try
            {
                _sleep = 0.1f;
                again = thread.Resume();
                next = _sleep;
            }
            finally { AiFor = outer; }
            if (again) ResumeAi(p, thread, next);
            else thread.Dispose();
        });

    [Native(Level.Close)]
    public void StartMeleeAI(player? num, string? script) => StartAi(num, script);

    [Native(Level.Close)]
    public void StartCampaignAI(player? num, string? script) => StartAi(num, script);

    /// <summary>The AI thread waits the seconds.</summary>
    [Native(Level.Close)]
    public void Sleep(float seconds)
    {
        _sleep = seconds;
        if (Threads is null) throw new InvalidOperationException("attempt to yield from outside a coroutine");
        Threads.Yield();
    }

    /// <summary>Runs the function in a thread of its own for the AI player that started it, sleeping and going on as the main one does.</summary>
    [Native(Level.Close)]
    public void StartThread(Action? func)
    {
        if (func?.Target is not IScriptFunction function || Threads is null || AiFor is not { } p) return;
        IScriptThread? thread;
        float wait;
        _sleep = 0.1f;
        thread = function.Start();
        wait = _sleep;
        if (thread is not null) ResumeAi(p, thread, wait);
    }
}
