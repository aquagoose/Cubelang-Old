using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cubelang.Desktop;

public class CubelangDesktop : CubelangBase
{
    public void log(object text)
    {
        Console.WriteLine(str(text));
    }

    public string prompt(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }

    public void file_save(string path, object data) => File.WriteAllText(path, str(data));

    public string file_read(string path) => File.ReadAllText(path);

    public bool file_exists(string path) => File.Exists(path);

    public void Execute(string code, params string[] args)
    {
        if (args.Length > 0)
        {
            List<object> newArgs = new List<object>();
            foreach (string item in args)
            {
                newArgs.Add(item);
            }

            Variables.Add("args", newArgs.ToList());
        }

        try
        {
            base.Execute(code);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void ExecuteFile(string path, params string[] args)
    {
        Execute(File.ReadAllText(path), args);
    }
}