using System;
using System.Collections.Generic;
using Cubelang;
using Cubelang.Desktop;

CubelangDesktop desktop = new CubelangDesktop();
if (args.Length > 0)
{
    if (args.Length > 1)
        desktop.ExecuteFile(args[0], args[1..]);
    else
        desktop.ExecuteFile(args[0]);
}
else
{
    Console.WriteLine($"Cubelang runtime - Ollie Robinson 2022\nVersion {CubelangBase.Version}\nPress Ctrl+C to exit.");
    bool executeNow = true;
    List<string> lineCache = new List<string>();
    while (true)
    {
        if (lineCache.Count > 0)
            Console.Write(">>> ");
        else
            Console.Write("> ");
        lineCache.Add(Console.ReadLine());
        if (lineCache[0].StartsWith("if"))
        {
            executeNow = false;
            if (lineCache[^1] == "endif")
                executeNow = true;
        }
        
        if (lineCache[0].StartsWith("repeat"))
        {
            executeNow = false;
            if (lineCache[^1] == "endrep")
                executeNow = true;
        }

        if (executeNow)
        {
            desktop.Execute(string.Join('\n', lineCache));
            lineCache.Clear();
        }
    }
}