using System.Reflection;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

/// <summary>A Recorded native's call goes in <see cref="Simulation.RecordedCalls"/>, with its step and arguments, and not in the log (D98, D820).</summary>
public sealed class RecordedCallsTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string main) => Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n"), "");

    static readonly string[] Recorded = [.. typeof(GameNatives).GetMethods()
        .Where(method => method.GetCustomAttribute<NativeAttribute>()?.Level == Level.Recorded)
        .Select(method => method.Name).Order(StringComparer.Ordinal)];

    static string Default(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(int) || parameter.ParameterType == typeof(float) ? "0"
        : parameter.ParameterType == typeof(bool) ? "false"
        : parameter.ParameterType == typeof(string) ? "''"
        : "nil";

    [Fact]
    public void Every_recorded_native_writes_a_record_of_its_call()
    {
        var calls = string.Join('\n', Recorded.Select(name =>
            $"pcall({name}{string.Concat(typeof(GameNatives).GetMethod(name)!.GetParameters().Select(p => ", " + Default(p)))})"));

        using var simulation = Load(calls);

        Assert.NotEmpty(Recorded);
        Assert.Equal(Recorded, simulation.RecordedCalls.Select(call => call.Native));
    }

    [Fact]
    public void A_record_holds_the_step_and_the_arguments_and_the_log_doesnt()
    {
        using var simulation = Load("""
            SetMapName("Arena")
            TimerStart(CreateTimer(), 0.5, false, function() SetEnemyStartLocPrio(1, 2, 3, MAP_LOC_PRIO_HIGH) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Collection(simulation.RecordedCalls,
            call =>
            {
                Assert.Equal((0L, "SetMapName"), (call.Step, call.Native));
                Assert.Equal(["Arena"], call.Arguments);
            },
            call =>
            {
                Assert.Equal((512L, "SetEnemyStartLocPrio"), (call.Step, call.Native));
                Assert.Equal([1, 2, 3], call.Arguments.Take(3));
                Assert.IsType<int>(call.Arguments[3]);
            });
        Assert.Empty(simulation.Log);
    }

    [Fact]
    public void A_handle_argument_is_its_id_as_lua_reads_it()
    {
        using var simulation = Load("""
            SetMapName(GetHandleId(MAP_LOC_PRIO_HIGH))
            SetEnemyStartLocPrio(1, 2, 3, MAP_LOC_PRIO_HIGH)
            """);

        var id = int.Parse((string)simulation.RecordedCalls[0].Arguments[0]!, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(id, simulation.RecordedCalls[1].Arguments[3]);
    }
}
