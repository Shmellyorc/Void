window.VOID_EXAMPLES = {
"standard-conversion-classification": {"title": "Standard conversion classification", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Identity, numeric, enum, reference/interface, null, nullable, and existing unsafe conversions are classified by one semantic conversion vocabulary.", "code": "int whole = 12;\nfloat widened = whole;\n\nPlayer player = new Player();\nEntity entity = player;\n\nint? optional = whole;\nEntity missing = null;"},
"user-defined-conversion-resolution": {"title": "User-defined conversion resolution", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Implicit and explicit conversion operators use one applicability, accessibility, ambiguity, and surrounding-standard-conversion resolver.", "code": "public struct Meters\n{\n    public int Value;\n\n    public static implicit operator Meters(int value)\n    {\n        return new Meters { Value = value };\n    }\n\n    public static explicit operator int(Meters value)\n    {\n        return value.Value;\n    }\n}\n\nMeters distance = 12;\nint raw = (int)distance;"},
"lifted-nullable-conversions": {"title": "Lifted nullable conversions", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "User-defined value conversions lift through nullable values and preserve absence instead of manufacturing a value.", "code": "int? source = 12;\nMeters? converted = source;\n\nsource = null;\nconverted = source; // empty remains empty"},
"conversion-site-integration": {"title": "Target-typed conversion sites", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Assignments, returns, arguments, constructors, arrays, initializers, conditionals, switch expressions, and params all ask the same conversion engine.", "code": "public static Meters Make(int value)\n{\n    return value;\n}\n\nMeters local = 5;\nMeters[] values = { 1, 2, 3 };\nUseMeters(4);\nMeters chosen = ready ? 6 : 7;"},
"static-interface-conversions": {"title": "Static interface conversion contracts", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Interfaces can require signature-only conversion operators and concrete types satisfy those contracts through normal static-interface validation.", "code": "public interface IFromInt<T>\n{\n    static implicit operator T(int value);\n}\n\npublic struct Score : IFromInt<Score>\n{\n    public int Value;\n\n    public static implicit operator Score(int value)\n    {\n        return new Score { Value = value };\n    }\n}"},
"constrained-implicit-conversions": {"title": "Constrained generic implicit conversion", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Generic source can use an implicit conversion only when its interface constraints authorize that conversion before specialization.", "code": "public static T Convert<T>(int value)\n    where T : IFromInt<T>\n{\n    return value;\n}\n\nScore score = Convert<Score>(12);"},
"constrained-explicit-conversions": {"title": "Constrained generic explicit conversion", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Explicit casts follow the same rule: a concrete specialization cannot retroactively legalize an unconstrained generic cast.", "code": "public interface IToInt<T>\n{\n    static explicit operator int(T value);\n}\n\npublic static int Read<T>(T value)\n    where T : IToInt<T>\n{\n    return (int)value;\n}"},
"generic-conversion-provenance": {"title": "Generic conversion provenance", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Generic inference, overload resolution, specialization, and target typing preserve the interface constraint that proved a conversion legal.", "code": "public static T Create<T>(int value)\n    where T : IFromInt<T>\n{\n    T converted = value;\n    return converted;\n}\n\nScore result = Create<Score>(9);"},
"conversion-iterator-closure-gc": {"title": "Conversions through iterator, closure & GC", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Conversion-contract provenance survives iterator fields and closure captures across yield, forced GC, exceptions, finally, and deterministic cleanup.", "code": "public static IEnumerable<T> ConvertAll<T>(int first, int second)\n    where T : IFromInt<T>\n{\n    T saved = first;\n    yield return saved;\n\n    GC.Collect();\n    T next = second;\n    yield return next;\n}"},
"conversion-integration": {"title": "Conversion semantics integration", "category": "Conversion semantics & generic conversions", "language": "VOID", "caption": "Standard/user conversions, nullable lifting, static-interface contracts, constrained generics, operators, closures, iterators, exceptions, cleanup, GC, and library compilation now share one semantic conversion model.", "code": "public static T Build<T>(int? source)\n    where T : IFromInt<T>\n{\n    if (source is int value)\n        return value;\n\n    return default;\n}"},
"iterator-pattern-capture": {"title": "Iterator declaration-pattern capture", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Declaration-pattern locals that remain live across yield are promoted through the normal iterator capture model and resume with their flow state intact.", "code": "public static IEnumerable<string> Names(object value)\n{\n    if (value is Player player)\n    {\n        yield return player.Name;\n        yield return player.Name;\n    }\n}"},
"iterator-switch-capture": {"title": "Iterator switch/guard capture", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Pattern locals introduced by switch cases and guards preserve their exact case scope when they must survive suspension.", "code": "public static IEnumerable<int> Scores(object value)\n{\n    switch (value)\n    {\n        case Player player when player.Score > 0:\n            yield return player.Score;\n            yield return player.Score + 1;\n            break;\n    }\n}"},
"iterator-pattern-gc": {"title": "Iterator pattern lifetime + GC", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Managed pattern captures remain rooted across suspension, forced GC, resume, disposal, exceptions, finally cleanup, and iterator teardown.", "code": "public static IEnumerable<string> Hold(object value)\n{\n    if (value is Player player)\n    {\n        try\n        {\n            yield return player.Name;\n            GC.Collect();\n            yield return player.Name;\n        }\n        finally\n        {\n            player.Close();\n        }\n    }\n}"},
"iterator-state-audit": {"title": "Iterator state-machine integration", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Ordinary locals, pattern captures, foreach, using/disposal, exceptions, generics, and switch forms share one iterator state-machine model.", "code": "public static IEnumerable<int> Read(object value)\n{\n    using var resource = Open();\n    if (value is Player player)\n    {\n        foreach (int score in player.Scores)\n            yield return score;\n    }\n}"},
"stable-diagnostics": {"title": "Stable diagnostics", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Compiler diagnostics describe the actual invalid construct rather than exposing historical internal milestone labels.", "code": "public static int MissingReturn(bool ready)\n{\n    if (ready)\n        return 1;\n\n    // The diagnostic names the missing return path itself.\n}"},
"unary-operators": {"title": "Unary operators", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "User-defined unary operators participate in VOID's normal overload, conversion, generic, and nullable-aware semantic machinery.", "code": "public struct Int2\n{\n    public int X;\n    public int Y;\n\n    public static Int2 operator -(Int2 value)\n    {\n        return new Int2 { X = -value.X, Y = -value.Y };\n    }\n}\n\nInt2 flipped = -value;"},
"binary-operators": {"title": "Binary operators", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "User-defined arithmetic, comparison, equality, and compound paths reuse normal conversion ranking and overload resolution.", "code": "public struct Int2\n{\n    public int X;\n    public int Y;\n\n    public static Int2 operator +(Int2 left, Int2 right)\n    {\n        return new Int2 { X = left.X + right.X, Y = left.Y + right.Y };\n    }\n}\n\nposition += velocity;"},
"static-interface-operators": {"title": "Static interface operator contracts", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Interfaces can express static operator requirements as compile-time contracts validated against implementing concrete types.", "code": "public interface IAdd<T>\n{\n    static T operator +(T left, T right);\n}\n\npublic struct Score : IAdd<Score>\n{\n    public int Value;\n\n    public static Score operator +(Score left, Score right)\n    {\n        return new Score { Value = left.Value + right.Value };\n    }\n}"},
"constrained-generic-operators": {"title": "Constrained generic operator dispatch", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Generic code may invoke an operator required by a static-interface constraint; specialization resolves it to the concrete static operator.", "code": "public static T Add<T>(T left, T right)\n    where T : IAdd<T>\n{\n    return left + right;\n}\n\nScore total = Add(first, second);"},
"operator-integration": {"title": "Iterator + generic operator integration", "category": "Iterator lifetime & generic operators", "language": "VOID", "caption": "Constrained generic operator provenance survives iterator promotion, yield/resume, GC, disposal, exceptions, and normal operator lowering without a parallel dispatch path.", "code": "public interface IAdd<T>\n{\n    static T operator +(T left, T right);\n}\n\npublic static IEnumerable<T> Accumulate<T>(T sum, T other)\n    where T : IAdd<T>\n{\n    yield return sum;\n    sum += other;\n    yield return sum;\n}"},
"declaration-patterns": {"title": "Declaration patterns", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "A successful type/declaration pattern narrows the value and introduces a definitely-assigned local only inside the matched flow.", "code": "object value = FindActor();\n\nif (value is Player player)\n{\n    Console.WriteLine(player.Name);\n}"},
"constant-null-patterns": {"title": "Constant + null patterns", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "Constants and null use the same pattern machinery and can participate anywhere patterns are accepted.", "code": "object value = LoadValue();\n\nif (value is null)\n    Console.WriteLine(\"missing\");\n\nint score = 100;\nif (score is 100)\n    Console.WriteLine(\"perfect\");"},
"relational-patterns": {"title": "Relational patterns", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "Relational patterns reuse VOID's existing comparison/type rules for concise range-style matching.", "code": "int health = GetHealth();\n\nif (health is > 0)\n    Console.WriteLine(\"alive\");\n\nif (health is <= 25)\n    Console.WriteLine(\"critical\");"},
"logical-patterns": {"title": "Logical patterns", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "and, or, and not compose patterns with short-circuiting and flow-aware binding rules.", "code": "int health = GetHealth();\n\nif (health is > 0 and <= 25)\n    Console.WriteLine(\"critical\");\n\nif (health is not 0)\n    Console.WriteLine(\"non-zero\");"},
"property-patterns": {"title": "Property patterns", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "Property patterns match through ordinary getters across classes, structs, interfaces, and inherited members.", "code": "if (actor is Player { Health: > 0 } player)\n{\n    Update(player);\n}"},
"recursive-patterns": {"title": "Recursive patterns", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "Nested property patterns preserve single evaluation and GC-safe temporary receiver lifetimes.", "code": "if (actor is Player\n    {\n        Health: > 0,\n        Inventory: Inventory { Count: > 0 }\n    } player)\n{\n    UseFirstItem(player);\n}"},
"pattern-switch": {"title": "Pattern switch statements", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "Switch statements accept type, constant/null, relational, logical, property, and recursive cases while preserving deterministic first-match behavior.", "code": "switch (actor)\n{\n    case Player { Health: <= 0 } player:\n        Respawn(player);\n        break;\n\n    case Player player:\n        Update(player);\n        break;\n\n    case null:\n        break;\n}"},
"switch-guards": {"title": "Switch guards + ordering", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "when guards refine a matched case after its pattern succeeds; cases remain deterministic and first-match wins.", "code": "switch (actor)\n{\n    case Player player when player.Health <= 0:\n        Respawn(player);\n        break;\n\n    case Player player when player.Health < 25:\n        Heal(player);\n        break;\n}"},
"switch-expressions": {"title": "Switch expressions", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "Expression-form switch combines patterns with target/result type unification, nullable/value handling, and conditional-expression typing rules.", "code": "string state = actor switch\n{\n    Player { Health: <= 0 } => \"down\",\n    Player { Health: > 0 } => \"active\",\n    null => \"missing\",\n    _ => \"other\"\n};"},
"pattern-integration": {"title": "Pattern integration", "category": "Pattern matching & control-flow expressions", "language": "VOID", "caption": "Declaration/property/recursive patterns, guarded switches, switch expressions, generics, interfaces, exceptions, iterators, GC, and nullable/value semantics compose through one flow-sensitive system.", "code": "object value = FindActor();\n\nstring state = value switch\n{\n    Player { Health: <= 0 } => \"down\",\n    Player { Health: > 0 } player => player.Name,\n    null => \"missing\",\n    _ => \"other\"\n};\n\nswitch (value)\n{\n    case Player player when player.Health > 0:\n        Update(player);\n        break;\n}"},
"idisposable-iterator": {"title": "IDisposable + iterator disposal", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Disposable enumerators and compiler-generated iterators participate in deterministic cleanup; disposing a suspended iterator runs pending cleanup exactly once.", "code": "public sealed class Resource : IDisposable\n{\n    public void Dispose()\n    {\n        Console.WriteLine(\"disposed\");\n    }\n}\n\npublic static IEnumerable<int> Values()\n{\n    try\n    {\n        yield return 1;\n        yield return 2;\n    }\n    finally\n    {\n        Console.WriteLine(\"iterator cleanup\");\n    }\n}"},
"foreach-disposal": {"title": "foreach disposal", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Non-array foreach guarantees enumerator disposal even when enumeration ends early through break, return, or an exception.", "code": "foreach (int value in Values())\n{\n    Console.WriteLine(value);\n    break;\n}\n\n// The generated enumerator is disposed here,\n// so the iterator's pending finally cleanup runs."},
"using-statement": {"title": "using statement", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "using lowers through ordinary structured cleanup so disposable reference/value resources are cleaned up on normal and exceptional exits.", "code": "using (Resource resource = new Resource())\n{\n    resource.Run();\n}\n\n// Dispose() has completed here."},
"using-declaration": {"title": "using declaration", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "using declarations keep a resource alive to scope exit and dispose multiple resources in reverse declaration order.", "code": "public static void Run()\n{\n    using var first = new Resource(\"first\");\n    using var second = new Resource(\"second\");\n\n    Play();\n}\n\n// second.Dispose() then first.Dispose()"},
"generic-inference": {"title": "Generic method inference", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Generic method type arguments can be inferred from normal call arguments while continuing to use the existing overload resolver and monomorphizer.", "code": "public static T Identity<T>(T value)\n{\n    return value;\n}\n\nint number = Identity(42);\nstring name = Identity(\"VOID\");\n\nConsole.WriteLine(number == 42);\nConsole.WriteLine(name == \"VOID\");"},
"extension-methods": {"title": "Extension methods", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Static methods with a this first parameter participate in instance-style calls while ordinary instance members retain precedence.", "code": "public static class NumberExtensions\n{\n    public static int Twice(this int value)\n    {\n        return value * 2;\n    }\n}\n\nint value = 21;\nConsole.WriteLine(value.Twice() == 42);"},
"static-interface-methods": {"title": "Static interface methods", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Static interface methods are compile-time contracts satisfied by ordinary public static methods on concrete types.", "code": "public interface IFactory\n{\n    static Widget Create();\n}\n\npublic sealed class WidgetFactory : IFactory\n{\n    public static Widget Create()\n    {\n        return new Widget();\n    }\n}"},
"constrained-static-dispatch": {"title": "Constrained static dispatch", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Constrained generic code can call a required static interface member through T; specialization lowers it to the concrete static call.", "code": "public interface IFactory<T>\n{\n    static T Create();\n}\n\npublic static T Build<T, TFactory>()\n    where TFactory : IFactory<T>\n{\n    return TFactory.Create();\n}"},
"default-interface-methods": {"title": "Default interface methods", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Interface methods may provide fallback bodies. Concrete implementations take precedence; ambiguous inherited defaults are diagnosed.", "code": "public interface ILogger\n{\n    void Log(string message)\n    {\n        Console.WriteLine(message);\n    }\n}\n\npublic sealed class GameLogger : ILogger\n{\n}\n\nILogger logger = new GameLogger();\nlogger.Log(\"ready\");"},
"lifetime-abstractions-integration": {"title": "Lifetime + abstractions integration", "category": "Deterministic lifetime & advanced abstractions", "language": "VOID", "caption": "Disposal, using, inferred generic calls, extension methods, constrained static calls, default interface dispatch, exceptions, and GC compose through the existing compiler/runtime systems.", "code": "using var resource = Factory.CreateResource();\n\nint value = Identity(21).Twice();\nILogger logger = new GameLogger();\n\ntry\n{\n    logger.Log(\"running\");\n    Console.WriteLine(value == 42);\n}\nfinally\n{\n    // using cleanup still runs at scope exit.\n}"},
"exception-foundation": {"title": "Exception + throw", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Exception is an ordinary managed base class with message state, inheritance, runtime type identity, GC tracing, and validated throw operands.", "code": "public sealed class GameException : Exception\n{\n    public int Code;\n\n    public GameException(string message, int code) : base(message)\n    {\n        Code = code;\n    }\n}\n\nGameException error = new GameException(\"bad state\", 17);\nthrow error;"},
"try-catch": {"title": "Typed + catch-all handlers", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Typed handlers select derived exceptions before base handlers, while catch-all handling can absorb any remaining active exception.", "code": "try\n{\n    RunGame();\n}\ncatch (GameException error)\n{\n    Console.WriteLine(error.Message);\n}\ncatch (Exception error)\n{\n    Console.WriteLine(error.Message);\n}\ncatch\n{\n    Console.WriteLine(\"unknown failure\");\n}"},
"exception-propagation": {"title": "Propagation + GC unwinding", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Exceptions can cross ordinary call frames while unwinding restores managed-root checkpoints and keeps the active exception reachable.", "code": "public static void LoadPlayer()\n{\n    Player player = new Player(\"Ada\");\n    GC.Collect();\n\n    Validate(player);\n}\n\npublic static void Validate(Player player)\n{\n    throw new Exception(player.Name);\n}\n\ntry\n{\n    LoadPlayer();\n}\ncatch (Exception error)\n{\n    Console.WriteLine(error.Message);\n}"},
"finally": {"title": "Finally cleanup", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "finally executes on both normal completion and exceptional completion through the same structured cleanup path.", "code": "try\n{\n    SaveGame();\n}\nfinally\n{\n    FlushPendingWrites();\n}"},
"structured-finally": {"title": "Structured exits through finally", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "return, break, and continue cross pending finally regions without skipping required cleanup.", "code": "public static int FindScore(int[] scores)\n{\n    try\n    {\n        foreach (int score in scores)\n        {\n            if (score > 100)\n                return score;\n        }\n\n        return -1;\n    }\n    finally\n    {\n        Console.WriteLine(\"search finished\");\n    }\n}"},
"nested-rethrow": {"title": "Nested handlers + rethrow", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Nested handlers compose normally and bare throw rethrows the active exception without replacing its identity.", "code": "try\n{\n    try\n    {\n        RunStep();\n    }\n    catch (GameException)\n    {\n        LogFailure();\n        throw;\n    }\n}\ncatch (Exception error)\n{\n    Console.WriteLine(error.Message);\n}"},
"iterator-exceptions": {"title": "Iterator exceptions", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Exception regions and finally cleanup remain valid across compiler-generated iterator suspension and resume.", "code": "public static IEnumerable<int> Values()\n{\n    try\n    {\n        yield return 1;\n        throw new Exception(\"iterator failed\");\n    }\n    finally\n    {\n        Console.WriteLine(\"iterator cleanup\");\n    }\n}"},
"delegate-event-exceptions": {"title": "Delegate + event exceptions", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Delegate, multicast-delegate, and event invocation use the ordinary propagation/unwinding model while managed targets remain rooted.", "code": "public static void Fail(int value)\n{\n    throw new Exception(\"handler failed\");\n}\n\nAction<int> handler = Fail;\n\ntry\n{\n    handler(10);\n}\ncatch (Exception error)\n{\n    Console.WriteLine(error.Message);\n}"},
"native-library-exceptions": {"title": "Native + library boundaries", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Native and exported library entry points use defined exception-boundary rules instead of allowing uncontrolled raw unwinding through C ABI frames.", "code": "public static class GameApi\n{\n    [Export(\"void_run_game\")]\n    public static int Run()\n    {\n        try\n        {\n            RunManagedGame();\n            return 0;\n        }\n        catch (Exception error)\n        {\n            Console.WriteLine(error.Message);\n            return -1;\n        }\n    }\n}"},
"exception-integration": {"title": "Exception integration", "category": "Exceptions & runtime control flow", "language": "VOID", "caption": "Structured handlers, propagation, finally, rethrow, iterator cleanup, delegate calls, native/library boundaries, GC, and static-initializer recovery share one runtime-control-flow model.", "code": "public static int Run(bool fail)\n{\n    try\n    {\n        if (fail)\n            throw new Exception(\"failed\");\n\n        return 42;\n    }\n    catch (Exception error)\n    {\n        Console.WriteLine(error.Message);\n        throw;\n    }\n    finally\n    {\n        Console.WriteLine(\"cleanup\");\n    }\n}"},
"unsafe-delegates": {"title": "Unsafe delegates", "category": "Language surface completion II", "language": "VOID", "caption": "Unsafe delegates reuse the normal delegate representation while allowing pointer returns/parameters, nested pointers, void*, and by-reference pointer forms inside existing unsafe boundaries.", "code": "public unsafe delegate int* PointerMap(int* value);\npublic unsafe delegate void PointerByRef(\n    ref int* current,\n    out int* previous,\n    in int* next);\n\npublic static unsafe int* Identity(int* value)\n{\n    return value;\n}"},
"unsafe-pointer-properties": {"title": "Unsafe pointer properties", "category": "Language surface completion II", "language": "VOID", "caption": "Unsafe classes, interfaces, and value structs can expose pointer-bearing properties through normal static, virtual, override, and interface property machinery.", "code": "public unsafe interface IPointerView\n{\n    public int* Pointer { get; }\n}\n\npublic unsafe class PointerBox : IPointerView\n{\n    public int* Pointer { get; set; } = null;\n    public int* Next => Pointer + 1;\n}"},
"do-while": {"title": "do / while", "category": "Language surface completion II", "language": "VOID", "caption": "Post-test loops support guaranteed first execution, break/continue, nesting, switch interaction, generics, lambdas, iterators, and managed locals.", "code": "int value = 0;\n\ndo\n{\n    value++;\n    if (value < 3)\n        continue;\n}\nwhile (value < 5);\n\nConsole.WriteLine(value == 5);"},
"expression-bodied-members": {"title": "Expression-bodied members", "category": "Language surface completion II", "language": "VOID", "caption": "Methods, constructors, operators/conversions, void members, and property setters can use expression bodies while reusing normal semantic and code-generation paths.", "code": "public sealed class Counter\n{\n    private int _value;\n\n    public int Value\n    {\n        get => _value;\n        set => _value = value;\n    }\n\n    public Counter(int value) => _value = value;\n    public int Read() => _value;\n    public void Reset() => _value = 0;\n}"},
"custom-event-accessors": {"title": "Custom event accessors", "category": "Language surface completion II", "language": "VOID", "caption": "Events can provide explicit add/remove bodies, including expression-bodied accessors, while subscription still flows through the existing event model.", "code": "public sealed class Hub\n{\n    private Action<int> _changed;\n\n    public event Action<int> Changed\n    {\n        add => _changed += value;\n        remove => _changed -= value;\n    }\n}"},
"null-conditional-arrays": {"title": "Null-conditional arrays", "category": "Language surface completion II", "language": "VOID", "caption": "?[] short-circuits indexing across one-dimensional, jagged, and rectangular arrays, lifts value results, and skips index expressions when the receiver is null.", "code": "int[][] rows = new int[2][];\nrows[0] = new int[] { 10, 20 };\nrows[1] = null;\n\nint? found = rows?[0]?[1];\nint? missing = rows?[1]?[0];\n\nConsole.WriteLine(found.Value == 20);\nConsole.WriteLine(!missing.HasValue);"},
"sized-rectangular-initializers": {"title": "Sized rectangular initializers", "category": "Language surface completion II", "language": "VOID", "caption": "Explicit compile-time dimensions can be paired with nested rectangular initializers when the declared shape matches exactly.", "code": "int[,] grid = new int[2, 3]\n{\n    { 1, 2, 3 },\n    { 4, 5, 6 }\n};\n\nConsole.WriteLine(grid.GetLength(0) == 2);\nConsole.WriteLine(grid[1, 2] == 6);"},
"implicit-rectangular-initializers": {"title": "Implicit rectangular initializers", "category": "Language surface completion II", "language": "VOID", "caption": "Rectangular-array rank and dimensions can be inferred directly from nested new[,] initializer shape.", "code": "int[,] grid = new[,]\n{\n    { 1, 2, 3 },\n    { 4, 5, 6 }\n};\n\nConsole.WriteLine(grid.Rank == 2);\nConsole.WriteLine(grid.GetLength(1) == 3);"},
"generic-constraints": {"title": "Generic constraints", "category": "Language surface completion II", "language": "VOID", "caption": "where constraints cover reference/value-type requirements, base/interface contracts, multiple interfaces, and new() construction constraints.", "code": "public static T Create<T>()\n    where T : class, new()\n{\n    return new T();\n}\n\npublic static T Keep<T>(T value)\n    where T : IActor\n{\n    return value;\n}"},
"surface-completion-integration": {"title": "Surface-completion integration", "category": "Language surface completion II", "language": "VOID", "caption": "The completed surface block composes custom events, expression-bodied members, post-test loops, null-conditional arrays, inferred rectangular initialization, generic constraints, and the existing runtime/toolchain paths.", "code": "public sealed class Box\n{\n    private Action<int> _changed;\n    public int Value { get; set; }\n\n    public event Action<int> Changed\n    {\n        add => _changed += value;\n        remove => _changed -= value;\n    }\n\n    public int Read() => Value;\n}\n\npublic static T Create<T>() where T : class, new()\n{\n    return new T();\n}\n\nBox[,] boxes = new[,] { { Create<Box>(), Create<Box>() } };\nint i = 0;\ndo { boxes[0, i].Value = ++i; } while (i < 2);\nint? first = boxes?[0, 0]?.Read();"},
"static-interface-properties": {"title": "Static interface properties", "category": "Language edge & project completion", "language": "VOID", "caption": "Interfaces can require public static properties. Implementing classes or structs provide the storage, and normal access stays on the implementing type.", "code": "public interface ISettings\n{\n    static int Count { get; set; }\n    static string Name { get; }\n}\n\npublic sealed class Settings : ISettings\n{\n    public static int Count { get; set; }\n\n    public static string Name\n    {\n        get { return \"game\"; }\n    }\n}\n\nSettings.Count = 4;\nConsole.WriteLine(Settings.Name == \"game\");"},
"static-interface-events": {"title": "Static interface events", "category": "Language edge & project completion", "language": "VOID", "caption": "Static interface events are contracts, not interface-owned storage. Classes and value structs can satisfy them with normal public static events.", "code": "public interface IEvents\n{\n    static event Action<int> Changed;\n}\n\npublic struct Events : IEvents\n{\n    public static event Action<int> Changed;\n\n    public static void Raise(int value)\n    {\n        Changed(value);\n    }\n}\n\nAction<int> handler = value => Console.WriteLine(value == 7);\nEvents.Changed += handler;\nEvents.Raise(7);\nEvents.Changed -= handler;"},
"constructor-return": {"title": "Constructor return", "category": "Language edge & project completion", "language": "VOID", "caption": "A plain return can exit an instance constructor after normal initializer/chaining work has already run; constructors still cannot return a value.", "code": "public sealed class Actor\n{\n    public int Health = 100;\n\n    public Actor(bool disabled)\n    {\n        if (disabled)\n            return;\n\n        Health = 150;\n    }\n}\n\nActor actor = new Actor(true);\nConsole.WriteLine(actor.Health == 100);"},
"rectangular-array-initializers": {"title": "Rectangular array initializers", "category": "Language edge & project completion", "language": "VOID", "caption": "Typed rectangular arrays can infer dimensions from nested initializer braces while preserving conversions, row-major ordering, GC safety, and shape diagnostics.", "code": "int[,] grid = new int[,]\n{\n    { 1, 2, 3 },\n    { 4, 5, 6 }\n};\n\nConsole.WriteLine(grid.Rank == 2);\nConsole.WriteLine(grid.GetLength(0) == 2);\nConsole.WriteLine(grid.GetLength(1) == 3);\nConsole.WriteLine(grid[1, 2] == 6);"},
"iterator-locals": {"title": "Iterator locals", "category": "Language edge & project completion", "language": "VOID", "caption": "Iterator methods use normal local inference, definite assignment, lexical shadowing, and managed tracing across yield suspension.", "code": "public static IEnumerable<int> Values()\n{\n    var total = 3;\n    int next;\n\n    if (total > 0)\n        next = 4;\n    else\n        next = 0;\n\n    yield return total;\n\n    {\n        int total = next;\n        yield return total;\n    }\n}"},
"receiver-expressions": {"title": "Receiver expressions", "category": "Language edge & project completion", "language": "VOID", "caption": "Instance methods work through temporaries, properties, indexers, arrays, conditionals, and constructor results with single evaluation and GC-safe receiver storage.", "code": "public struct Counter\n{\n    public int Value;\n\n    public Counter(int value)\n    {\n        Value = value;\n    }\n\n    public int Add(int amount)\n    {\n        Value += amount;\n        return Value;\n    }\n}\n\nConsole.WriteLine(new Counter(3).Add(4) == 7);"},
"explicit-generic-calls": {"title": "Explicit generic method calls", "category": "Language edge & project completion", "language": "VOID", "caption": "Explicit generic calls work through static, instance, inherited, virtual, interface, specialized-generic, property, indexer, and temporary receiver paths.", "code": "public static class Utility\n{\n    public static T Echo<T>(T value)\n    {\n        return value;\n    }\n}\n\npublic sealed class Box<T>\n{\n    public U Read<U>(U value)\n    {\n        return value;\n    }\n}\n\nint value = Utility.Echo<int>(42);\nstring name = new Box<int>().Read<string>(\"player\");\n\nConsole.WriteLine(value == 42);\nConsole.WriteLine(name == \"player\");"},
"native-callback-byref": {"title": "By-reference native callbacks", "category": "Language edge & project completion", "language": "VOID", "caption": "Native callback delegates reuse VOID's existing by-reference ABI for ref, out, and in primitive/native-struct parameters.", "code": "public delegate void Adjust(ref int value);\npublic delegate void Produce(out int value);\npublic delegate int Read(in int value);\n\npublic static void AddTwo(ref int value)\n{\n    value += 2;\n}\n\npublic static void MakeValue(out int value)\n{\n    value = 9;\n}"},
"project-library-output": {"title": "Project library output", "category": "Language edge & project completion", "language": "VOID", "caption": "Library projects emit native static archives. Exported public static methods become stable C entry points while ordinary reachability keeps their private implementation graph.", "code": "public static class MathApi\n{\n    [Export(\"void_math_add\")]\n    public static int Add(int left, int right)\n    {\n        return left + right;\n    }\n}\n\n// build output:\n// bin/libMyLibrary.a\n// publish/libMyLibrary.a"},
"language-edge-integration": {"title": "Language-edge integration", "category": "Language edge & project completion", "language": "VOID", "caption": "The completed edge/project block composes static interface contracts, constructor flow, rectangular initialization, iterator locals, receiver/generic calls, callback ABI, and reusable library output in one library/consumer workflow.", "code": "public interface IState\n{\n    static int Count { get; }\n}\n\npublic sealed class State : IState\n{\n    public static int Count\n    {\n        get { return 2; }\n    }\n\n    public State(bool skip)\n    {\n        if (skip)\n            return;\n    }\n}\n\npublic static class Utility\n{\n    public static T Echo<T>(T value)\n    {\n        return value;\n    }\n}\n\n[Export(\"void_state_check\")]\npublic static int Check()\n{\n    State state = new State(true);\n    int[,] values = new int[,] { { 1, 2 }, { 3, 4 } };\n    return Utility.Echo<int>(values[1, 1] + State.Count);\n}"},
"nested-value-structs": {
    "title": "Nested value structs",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Value structs can contain other value structs normally, including managed references, copies, defaults, arrays, generics, nullable wrappers, and GC tracing.",
    "code": "public struct Leaf\n{\n    public int Value;\n    public Node Owner;\n}\n\npublic struct Frame\n{\n    public Leaf Leaf;\n    public int Weight;\n}\n\nFrame value = new Frame();\nvalue.Leaf.Value = 10;\n\nFrame copy = value;\ncopy.Leaf.Value = 99;\n\nConsole.WriteLine(value.Leaf.Value == 10);\nConsole.WriteLine(copy.Leaf.Value == 99);"
  },
  "readonly-structs": {
    "title": "Readonly structs",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Readonly value types and readonly instance methods/accessors preserve value semantics and can be called through in parameters.",
    "code": "public readonly struct Coord\n{\n    public readonly int X;\n    public readonly int Y;\n\n    public Coord(int x, int y)\n    {\n        X = x;\n        Y = y;\n    }\n\n    public int Sum()\n    {\n        return X + Y;\n    }\n}\n\npublic static int Read(in Coord value)\n{\n    return value.Sum();\n}"
  },
  "jagged-arrays": {
    "title": "Jagged arrays",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Recursive array-of-array types support variable inner lengths, nested indexing, null inner arrays, foreach, generics, and managed/value elements.",
    "code": "int[][] rows = new int[][]\n{\n    new int[] { 1, 2 },\n    new int[] { 3, 4, 5 }\n};\n\nConsole.WriteLine(rows.Length == 2);\nConsole.WriteLine(rows[0].Length == 2);\nConsole.WriteLine(rows[1][2] == 5);\n\nint[][] sized = new int[2][];\nsized[0] = new int[] { 10, 11 };\nsized[1] = null;"
  },
  "rectangular-arrays": {
    "title": "Rectangular arrays",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Real multidimensional arrays expose rank, total length, per-dimension lengths, indexing, iteration, bounds checks, and GC-aware elements.",
    "code": "int[,] grid = new int[2, 3];\ngrid[0, 0] = 1;\ngrid[1, 2] = 6;\n\nConsole.WriteLine(grid.Rank == 2);\nConsole.WriteLine(grid.Length == 6);\nConsole.WriteLine(grid.GetLength(0) == 2);\nConsole.WriteLine(grid.GetLength(1) == 3);\nConsole.WriteLine(grid[1, 2] == 6);\n\nint[,,] cube = new int[2, 2, 2];\ncube[1, 0, 1] = 9;"
  },
  "closure-frames": {
    "title": "Nested + foreach closures",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Nested lambdas can capture outer state, while foreach captures get a fresh frame for every iteration so escaped closures do not alias one loop variable.",
    "code": "List<Func<int>> getters = new();\n\nforeach (int value in values)\n{\n    getters.Add(() => value);\n}\n\nFunc<Func<int>> BuildNested(int score)\n{\n    return () => () => score;\n}\n\nConsole.WriteLine(getters[0]() == values[0]);"
  },
  "byref-closures": {
    "title": "By-reference closures",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Closures can retain ref/out/in parameters, by-reference delegate lambdas work normally, and struct instance methods can capture struct this.",
    "code": "public static Func<int> CaptureRef(ref int value)\n{\n    return () => value;\n}\n\nint score = 20;\nFunc<int> read = CaptureRef(ref score);\n\nConsole.WriteLine(read() == 20);\nscore = 25;\nConsole.WriteLine(read() == 25);\n\nRefAction adjust = value => value += 2;\nadjust(ref score);"
  },
  "iterator-completion": {
    "title": "Iterator completion",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Iterator state machines preserve foreach, switch, nested loops, break/continue, base access, managed references, and suspension across yield points.",
    "code": "public static IEnumerator<int> Values(int[] source)\n{\n    foreach (int value in source)\n    {\n        switch (value)\n        {\n            case 0:\n                continue;\n            case 1:\n                yield return 10;\n                break;\n            default:\n                yield return value;\n                break;\n        }\n    }\n}"
  },
  "unsafe-pointer-members": {
    "title": "Unsafe pointer members",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Unsafe types can expose pointer properties and constructors, including ref/out/in pointer parameters, while safe project/type boundaries remain enforced.",
    "code": "public unsafe struct PointerBox\n{\n    private int* _pointer;\n\n    public int* Pointer\n    {\n        get { return _pointer; }\n        set { _pointer = value; }\n    }\n\n    public PointerBox(int* pointer)\n    {\n        _pointer = pointer;\n    }\n\n    public int Value => *_pointer;\n}"
  },
  "compound-targets": {
    "title": "Compound targets",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Compound assignment works through indexers and writable vector swizzles while evaluating receivers, indices, and right-hand sides only once.",
    "code": "box[index] += 5;\n\n[Swizzle(\"IntVector\")]\npublic struct Int4\n{\n    public int X;\n    public int Y;\n    public int Z;\n    public int W;\n}\n\nholder.Value.XY += new Int2(10, 20);"
  },
  "value-control-integration": {
    "title": "Value/control-flow integration",
    "category": "Value types, closures & control flow",
    "language": "VOID",
    "caption": "Nested/readonly value types, jagged and rectangular arrays, closures, iterators, unsafe pointer members, compound targets, and forced GC compose in one project.",
    "code": "Frame[][] rows = BuildRows();\nList<Func<int>> closures = BuildClosures(rows);\n\nFrame[,] grid = new Frame[1, 2];\ngrid[0, 0] = rows[0][0];\ngrid[0, 1] = rows[0][1];\n\nIEnumerator<int> values = IterateGrid(grid);\nGetBox()[NextIndex()] += 5;\n\nFunc<int> capture = rows[0][0].Payload.Capture();\nGC.Collect();\n\nConsole.WriteLine(closures[0]() > 0);\nConsole.WriteLine(values.MoveNext());\nConsole.WriteLine(capture() > 0);"
  },
  "base-constructor-chaining": {
    "title": "Base constructor chaining",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Derived constructors can explicitly select a base constructor while normal field/property initialization ordering is preserved.",
    "code": "public class Entity\n{\n    public int Id { get; set; }\n\n    public Entity(int id)\n    {\n        Id = id;\n    }\n}\n\npublic sealed class Actor : Entity\n{\n    public Actor(int id) : base(id)\n    {\n    }\n}\n\nActor actor = new Actor(42);\nConsole.WriteLine(actor.Id == 42);"
  },
  "constructor-delegation": {
    "title": "Constructor delegation",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Constructors can delegate to another constructor on the same type with : this(...) and then continue through the selected base path.",
    "code": "public sealed class Actor : Entity\n{\n    public int Bonus { get; set; }\n\n    public Actor() : this(10, 2)\n    {\n    }\n\n    public Actor(int id, int bonus) : base(id)\n    {\n        Bonus = bonus;\n    }\n}"
  },
  "interface-inheritance": {
    "title": "Interface inheritance",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Interfaces can inherit other interfaces and expose the combined contract through normal assignment and dispatch.",
    "code": "public interface IScore\n{\n    int Score { get; }\n}\n\npublic interface IActor : IScore\n{\n    int Apply(int amount);\n}\n\npublic interface IAdvancedActor : IActor\n{\n    int Bonus { get; }\n}\n\nIAdvancedActor advanced = actor;\nIActor inherited = advanced;\nIScore scored = inherited;"
  },
  "struct-interfaces": {
    "title": "Struct interface implementation",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Value types can implement interfaces, box through interface references, and remain valid through normal managed-GC paths.",
    "code": "public interface IScore\n{\n    int Score { get; }\n}\n\npublic struct ScorePacket : IScore\n{\n    public int Score { get; set; }\n\n    public ScorePacket(int score)\n    {\n        Score = score;\n    }\n}\n\nScorePacket packet = new ScorePacket(40);\nIScore score = packet;\nGC.Collect();\nConsole.WriteLine(score.Score == 40);"
  },
  "virtual-properties": {
    "title": "Virtual properties",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Virtual, abstract, and override property accessors dispatch through base-class references like normal virtual members.",
    "code": "public abstract class Entity\n{\n    public abstract int Score { get; }\n}\n\npublic sealed class Actor : Entity\n{\n    private int _score;\n    public override int Score\n    {\n        get { return _score; }\n    }\n\n    public Actor(int score)\n    {\n        _score = score;\n    }\n}\n\nEntity entity = new Actor(25);\nConsole.WriteLine(entity.Score == 25);"
  },
  "static-events": {
    "title": "Static events",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Static events own static delegate storage, participate in type initialization, support add/remove, and keep subscribed targets rooted.",
    "code": "public static class GameEvents\n{\n    public static event Action Started = OnStarted;\n\n    private static void OnStarted()\n    {\n        Console.WriteLine(\"started\");\n    }\n\n    public static void Raise()\n    {\n        Started();\n    }\n}\n\nGameEvents.Started += listener.Handle;\nGameEvents.Raise();\nGameEvents.Started -= listener.Handle;"
  },
  "interface-events": {
    "title": "Interface events + initializers",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Interfaces can declare event contracts while concrete instance events can use declaration-time initializers and normal subscription/removal.",
    "code": "public interface IActor\n{\n    event Action Changed;\n}\n\npublic sealed class Actor : IActor\n{\n    public event Action Changed = OnInitialChanged;\n\n    private static void OnInitialChanged()\n    {\n    }\n}\n\nIActor actor = new Actor();\nactor.Changed += listener.Handle;\nactor.Changed -= listener.Handle;"
  },
  "delegate-completion": {
    "title": "Delegate completion",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Delegates support by-reference parameters, structural equality, multicast equality, and struct instance-method targets with captured value-copy semantics.",
    "code": "public delegate void RefAdjust(ref int value);\n\npublic static void Double(ref int value)\n{\n    value *= 2;\n}\n\nint value = 10;\nRefAdjust adjust = Double;\nadjust(ref value);\nConsole.WriteLine(value == 20);\n\nRefAdjust same = Double;\nConsole.WriteLine(adjust == same);"
  },
  "conditional-expression": {
    "title": "Conditional expression",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "The ?: operator is target-typed and composes with interface/reference values, structs, delegates, nullable values, conversions, and generics.",
    "code": "ScorePacket packet = new ScorePacket(40);\nIScore selected = useActor ? actor : packet;\n\nRefAdjust adjust = actor.Bonus > 0\n    ? Increment\n    : Double;\n\nint? score = selected != null\n    ? selected.Score\n    : null;"
  },
  "object-model-integration": {
    "title": "Object-model integration",
    "category": "Object model & expressions",
    "language": "VOID",
    "caption": "Constructor chaining, interface inheritance, struct interfaces, virtual properties, events, delegates, and conditional expressions compose in one normal multi-file program.",
    "code": "Actor actor = new Actor();\n\nEntity entity = actor;\nIAdvancedActor advanced = actor;\nIActor inherited = advanced;\n\ninherited.Changed += listener.Handle;\nint value = inherited.Apply(5);\n\nScorePacket packet = new ScorePacket(40);\nIScore selected = false ? actor : packet;\n\nRefAdjust adjust = actor.Bonus > 0\n    ? Increment\n    : Double;\nadjust(ref value);"
  },
  "completion-integration": {
    "title": "Game-style registry integration",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "A game-style static registry combines nullable values, named arguments, target-typed defaults, instance/object/collection initializers, static initialization, and managed GC roots.",
    "code": "public static class Registry\n{\n    public static readonly int Seed = 10;\n\n    public static Node Primary { get; set; } = Factory.Create(\n        name: \"Alpha\",\n        score: 5,\n        id: Seed);\n\n    public static List<Node> Nodes { get; set; } = new()\n    {\n        Primary,\n        new Node(name: \"Beta\", id: 20)\n        {\n            Score = default\n        }\n    };\n\n    public static int? LastScore = default;\n\n    static Registry()\n    {\n        LastScore ??= Nodes?[0]?.Score;\n        GC.Collect();\n    }\n}"
  },
  "nullable-values": {
    "title": "Nullable values",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Value types can use T? storage with null, HasValue, Value, fields, properties, arrays, generics, enums, and structs.",
    "code": "public sealed class Player\n{\n    public int? Score { get; set; } = default;\n}\n\nint? score = 42;\nConsole.WriteLine(score.HasValue);\nConsole.WriteLine(score.Value == 42);\n\nscore = null;\nConsole.WriteLine(!score.HasValue);\n\nint?[] values = new int?[] { 5, null, 11 };\nConsole.WriteLine(values[0].Value == 5);"
  },
  "nullable-operators": {
    "title": "Nullable operators",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Nullable values participate in coalescing, lifted arithmetic, lifted comparisons, and value-returning null conditionals.",
    "code": "int? score = null;\nscore ??= 10;\n\nint? boosted = score + 2;\nbool? small = boosted < 20;\n\nNode[] nodes = new Node[]\n{\n    new Node { Score = 5 }\n};\n\nint? first = nodes?[0]?.Score;\nConsole.WriteLine(first.Value == 5);\nConsole.WriteLine(small.Value);"
  },
  "null-conditional-indexing": {
    "title": "Null-conditional indexing",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "The ?[] operator short-circuits indexing when the receiver is null and composes with ?. and nullable value results.",
    "code": "Node[] nodes = null;\nint? missing = nodes?[0]?.Score;\nConsole.WriteLine(!missing.HasValue);\n\nnodes = new Node[]\n{\n    new Node { Score = 12 }\n};\n\nint? score = nodes?[0]?.Score;\nConsole.WriteLine(score.HasValue);\nConsole.WriteLine(score.Value == 12);"
  },
  "named-arguments": {
    "title": "Named arguments",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Arguments can bind by parameter name, reorder calls, and mix with optional/default parameters, constructors, generics, delegates, and params.",
    "code": "public static int Mix(int a, int b = 2, int c = 3)\n{\n    return a * 100 + b * 10 + c;\n}\n\nConsole.WriteLine(Mix(c: 9, a: 1));\n\nNode node = Factory.Create(\n    name: \"Alpha\",\n    score: 5,\n    id: 10);\n\nConsole.WriteLine(Registry.Resolve(\n    fallback: -5,\n    index: 2));"
  },
  "target-typed-default": {
    "title": "Target-typed default",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "A bare default resolves from the destination type in locals, returns, arguments, fields, properties, generics, pointers, enums, structs, and nullable values.",
    "code": "public static T Zero<T>()\n{\n    return default;\n}\n\nint count = default;\nNode node = default;\nGameMode mode = default;\nint? score = default;\n\nConsole.WriteLine(count == 0);\nConsole.WriteLine(node == null);\nConsole.WriteLine(!score.HasValue);"
  },
  "instance-initializers": {
    "title": "Instance initializers",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Instance fields and automatic properties can initialize before constructor body execution while managed references remain GC-safe.",
    "code": "public sealed class Node\n{\n    public int Id = 1;\n    public string Name { get; set; } = \"Node\";\n    public int? Score { get; set; } = default;\n\n    public Node(int id = 1)\n    {\n        Id = id;\n    }\n}\n\nNode node = new Node(10);\nConsole.WriteLine(node.Id == 10);\nConsole.WriteLine(node.Name != null);"
  },
  "object-initializers": {
    "title": "Object initializers",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Fields and properties can be assigned after one construction expression using the same access rules as normal assignments.",
    "code": "Node node = new Node(id: 50)\n{\n    Name = \"Override\",\n    Score = 12\n};\n\nConsole.WriteLine(node.Id == 50);\nConsole.WriteLine(node.Score.Value == 12);"
  },
  "array-initializers": {
    "title": "Array initializers",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Arrays can be created and populated inline, including managed references, nullable values, target-typed defaults, and converted elements.",
    "code": "int[] values = new int[] { 1, 2, 3 };\nint?[] nullable = new int?[] { 5, null, 9 };\n\nNode[] nodes = new Node[]\n{\n    new Node(10),\n    new Node(20) { Score = default }\n};\n\nConsole.WriteLine(values.Length == 3);\nConsole.WriteLine(!nullable[1].HasValue);"
  },
  "collection-initializers": {
    "title": "Collection initializers",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Collection initializer syntax lowers through normal Add(...) binding for lists, sets, dictionaries, custom collections, params, and conversions.",
    "code": "using Void.Collections;\n\nList<int> values = new() { 1, 2, 3 };\nHashSet<int> unique = new() { 7, 7, 8 };\n\nDictionary<string, int> scores = new()\n{\n    { \"Alpha\", 10 },\n    { \"Beta\", 20 }\n};\n\nConsole.WriteLine(values.Count == 3);\nConsole.WriteLine(unique.Count == 2);"
  },
  "static-members": {
    "title": "Static members",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Classes and structs can own static fields and properties, including managed references, nullable structs, accessors, and static readonly state.",
    "code": "public static class Registry\n{\n    public static readonly int Seed = 10;\n    public static Node Primary { get; set; } = new Node(Seed);\n    public static int? LastScore = default;\n\n    public static int Count { get; private set; }\n}\n\nConsole.WriteLine(Registry.Seed == 10);\nConsole.WriteLine(Registry.Primary.Id == 10);"
  },
  "static-initialization": {
    "title": "Static initialization",
    "category": "Nullability & initialization",
    "language": "VOID",
    "caption": "Static field/property initializers and static constructors run once before first use, including generic types and managed GC roots.",
    "code": "public static class Registry\n{\n    public static List<Node> Nodes { get; set; } = new()\n    {\n        new Node(10),\n        new Node(20)\n    };\n\n    public static int Ready { get; private set; } = 0;\n\n    static Registry()\n    {\n        Ready = Nodes.Count;\n        GC.Collect();\n    }\n}\n\nConsole.WriteLine(Registry.Ready == 2);"
  },
  "enums": {
    "title": "Enums",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "Named enum values, explicit values, underlying byte/sbyte storage, comparisons, parameters, and returns.",
    "code": "public enum Direction\n{\n    None,\n    Left = 4,\n    Right,\n    Up = 9\n}\n\npublic enum Tiny : byte\n{\n    Zero,\n    Max = 200,\n    Next\n}\n\nDirection current = Direction.Right;\nConsole.WriteLine(current == Direction.Right);\nConsole.WriteLine(Direction.Right < Direction.Up);\nConsole.WriteLine(Tiny.Zero < Tiny.Max);"
  },
  "switch": {
    "title": "Switch statements",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "switch/case/default works with enums, grouped cases, break, continue, nested switches, integers, and chars.",
    "code": "public enum State\n{\n    Idle,\n    Walk,\n    Attack,\n    Dead\n}\n\npublic static int Describe(State state)\n{\n    switch (state)\n    {\n        case State.Idle:\n            return 10;\n\n        case State.Walk:\n        case State.Attack:\n            return 20;\n\n        default:\n            return 30;\n    }\n}\n\nConsole.WriteLine(Describe(State.Walk));"
  },
  "const-readonly": {
    "title": "const + readonly",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "Compile-time constants and constructor-initialized readonly fields follow distinct assignment rules.",
    "code": "public static class Rules\n{\n    public const int BaseDamage = 10;\n    public const int CriticalDamage = BaseDamage + BaseDamage;\n    public const string DefaultName = \"VOID\";\n}\n\npublic sealed class Player\n{\n    public readonly int Id;\n    public readonly string Name;\n\n    public Player(int id, string name)\n    {\n        Id = id;\n        Name = name;\n    }\n}"
  },
  "casts-is-as": {
    "title": "Casts + is / as",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "Reference type tests, safe as-casts, explicit reference casts, numeric casts, and enum casts share one type system.",
    "code": "Animal animal = new Dog();\n\nConsole.WriteLine(animal is Dog);\nConsole.WriteLine(animal is IAnimal);\n\nDog dog = animal as Dog;\nCat cat = animal as Cat;\nDog required = (Dog)animal;\n\nfloat value = 12.75f;\nint whole = (int)value;\n\nint rawMode = (int)Mode.Run;\nMode mode = (Mode)1;"
  },
  "null-operators": {
    "title": "Null operators",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "Null coalescing, coalescing assignment, and null-conditional access short-circuit managed-reference expressions.",
    "code": "Hero hero = null;\nHero fallback = new Hero();\n\nHero chosen = hero ?? fallback;\n\nhero ??= new Hero(20, \"Fallback\");\n\nstring name = hero?.Name;\nstring childName = hero?.Child?.Name;\n\nConsole.WriteLine(chosen != null);\nConsole.WriteLine(name != null);"
  },
  "optional-params": {
    "title": "Optional parameters + params",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "Optional/default parameters and params arrays are resolved and packed at normal call sites.",
    "code": "public static int Sum(\n    int start = 0,\n    params int[] values)\n{\n    int total = start;\n    for (int i = 0; i < values.Length; i++)\n        total += values[i];\n\n    return total;\n}\n\nConsole.WriteLine(Sum());\nConsole.WriteLine(Sum(10));\nConsole.WriteLine(Sum(10, 1, 2, 3));"
  },
  "default-values": {
    "title": "default(T)",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "default(T) produces the correct zero/null/default value through concrete and generic type contexts.",
    "code": "public static T Zero<T>()\n{\n    return default(T);\n}\n\nint number = default(int);\nGameState state = default(GameState);\nHero hero = default(Hero);\nDamage damage = default(Damage);\n\nConsole.WriteLine(Zero<int>() == 0);\nConsole.WriteLine(Zero<GameState>() == GameState.None);\nConsole.WriteLine(Zero<Hero>() == null);"
  },
  "type-metadata": {
    "title": "typeof + type metadata",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "typeof returns runtime Type metadata for classes, structs, enums, primitives, interfaces, and generic type arguments.",
    "code": "public static Type TypeOf<T>()\n{\n    return typeof(T);\n}\n\nType heroType = typeof(Hero);\nType stateType = TypeOf<GameState>();\nType damageType = typeof(Damage);\n\nConsole.WriteLine(heroType.IsClass);\nConsole.WriteLine(heroType.IsReferenceType);\nConsole.WriteLine(stateType.IsEnum);\nConsole.WriteLine(stateType.UnderlyingType == typeof(int));\nConsole.WriteLine(damageType.IsStruct);\nConsole.WriteLine(damageType.IsValueType);"
  },
  "conversions": {
    "title": "implicit / explicit conversions",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "User-defined implicit and explicit conversion operators participate in assignments, calls, returns, casts, generics, and params packing.",
    "code": "public struct Meters\n{\n    public int Value;\n\n    public Meters(int value)\n    {\n        Value = value;\n    }\n\n    public static implicit operator Meters(int value)\n    {\n        return new Meters(value);\n    }\n\n    public static implicit operator int(Meters value)\n    {\n        return value.Value;\n    }\n\n    public static explicit operator float(Meters value)\n    {\n        return (float)value.Value;\n    }\n}\n\nMeters distance = 5;\nint raw = distance;\nfloat precise = (float)distance;"
  },
  "ergonomics-integration": {
    "title": "Ergonomics integration",
    "category": "Core language ergonomics",
    "language": "VOID",
    "caption": "The completed core language ergonomics features compose through one cumulative integration path without adding another language feature.",
    "code": "public static int Resolve(\n    GameState state = Rules.DefaultState,\n    params Damage[] values)\n{\n    int total = 0;\n    for (int i = 0; i < values.Length; i++)\n        total += values[i].Value;\n\n    switch (state)\n    {\n        case Rules.DefaultState:\n            return total + Rules.Bonus;\n        case GameState.Paused:\n            return default(int);\n        default:\n            return -1;\n    }\n}\n\nHero missing = null;\nHero hero = missing ?? new Hero();\nConsole.WriteLine(hero is Hero);\nConsole.WriteLine(typeof(Hero).IsClass);\nConsole.WriteLine(Resolve(GameState.Playing, 1, 2, 3));"
  },
  "collections": {
    "title": "Generic collections + foreach",
    "category": "Language & library",
    "language": "VOID",
    "caption": "Generic standard-library code using the shared enumeration model.",
    "code": "using Void.Collections;\n\nList<int> values = new();\nvalues.Add(3);\nvalues.Add(4);\nvalues.Add(5);\n\nint total = 0;\nforeach (var value in values)\n    total += value;"
  },
  "iterators": {
    "title": "Iterator methods + yield",
    "category": "Language & library",
    "language": "VOID",
    "caption": "yield lowers to a compiler-generated iterator state machine.",
    "code": "public static IEnumerator<int> Numbers()\n{\n    yield return 1;\n    yield return 2;\n    yield return 3;\n    yield break;\n}"
  },
  "closures": {
    "title": "Lambdas + closures",
    "category": "Language & library",
    "language": "VOID",
    "caption": "Captured state lives in GC-managed closure objects.",
    "code": "int factor = 2;\nFunc<int, int> times = x => x * factor;\n\nfactor = 3;\nConsole.WriteLine(times(4));\n\nAction<int> echo = x => Console.WriteLine(x);"
  },
  "events": {
    "title": "Events",
    "category": "Language & library",
    "language": "VOID",
    "caption": "Field-like events build on delegates and preserve subscriber lifetime.",
    "code": "public sealed class Publisher\n{\n    public event Action<int> Changed;\n\n    public void Raise(int value)\n    {\n        if (Changed != null)\n            Changed(value);\n    }\n}"
  },
  "discovery": {
    "title": "Runtime discovery",
    "category": "Language & library",
    "language": "VOID",
    "caption": "Focused attribute metadata supports game-style discovery without a giant reflection system.",
    "code": "[Discoverable(Name = \"Level\", Metadata = \"Farm\")]\npublic class FarmLevel\n{\n}\n\nstring name = Runtime.AttributeValue(\n    \"FarmLevel\", \"Discoverable\", \"Metadata\");"
  },
  "pointers": {
    "title": "Unsafe pointers",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "Address-of, dereference, pointer arithmetic, and pointer comparisons stay inside unsafe methods.",
    "code": "public static unsafe void Main()\n{\n    int value = 7;\n    int* pointer = &value;\n\n    *pointer = 12;\n\n    int[] values = new int[4];\n    int* start = &values[0];\n    int* cursor = start + 2;\n\n    Console.WriteLine(cursor - start);\n}"
  },
  "native-structs": {
    "title": "Native structs",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "Unmanaged structs can cross the C ABI with native-compatible field layout.",
    "code": "public struct TimeValue\n{\n    public long Seconds;\n    public long Microseconds;\n}\n\npublic static class NativeC\n{\n    public static unsafe extern int gettimeofday(\n        TimeValue* value,\n        void* zone);\n}"
  },
  "native-byref": {
    "title": "Native ref / out / in",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "By-reference parameter modes lower to the pointer forms expected by native C APIs.",
    "code": "public static class NativeC\n{\n    public static unsafe extern long time(ref long value);\n    public static unsafe extern int gettimeofday(\n        out TimeValue value,\n        void* zone);\n    public static unsafe extern int nanosleep(\n        in TimeSpec request,\n        TimeSpec* remaining);\n}"
  },
  "native-aliases": {
    "title": "Native symbol aliases",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "Readable VOID method names can bind directly to existing C symbol names.",
    "code": "public static class NativeMath\n{\n    [Native(\"abs\")]\n    public static unsafe extern int Abs(int value);\n\n    [Native(\"toupper\")]\n    public static unsafe extern int ToUpper(int value);\n}"
  },
  "callbacks": {
    "title": "Native callbacks",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "Compatible static methods can cross the ABI as raw native function pointers.",
    "code": "public delegate int Transform(int value);\n\npublic static class NativeFixture\n{\n    [Native(\"game_apply\")]\n    public static unsafe extern int Apply(\n        int value,\n        Transform callback);\n}\n\npublic static int Double(int value)\n{\n    return value * 2;\n}"
  },
  "memory": {
    "title": "sizeof + raw memory",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "Focused memory primitives support unmanaged layouts without becoming a general unsafe runtime.",
    "code": "Pair source = new Pair();\nsource.A = 12;\nsource.B = 34;\n\nPair target = new Pair();\nRuntime.MemoryClear(&target, sizeof(Pair));\nRuntime.MemoryCopy(&target, &source, sizeof(Pair));\n\nConsole.WriteLine(sizeof(Pair));"
  },
  "stackalloc": {
    "title": "Stack allocation",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "stackalloc creates lexical unmanaged temporary storage for native buffers.",
    "code": "public static unsafe void Main()\n{\n    int* values = stackalloc int[4];\n    values[0] = 10;\n    values[1] = 20;\n\n    int* cursor = values + 1;\n    Console.WriteLine(*cursor);\n}"
  },
  "local-library": {
    "title": "Project-local native library",
    "category": "Unsafe & native",
    "language": ".voidproj",
    "caption": "Native dependencies can live beside a project and be found through project-relative search paths.",
    "code": "{\n  \"format\": 1,\n  \"name\": \"NativeIntegration\",\n  \"output\": \"exe\",\n  \"compiler\": {\n    \"unsafe\": true,\n    \"libraryPaths\": [\"native\"],\n    \"libraries\": [\"game_native\"]\n  }\n}"
  },
  "integration": {
    "title": "End-to-end native integration",
    "category": "Unsafe & native",
    "language": "VOID",
    "caption": "The controlled C fixture combines callbacks, native structs, by-reference parameters, stack memory, and local linking in one regression.",
    "code": "Pair pair = new Pair();\nNativeFixture.MakePair(7, 9, out pair);\nNativeFixture.ShiftPair(ref pair, 3, 4);\n\nConsole.WriteLine(NativeFixture.Apply(6, Double));\n\nint* values = stackalloc int[4];\nRuntime.MemoryClear(values, sizeof(int) * 4);\nNativeFixture.Fill(values, 4);\n\nPair copy = new Pair();\nRuntime.MemoryCopy(&copy, &pair, sizeof(Pair));"
  },
  "source-spans": {
    "title": "Source spans",
    "category": "Compiler tooling",
    "language": "Compiler model",
    "caption": "Tokens and syntax carry end-exclusive source ranges, giving later tooling a precise foundation for selections, navigation, and editor queries.",
    "code": "// Source range convention: start is inclusive, end is exclusive.\n\npublic class Player\n{\n    public int Score { get; set; }\n}\n\n// Example token ranges:\n1:1-1:7   public\n1:8-1:13  class\n1:14-1:20 Player\n\n// A semantic reference can now point at the full identifier range:\n8:12-8:24 MissingValue"
  },
  "ranged-diagnostics": {
    "title": "Ranged diagnostics",
    "category": "Compiler tooling",
    "language": "Compiler output",
    "caption": "Lexer, parser, and semantic errors report end-exclusive start/end ranges through one shared diagnostic model.",
    "code": "Program.void:6:5-6:6: error: expected ';', found '}'\n\nBlockComment.void:1:1-3:1: error: unterminated block comment\n\nProgram.void:8:12-8:24: error: unknown identifier 'MissingValue'\n\n// Token ranges are exposed too:\n1:1-1:7 public             \"public\"\n1:8-1:13 class             \"class\""
  },
  "source-checking": {
    "title": "Source-only checking",
    "category": "Compiler tooling",
    "language": "CLI",
    "caption": "voidc check runs the same front-end analysis as a real build, but stops before C generation, native compilation, or output creation.",
    "code": "./bin/voidc check MyGame\n\nVOID project resolved\n  mode:      project\n  root:      MyGame\n  ...\n\nchecked: MyGame\n\n// No generated C, bin, publish, or native compiler invocation."
  },
  "semantic-queries": {
    "title": "Semantic queries",
    "category": "Compiler tooling",
    "language": "Compiler API",
    "caption": "One persistent analyzed session can answer editor-facing symbol, type, definition, member, and parameter questions without reparsing the project.",
    "code": "VcQuerySession *session = vc_query_session_create(...);\n\nvc_query_symbol_at(session, source_path, offset, &symbol);\nvc_query_type_at(session, source_path, offset, &type);\nvc_query_definition_at(session, source_path, offset, &definition);\nvc_query_members_at(session, source_path, offset, &members);\nvc_query_parameter_at(session, source_path, offset, 0, &parameter);\n\nvc_query_session_destroy(session);"
  },
  "debug-source-mapping": {
    "title": "Debug source mapping",
    "category": "Compiler tooling",
    "language": "Generated C / DWARF",
    "caption": "Debug builds emit standard #line mappings so native debug information points back to original VOID filenames and statement lines.",
    "code": "// VOID source\nint sum = left + right;\n\n// Generated C\n#line 7 \"Game/Player.void\"\nint32_t sum = left + right;\n\n// Native debug tools can report:\nGame/Player.void:7"
  },
  "lsp-bootstrap": {
    "title": "LSP bootstrap",
    "category": "Compiler tooling",
    "language": "LSP / JSON-RPC",
    "caption": "voidc hosts the first language-server protocol layer directly, with lifecycle handling, full-document synchronization, in-memory document state, and project discovery.",
    "code": "./bin/voidc lsp\n\n// initialize advertises full-document sync\n{\n  \"capabilities\": { \"textDocumentSync\": 1 },\n  \"serverInfo\": { \"name\": \"voidc\" }\n}\n\n// document notifications\ntextDocument/didOpen\ntextDocument/didChange\ntextDocument/didClose\n\n// lifecycle\ninitialize -> initialized -> shutdown -> exit"
  },
  "live-diagnostics": {
    "title": "Live LSP diagnostics",
    "category": "Compiler tooling",
    "language": "VOID / LSP",
    "caption": "Unsaved editor text is checked through the real compiler front end, published with precise ranges, and cleared as soon as the buffer becomes valid.",
    "code": "// Program.void (unsaved)\nint score = MissingValue;\n\n// voidc lsp publishes:\nProgram.void:1:13-1:25\nunknown identifier 'MissingValue'\n\n// Edit the same in-memory document:\nint score = 10;\n\n// The server publishes an empty diagnostic list.\n// Nothing is written to disk."
  },
  "hover-signature-help": {
    "title": "Hover + signature help",
    "category": "Compiler tooling",
    "language": "VOID / LSP",
    "caption": "Hover and signatures come from the compiler's bound symbols and call mapping, including overloads, optional parameters, params, delegates, and named arguments.",
    "code": "int mixed = calc.Mix(right: 4, left: 2);\n\n// Hover on Mix:\nmethod Mix(int left, int right = ...): int\n\n// Signature help knows the argument mapping:\n// written first: right: 4\n// active parameter: right\n\n// Constructors and delegate invocations use\n// the same semantic query path."
  },
  "semantic-completion": {
    "title": "Semantic completion",
    "category": "Compiler tooling",
    "language": "VOID / LSP",
    "caption": "Completion is driven by resolved types and accessibility rules instead of text matching.",
    "code": "Player player = new Player();\n\n// At player. -> accessible instance members:\nplayer.Health;\nplayer.Damage(5);\n\n// At Player. -> static members only:\nPlayer.Count;\n\n// Also supported:\n// inherited members\n// enum members\n// locals + parameters\n// project types + namespaces\n// private/protected accessibility filtering"
  },
  "navigation-symbols": {
    "title": "Navigation + symbols",
    "category": "Compiler tooling",
    "language": "VOID / LSP",
    "caption": "Definitions and references use semantic declaration identity, while document/workspace symbols come from real compiler declarations.",
    "code": "// Program.void\nplayer.Damage(5);\n\n// Go to definition:\n// -> Player.void : Damage(int amount)\n\n// Find references:\n// -> semantic uses of the same declaration\n//    with optional declaration inclusion\n\n// Symbol queries also expose:\n// documentSymbol\n// workspace/symbol\n// namespaces, types, methods, fields,\n// properties, events, constants, enum members"
  },
  "tooling-integration": {
    "title": "Multi-file editor integration",
    "category": "Compiler tooling",
    "language": "VOID / LSP",
    "caption": "All open files in one VOID project are analyzed from the same in-memory snapshot, so unsaved cross-file edits never fall back to stale disk state.",
    "code": "// Program.void (unsaved)\nPlayer player = new Player();\nplayer.Heal(5);\n\n// Player.void (unsaved)\npublic void Heal(int amount)\n{\n    Health += amount;\n}\n\n// Both buffers are analyzed together.\n// Program.void diagnostics clear immediately.\n// Hover, definition, references, and workspace\n// symbols can all see Heal before either file is saved.\n\n// Temporarily break Player.void -> parser diagnostic.\n// Fix it -> semantic queries recover.\n// Close Player.void -> compiler falls back to disk."
  }
};

(() => {
  const init = () => {
    const browser = document.querySelector('[data-example-browser]');
    if (!browser) return;

    const select = browser.querySelector('[data-example-select]');
    const code = browser.querySelector('[data-example-code]');
    const title = browser.querySelector('[data-example-title]');
    const caption = browser.querySelector('[data-example-caption]');
    const category = browser.querySelector('[data-example-category]');
    const language = browser.querySelector('[data-example-language]');
    if (!select || !code) return;

    const showExample = (value) => {
      const entry = window.VOID_EXAMPLES?.[value];
      if (!entry) return;

      window.VOID_SYNTAX?.highlightCode(code, entry.code || '');
      if (title) title.textContent = entry.title || '';
      if (caption) caption.textContent = entry.caption || '';
      if (category) category.textContent = entry.category || '';
      if (language) language.textContent = entry.language || 'VOID';
    };

    select.addEventListener('change', () => showExample(select.value));
    showExample(select.value);
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init, { once: true });
  } else {
    init();
  }
})();
