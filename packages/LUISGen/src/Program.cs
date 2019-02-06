// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.IO;

namespace LUISGen
{
    public class Program
    {
        static void Usage()
        {
            Console.Error.WriteLine("LUISGen <LUIS.json> [--stdin] [-cs [CLASS]] [-ts [INTERFACE]] [-o PATH] [-v] [--version]");
            Console.Error.WriteLine("From a LUIS export file generate a strongly typed class for consuming intents and entities.");
            Console.Error.WriteLine("[--stdin], optionally get the export file from stdin instead of providing JSON file path");
            Console.Error.WriteLine("At least one of -cs or -ts must be supplied.");
            Console.Error.WriteLine("-cs [CLASS] : Generate C# class file including namespace.  Default is Luis.<appName> if no class name is specified.");
            Console.Error.WriteLine("-ts [INTERFACE] : Generate Typescript interface descriptions.  Default is <appName> if no class name is specified.");
            Console.Error.WriteLine("-o PATH : Where to put generated files, defaults to directory where export file is.");
            Console.Error.WriteLine("-v --version : Report the LUISGen version.");
            System.Environment.Exit(-1);
        }

        static string NextArg(ref int i, string[] args, bool optional = false, bool allowCmd = false)
        {
            string arg = null;
            if (i < args.Length)
            {
                arg = args[i];
                if (arg.StartsWith("{"))
                {
                    while (!args[i].EndsWith("}") && ++i < args.Length) ;
                    ++i;
                }
                arg = null;
                if (allowCmd)
                {
                    if (i < args.Length)
                    {
                        arg = args[i];
                    }
                }
                else
                {
                    if (i < args.Length && !args[i].StartsWith('-'))
                    {
                        arg = args[i];
                    }
                    else if (!optional)
                    {
                        Usage();
                    }
                    else
                    {
                        --i;
                    }
                }
            }
            return arg?.Trim();
        }

        public static void Main(string[] args)
        {
            string path = null;
            string outPath = null;
            string outType = null;
            var space = "Luis";
            string className = null;
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = NextArg(ref i, args, allowCmd: true);
                if (arg != null)
                {
                    if (arg.StartsWith('-'))
                    {
                        arg = arg.ToLower();
                        switch (arg)
                        {
                            case "-cs":
                            case "-ts":
                                {
                                    ++i;
                                    var name = NextArg(ref i, args, optional: true);
                                    if (name != null)
                                    {
                                        var lastDot = arg == "-cs" ? name.LastIndexOf('.') : -1;
                                        if (lastDot == -1)
                                        {
                                            className = name;
                                        }
                                        else
                                        {
                                            space = name.Substring(0, lastDot);
                                            className = name.Substring(lastDot + 1);
                                        }
                                    }
                                    outType = arg.Substring(1);
                                }
                                break;
                            case "--stdin":
                                {
                                    path = "--stdin";
                                    break;
                                }
                            case "-o":
                                {
                                    ++i;
                                    outPath = NextArg(ref i, args);
                                }
                                break;
                            case "-v":
                            case "--version":
                                Console.WriteLine($"LUISGen version {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                                break;
                            default:
                                Usage();
                                break;
                        }
                    }
                    else if (path == null)
                    {
                        path = arg;
                    }
                    else
                    {
                        Usage();
                    }
                }
            }
            if (path == null || outType == null)
            {
                Usage();
            }
            outPath = outPath ?? (path == "--stdin" ? "." : Path.GetDirectoryName(path));

            if (path == "--stdin")
            {
                Console.Error.WriteLine("Reading from stdin until EOF (Ctrl-Z).");
            }
            dynamic app;
            using (var inFile = path == "--stdin" ? Console.In : new StreamReader(path))
            {
                app = JsonConvert.DeserializeObject(inFile.ReadToEnd());
            }
            className = className ?? ((string)app.name).Replace(' ', '_');
            if (outType == "cs")
            {
                var description = $"LUISGen {path} -cs {space}.{className} -o {outPath}";
                CSharp.Generate(description, app, className, space, outPath);
            }
            else if (outType == "ts")
            {
                var description = $"LUISGen {path} -ts {className} -o {outPath}";
                Typescript.Generate(description, app, className, outPath);
            }
        }
    }
}
