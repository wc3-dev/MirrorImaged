using System.Reflection;
using MirrorImaged.Lua;
using MirrorImaged.Natives;
using MirrorImaged.World;
using War3Net.CodeAnalysis.Jass;
using War3Net.CodeAnalysis.Jass.Syntax;

namespace MirrorImaged;

// Preloader runs a file's JASS as the game does (decisions D102): turned into Lua by War3Net's transpiler, as blizzard.j
// is, and run with common.j's natives and globals only, as they were before the map ran. A file that doesn't parse or
// names anything else (a blizzard.j function or global) closes the game (facts), and so stops the run.
public sealed partial class Simulation
{
    static JassCompilationUnitSyntax? _commonJ;

    LuaFunction? _preloadFile;
    (string Lua, string Path) _preloadNext;

    // The AI script, with common.ai in front of it, as Lua in an environment of its own whose global function main is made a global of the
    // game's under a name for this script; null for a script the map and the install lack
    int _aiScripts;
    bool _aiRules;

    // Where the AI script is read from: the map, else the balance set the map's values come from (Reign of Chaos's sets have race scripts of
    // their own), else the game's own
    internal (string From, byte[] Bytes)? AiScript(string script)
    {
        var file = script.Replace('\\', '/');
        if ((_natives.Data.Map?.File(script) ?? _natives.Data.Map?.File("Scripts" + '\\' + script)) is { } own) return ("map:" + script, own);
        if (Data.StandardData.Sets(_natives.Data.Map?.Files.Info).Values is { } set && _natives.Data.InstallFile($"War3.w3mod:_balance/{set}.w3mod:Scripts/{file}") is { } balanced)
            return ($"_balance/{set}.w3mod:Scripts/{file}", balanced);
        return _natives.Data.InstallFile("War3.w3mod:Scripts/" + file) is { } found ? ("Scripts/" + file, found) : null;
    }

    string? LoadAi(string script)
    {
        var bytes = AiScript(script)?.Bytes;
        var common = _natives.Data.InstallFile("War3.w3mod:Scripts/common.ai");
        if (bytes is null || common is null) return null;
        var jass = System.Text.Encoding.UTF8.GetString(common) + "\n" + System.Text.Encoding.UTF8.GetString(bytes);
        var lua = Jass.ToLua([CommonJ()], jass, rules: true);
        if (!_jass && !_aiRules) { AddJassRules(); _aiRules = true; }
        var name = $"MirrorImaged_Ai{++_aiScripts}";
        if (Environment.GetEnvironmentVariable("MIRROR_DUMP_AI") is { Length: > 0 } dump) File.WriteAllText(Path.Combine(dump, name + ".lua"), lua);
        _lua.Run($"do local _ENV = setmetatable({{}}, {{__index = _G}})\n{lua}\n_G.{name} = main\nend", name);
        return name;
    }

    // common.j's declarations, made once: a game's globals have the same names and types as any other's
    static JassCompilationUnitSyntax CommonJ() => _commonJ ??= JassSyntaxFactory.ParseCompilationUnit(Declarations(new GameNatives(new Game(0))));

    static IEnumerable<MethodInfo> NativeMethods => typeof(GameNatives).GetMethods().Where(method => method.GetCustomAttribute<NativeAttribute>() is not null);

    // common.j's globals and natives as JASS declarations, which a file is transpiled against: War3Net needs a function's
    // declaration to call it inside an expression.
    static string Declarations(GameNatives natives) =>
        string.Join('\n', [
            "globals",
            .. natives.Globals().Select(global => $"constant {JassType(global.Value!.GetType())} {global.Name} = null"),
            "endglobals",
            .. NativeMethods.Select(native =>
            {
                var parameters = native.GetParameters();
                var takes = parameters.Length == 0 ? "nothing" : string.Join(", ", parameters.Select(p => $"{JassType(p.ParameterType)} {p.Name}"));
                return $"native {native.Name} takes {takes} returns {JassType(native.ReturnType)}";
            }),
        ]);

    static string JassType(Type type) =>
        type == typeof(void) ? "nothing"
        : type == typeof(int) ? "integer"
        : type == typeof(float) ? "real"
        : type == typeof(bool) ? "boolean"
        : type == typeof(string) ? "string"
        : typeof(Delegate).IsAssignableFrom(type) ? "code"
        : type.Name;

    // Keeps a Lua function that runs a file's Lua, as the chunk named by its path, with common.j's natives and globals
    // and __jarray, which War3Net makes a JASS array with, taken now, before blizzard.j and the map can change them; any
    // other name it reads is an error.
    void AddPreloader()
    {
        _lua["preloaderNames"] = string.Join(' ', _natives.Globals().Select(global => global.Name).Concat(NativeMethods.Select(native => native.Name)).Append("__jarray"));
        _lua.Register("preloader", args =>
        {
            _preloadFile = ((LuaFunction)args[0]!).Keep();
            return [];
        });
        _lua.Register("preloaderNext", _ => [_preloadNext.Lua, _preloadNext.Path]);
        _lua.Run("""
            local load, error, setmetatable, nextFile, jass = load, error, setmetatable, preloaderNext, {}
            for name in preloaderNames:gmatch('%S+') do jass[name] = _G[name] end
            setmetatable(jass, {__index = function(_, name) error(name .. " is not a native or a global of common.j", 2) end})
            preloader(function()
              local lua, path = nextFile()
              local file = setmetatable({}, {__index = jass})
              local run, problem = load(lua, '@' .. path, 't', file)
              if not run then error(problem, 0) end
              run()
              file.PreloadFiles()
            end)
            preloaderNames, preloader, preloaderNext = nil, nil, nil
            """, "mirror");
    }

    void RunPreloadFile(string path, string jass)
    {
        try
        {
            // A thread is started with no arguments, so the function asks for the file; a wait in it isn't kept (D102).
            _preloadNext = (Blizzard.ToLua(CommonJ(), jass), path);
            _preloadFile!.Start().Dispose();
        }
        catch (Exception e) when (_lua.Halted is null)
        {
            throw new GameClosedException($"The game closes: Preloader ran {path}, which it can't run ({e.Message.Split("\nstack traceback:")[0].ReplaceLineEndings(" ")})");
        }
    }
}
