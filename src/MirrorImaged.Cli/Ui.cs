using MirrorImaged.Natives;
using Spectre.Console;

// On a terminal the output is styled; piped or redirected it is the plain text that scripts, CI and the tests read
static class Ui
{
    static readonly bool Styled = !Console.IsOutputRedirected;
    static readonly IAnsiConsole? Errors = Console.IsErrorRedirected ? null : AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) });

    static readonly (string Command, string Does)[] Commands =
    [
        ("coverage", "How many natives are at each trust level, and which are Missing"),
        ("check <map>", "Lists the natives a map's script calls with their trust levels"),
        ("run <map> [--seconds <n>] [--seed <n>] [--hash <n>] [--record <file>] [--input <file>]", "Runs a Lua or JASS map for n simulated seconds (60) with a seed (0), then prints its log; with --hash, the state hash every n seconds; with --input, the inputs of a recording (or a list of them) at their steps; with --record, saves the run as a recording"),
        ("replay <recording or .w3g> [--map <map>] [--hash <n>]", "Runs a recording again, exactly, and prints the state hash at its end (or every n seconds); a game replay (.w3g) needs its --map"),
        ("desync <map> [--seconds <n>] [--seed <n>]", "Runs the map for each person's slot as that player's own game and reports where the runs first differ"),
        ("snapshot <map or recording> [--at <n>] [--seed <n>] [--out <file>]", "Saves the world after n simulated seconds with what it takes to run to it again"),
        ("view <map or recording> [--seconds <n>] [--every <n>] [--out <file>]", "Runs it and saves a page that opens in a browser: the map from above, the units, a timeline to scrub and the log"),
        ("compare <snapshot> [--map <map>]", "Runs the snapshot's run again to its step and lists what differs"),
        ("test <file or folder> [--junit <file>]", "Runs the Lua tests (*.test.lua) with the Lua test API, prints each result and writes JUnit XML to the file"),
        ("bench <map> [--seconds <n>] [--seed <n>]", "Times a run: how many times faster than real time it goes"),
        ("fuzz <map> [--seconds <n>] [--seed <n>] [--orders <n>]", "Gives every player's units random legal orders each second and reports script errors"),
    ];

    const string Seating = "Any command that takes a map also takes --users <slots> and --computers <slots> (comma separated), or --melee true to seat the first user slot with a person and the others with computers, and a map named War3.w3mod:Maps/... is read from the installed game.";

    // The fixed text may hold Spectre markup; what is interpolated is never read as markup
    public static void Line(FormattableString line)
    {
        if (Styled) AnsiConsole.MarkupLineInterpolated(line);
        else Console.Write(string.Format(Markup.Remove(line.Format), line.GetArguments()) + "\n");
    }

    public static void Error(string text)
    {
        if (Errors is null) Console.Error.Write(text + "\n");
        else Errors.MarkupLineInterpolated($"[red]{text}[/]");
    }

    // A spinner while the work goes on; nothing else may write to the terminal meanwhile but Line
    public static T Work<T>(string what, Func<T> work) => Styled ? AnsiConsole.Status().Start(Markup.Escape(what), _ => work()) : work();

    public static void Work(string what, Action work) => Work(what, () => { work(); return 0; });

    public static void Coverage(Coverage coverage)
    {
        if (!Styled)
        {
            Console.Write(coverage);
            return;
        }
        var chart = new BarChart().Label("[bold]Natives by trust level[/]").LeftAlignLabel();
        foreach (var level in Enum.GetValues<Level>()) chart.AddItem(level.ToString(), coverage.Counts[level], Colour(level));
        AnsiConsole.Write(chart);
        if (coverage.Missing.Count == 0) AnsiConsole.MarkupLineInterpolated($"\n[bold]{coverage.Counts.Values.Sum()}[/] natives, [green]none Missing[/]");
        else
        {
            AnsiConsole.MarkupLineInterpolated($"\n[bold]{coverage.Counts.Values.Sum()}[/] natives, [red]{coverage.Missing.Count} Missing[/]");
            AnsiConsole.Write(new Columns(coverage.Missing.Select(name => new Text(name))));
        }
    }

    public static void Natives(IReadOnlyList<(string Name, int Count, Level Level)> used)
    {
        if (!Styled)
        {
            foreach (var native in used) Console.Write($"{native.Level,-9}{native.Name} x{native.Count}\n");
            return;
        }
        var table = new Table().RoundedBorder().AddColumn("Level").AddColumn("Native").AddColumn(new TableColumn("Calls").RightAligned());
        foreach (var native in used) table.AddRow($"[{Colour(native.Level).ToMarkup()}]{native.Level}[/]", native.Name, native.Count.ToString());
        AnsiConsole.Write(table);
    }

    public static int Usage()
    {
        if (Errors is null)
        {
            Console.Error.Write("usage:\n");
            foreach (var (command, does) in Commands) Console.Error.Write($"  mirror {command}\n      {does}\n");
            Console.Error.Write($"\n{Seating}\n");
            return 2;
        }
        Errors.Write(new Rule("[bold]mirror[/]").LeftJustified());
        foreach (var (command, does) in Commands)
        {
            var words = command.Split(' ', 2);
            Errors.MarkupLineInterpolated($"[bold aqua]mirror {words[0]}[/] [grey]{words.ElementAtOrDefault(1)}[/]");
            Errors.Write(new Padder(new Text(does), new Padding(4, 0, 0, 1)));
        }
        Errors.Write(new Text(Seating + "\n", new Style(Color.Grey)));
        return 2;
    }

    static Color Colour(Level level) => level switch
    {
        Level.Exact => Color.Green,
        Level.Close => Color.Yellow,
        Level.Recorded => Color.Blue,
        _ => Color.Red,
    };
}
