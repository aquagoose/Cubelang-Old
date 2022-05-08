using Cubelang.Desktop;

CubelangDesktop desktop = new CubelangDesktop();
if (args.Length > 1)
    desktop.ExecuteFile(args[0], args[1..]);
else
    desktop.ExecuteFile(args[0]);