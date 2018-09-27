﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using DaySim;
using DaySim.Framework.Core;
using NDesk.Options;
using Ninject;

namespace DaySimController {
  public static class Program {
    private static string _configurationPath;
    private static string _printFilePath;
    private static bool _showHelp;
    private static string _overrides = "";

    private static void Main(string[] args) {
      OptionSet options = new OptionSet {
                {"c|configuration=", "Path to configuration file", v => _configurationPath = v},
                    {"o|overrides=", "comma delimited name=value pairs to override configuration file values", v => _overrides = v},
                    {"p|printfile=", "Path to print file", v => _printFilePath = v},
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


      using (DaySimModule daysimModule = new DaySimModule()) {
        Global.Kernel = new StandardKernel(daysimModule);

        Controller.BeginProgram();
      }
    }
  }
}
