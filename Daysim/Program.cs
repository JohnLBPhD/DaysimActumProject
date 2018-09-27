// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.IO;
using DaySim.DomainModels.Factories;
using DaySim.Framework.Core;
using DaySim.Settings;
using NDesk.Options;
using Ninject;

namespace DaySim {
  public static class Program {
    private static string _configurationPath;
    private static string _printFilePath;
    private static int _start = -1;
    private static int _end = -1;
    private static int _index = -1;
    private static bool _showHelp;
    private static bool _showVersion;
    private static string _overrides = "";

    private static void Main(string[] args) {
      try {
        OptionSet options = new OptionSet {
                    {"c|configuration=", "Path to configuration file", v => _configurationPath = v},
                    {"o|overrides=", "comma delimited name=value pairs to override configuration file values", v => _overrides = v},
                    {"p|printfile=", "Path to print file", v => _printFilePath = v},
                    {"s|start=", "Start index of household range", v => _start = int.Parse(v)},
                    {"e|end=", "End index of household range", v => _end = int.Parse(v)},
                    {"i|index=", "Cluser index", v => _index = int.Parse(v)},
                    {"v|version", "Show version information", v => _showVersion = v != null},
                    {"h|?|help", "Show help and syntax summary", v => _showHelp = v != null}
                };
        options.Parse(args);

        if (_showHelp) {
          options.WriteOptionDescriptions(Console.Out);

          Console.WriteLine();
          Console.WriteLine("If you do not provide a configuration then the default is to use {0}, in the same directory as the executable.", ConfigurationManagerRSG.DEFAULT_CONFIGURATION_NAME);

          Console.WriteLine();
          Console.WriteLine("If you do not provide a printfile then the default is to create {0}, in the same directory as the executable.", PrintFile.DEFAULT_PRINT_FILENAME);

          Console.WriteLine("Please press any key to exit");
          Console.ReadKey();

          Environment.Exit(0);
        }
        Console.WriteLine("Configuration file: " + _configurationPath);
        if (!File.Exists(_configurationPath)) {
          throw new Exception("Configuration file '" + _configurationPath + "' does not exist. You must pass in a DaySim configuration file with -c or --configuration");
        }
        ConfigurationManagerRSG configurationManager = new ConfigurationManagerRSG(_configurationPath);
        Global.Configuration = configurationManager.Open();

        Global.Configuration = configurationManager.OverrideConfiguration(Global.Configuration, _overrides);
        Global.Configuration = configurationManager.ProcessPath(Global.Configuration, _configurationPath);
        Global.PrintFile = configurationManager.ProcessPrintPath(Global.PrintFile, _printFilePath);

        string message = string.Format("--overrides = {0}", _overrides);
        Console.WriteLine(message);
        if (Global.PrintFile != null) {
          Global.PrintFile.WriteLine(message);
        }

        SettingsFactory settingsFactory = new SettingsFactory(Global.Configuration);
        Framework.Factories.ISettings settings = settingsFactory.Create();
        ParallelUtility.Init(Global.Configuration);
        Global.Settings = settings;
        Global.Kernel = new StandardKernel(new DaySimModule());

        ModuleFactory moduleFactory = new ModuleFactory(Global.Configuration);
        Ninject.Modules.NinjectModule modelModule = moduleFactory.Create();

        Global.Kernel.Load(modelModule);

        Engine.BeginProgram(_start, _end, _index);
        //Engine.BeginTestMode();
      } catch (Exception e) {
        Console.WriteLine();
        Console.WriteLine(e.GetBaseException().Message);

        Console.WriteLine();
        Console.WriteLine(e.StackTrace);

        Console.WriteLine();
        Console.WriteLine("Please press any key to exit");

        if (Global.PrintFile != null) {
          Global.PrintFile.WriteLine(e.GetBaseException().Message);
          Global.PrintFile.WriteLine();
          Global.PrintFile.WriteLine(e.StackTrace);
        }

        Console.ReadKey();

        Environment.Exit(2);
      } finally {
        if (Global.PrintFile != null) {
          Global.PrintFile.Dispose();
        }
      }
      Environment.Exit(0);
    }
  }
}
