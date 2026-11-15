using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The responses and registrations of events whose sources come later, and sync data, which does arrive.</summary>
public sealed class LaterEventTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
            {{main}}
            end
            """), "", data: new StandardData());
        simulation.Advance(TimeSpan.FromSeconds(1));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void Responses_outside_their_events_read_nil_and_zero_and_registrations_answer()
    {
        var lines = Run("""
            print(GetSpellAbilityId(), GetSpellAbility(), GetSpellTargetUnit(), GetSpellTargetX(), GetAttacker(), GetTrainedUnitType(), GetSoldItem(), GetWinningPlayer())
            print(CreateTrigger() ~= nil, TriggerRegisterPlayerChatEvent(CreateTrigger(), Player(0), "-x", true) ~= nil, TriggerRegisterPlayerChatEvent(nil, Player(0), "-x", true), GetEventPlayerChatString() == "")
            print(BlzIsKeyPressed(nil), BlzGetMouseScreenPosX(), BlzGetTriggerPlayerKey(), UnitUseItemPoint(nil, nil, 0, 0))
            """);

        Assert.Equal(["0\tnil\tnil\t0.0\tnil\t0\tnil\tnil", "true\ttrue\tnil\ttrue", "false\t0\tnil\tfalse"], lines);
    }

    // sync data goes to the registrations for its prefix, from the local player, a step later; over 255 characters it is refused
    [Fact]
    public void Sync_data_arrives_a_step_later_for_its_prefix()
    {
        var lines = Run("""
            local t = CreateTrigger()
            BlzTriggerRegisterPlayerSyncEvent(t, Player(0), "pfx", false)
            TriggerAddAction(t, function() print("got", BlzGetTriggerSyncPrefix(), BlzGetTriggerSyncData(), GetPlayerId(GetTriggerPlayer())) end)
            print(BlzSendSyncData("pfx", "hello"), BlzSendSyncData("other", "x"), BlzSendSyncData("pfx", string.rep("a", 300)))
            print("sent", BlzGetTriggerSyncData() == "")
            """);

        Assert.Equal(["true\ttrue\tfalse", "sent\ttrue", "got\tpfx\thello\t0"], lines);
    }
}
