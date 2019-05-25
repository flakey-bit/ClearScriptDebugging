using System;
using Microsoft.ClearScript.V8;

namespace Target
{
    public class Program
    {
        static void Main()
        {
            V8ScriptEngineFlags flags = V8ScriptEngineFlags.EnableDebugging |
                                        V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart;

            using (var engine1 = new V8ScriptEngine(flags, 8881))
            {
                Console.Out.WriteLine("Starting script...");
                engine1.Execute(script);
                Console.Out.WriteLine($"Script executed, result: {engine1.Script.result}");
            }

            Console.ReadKey();
        }

        private static string script = @"
var func = function() {
    var two;
    debugger;
    return two + 2;
};

var result = func();
";
    }
}