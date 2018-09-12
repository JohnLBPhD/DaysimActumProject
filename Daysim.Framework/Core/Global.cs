// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DaySim.Framework.ChoiceModels;
using DaySim.Framework.DomainModels.Models;
using DaySim.Framework.DomainModels.Wrappers;
using DaySim.Framework.Factories;
using DaySim.Framework.Sampling;
using Ninject;

namespace DaySim.Framework.Core {
  public static class Global {
    public static IKernel Kernel { get; set; }

    public static Configuration Configuration { get; set; }

    public static ISettings Settings { get; set; }

    public static ChoiceModelSession ChoiceModelSession { get; set; }

    public static PrintFile PrintFile { get; set; }

    public static bool TraceResults { get; set; }

    public static int[] ANodeId { get; set; }

    public static int[] ANodeFirstRecord { get; set; }

    public static int[] ANodeLastRecord { get; set; }

    public static int[] NodePairBNodeId { get; set; }

    public static ushort[] NodePairDistance { get; set; }

    public static int[] NodeNodePreviousOriginParcelId { get; set; }

    public static int[] NodeNodePreviousDestinationParcelId { get; set; }

    public static double[] NodeNodePreviousDistance { get; set; }

    public static double[][][][][] AggregateLogsums { get; set; }

    public static SegmentZone[][] SegmentZones { get; set; }

    public static Dictionary<int, int> NodeIndex { get; set; }

    public static Dictionary<int, int> TransitStopAreaMapping { get; set; }

    public static Dictionary<int, ITransitStopAreaWrapper> TransitStopAreaDictionary { get; set; }
    public static Dictionary<int, ITransitStopAreaWrapper>.ValueCollection TransitStopAreas { get; set; }

    public static Dictionary<int, int> MicrozoneMapping { get; set; }

    public static Dictionary<int, int> ParkAndRideNodeMapping { get; set; }

    public static int[] ParcelStopAreaParcelIds { get; set; }
    public static int[] ParcelStopAreaStopAreaKeys { get; set; }
    public static int[] ParcelStopAreaStopAreaIds { get; set; }
    public static float[] ParcelStopAreaLengths { get; set; }

    public static int[] ParcelParkAndRideNodeIds { get; set; }
    public static int[] ParcelParkAndRideNodeSequentialIds { get; set; }
    public static float[] ParcelToBikeCarParkAndRideNodeLength { get; set; }

    public static double Coefficients_CostCoefficientIncomeMultipleMinimum => Math.Abs(Configuration.Coefficients_CostCoefficientIncomeMultipleMinimum) < Constants.EPSILON
                    ? 0.1
                    : Configuration.Coefficients_CostCoefficientIncomeMultipleMinimum;

    public static double Coefficients_CostCoefficientIncomeMultipleMaximum => Math.Abs(Configuration.Coefficients_CostCoefficientIncomeMultipleMaximum) < Constants.EPSILON
                    ? 10.0
                    : Configuration.Coefficients_CostCoefficientIncomeMultipleMaximum;

    public static string AggregateLogsumCalculator => string.IsNullOrEmpty(Configuration.AggregateLogsumCalculator)
                    ? "AggregateLogsumCalculator"
                    : Configuration.AggregateLogsumCalculator;

    public static string PathTypeModel => string.IsNullOrEmpty(Configuration.PathTypeModel)
                    ? "PathTypeModel"
                    : Configuration.PathTypeModel;

    public static bool PSRC => Configuration.PSRC;
    public static bool Nashville => Configuration.Nashville;

    public static bool ParkAndRideNodeIsEnabled => !string.IsNullOrEmpty(Configuration.RawParkAndRideNodePath) && !string.IsNullOrEmpty(Configuration.InputParkAndRideNodePath);

    public static string DefaultInputParkAndRideNodePath => GetWorkingPath("park_and_ride_node.tsv");

    public static bool ParcelNodeIsEnabled => !string.IsNullOrEmpty(Configuration.RawParcelNodePath) && !string.IsNullOrEmpty(Configuration.InputParcelNodePath);

    public static bool StopAreaIsEnabled => !string.IsNullOrEmpty(Configuration.RawTransitStopAreaPath) && !string.IsNullOrEmpty(Configuration.InputTransitStopAreaPath);

    public static string DefaultInputParcelNodePath => GetWorkingPath("parcel_node.tsv");

    public static string DefaultInputParcelPath => GetWorkingPath("parcel.tsv");

    public static string DefaultInputZonePath => GetWorkingPath("zone.tsv");

    public static string DefaultInputTransitStopAreaPath => GetWorkingPath("stoparea.tsv");

    public static string DefaultInputHouseholdPath => GetWorkingPath("household.tsv");

    public static string DefaultInputHouseholdDayPath => GetWorkingPath("household_day.tsv");

    public static string DefaultInputJointTourPath => GetWorkingPath("joint_tour.tsv");

    public static string DefaultInputFullHalfTourPath => GetWorkingPath("full_half_tour.tsv");

    public static string DefaultInputPartialHalfTourPath => GetWorkingPath("partial_half_tour.tsv");

    public static string DefaultInputPersonPath => GetWorkingPath("person.tsv");

    public static string DefaultInputPersonDayPath => GetWorkingPath("person_day.tsv");

    public static string DefaultInputTourPath => GetWorkingPath("tour.tsv");

    public static string DefaultInputTripPath => GetWorkingPath("trip.tsv");

    public static string WorkingParkAndRideNodePath => GetWorkingPath("park_and_ride_node.bin");

    public static string WorkingParcelNodePath => GetWorkingPath("parcel_node.bin");

    public static string WorkingParcelPath => GetWorkingPath("parcel.bin");

    public static string WorkingZonePath => GetWorkingPath("zone.bin");

    public static string WorkingTransitStopAreaPath => GetWorkingPath("transit_stop_area.bin");

    public static string WorkingHouseholdPath => GetWorkingPath("household.bin");

    public static string WorkingHouseholdDayPath => GetWorkingPath("household_day.bin");

    public static string WorkingJointTourPath => GetWorkingPath("joint_tour.bin");

    public static string WorkingFullHalfTourPath => GetWorkingPath("full_half_tour.bin");

    public static string WorkingPartialHalfTourPath => GetWorkingPath("partial_half_tour.bin");

    public static string WorkingPersonPath => GetWorkingPath("person.bin");

    public static string WorkingPersonDayPath => GetWorkingPath("person_day.bin");

    public static string WorkingTourPath => GetWorkingPath("tour.bin");

    public static string WorkingTripPath => GetWorkingPath("trip.bin");

    public static string AggregateLogsumsPath => GetWorkingPath("aggregate_logsums.bin");

    public static string SamplingWeightsPath => GetWorkingPath("sampling_weights_{0}.bin");

    public static string ShadowPricesPath => GetWorkingPath("shadow_prices.txt");

    public static string ParkAndRideShadowPricesPath => GetWorkingPath("park_and_ride_shadow_prices.txt");

    public static double Coefficients_BaseCostCoefficientPerMonetaryUnit => Math.Abs(Configuration.Coefficients_BaseCostCoefficientPerMonetaryUnit) > Constants.EPSILON
                    ? Configuration.Coefficients_BaseCostCoefficientPerMonetaryUnit
                    : Configuration.Coefficients_BaseCostCoefficientPerDollar;

    public static double PathImpedance_WalkMinutesPerDistanceUnit => Math.Abs(Configuration.PathImpedance_WalkMinutesPerDistanceUnit) > Constants.EPSILON
                ? Configuration.PathImpedance_WalkMinutesPerDistanceUnit
                : Configuration.PathImpedance_WalkMinutesPerMile;

    public static double PathImpedance_AutoOperatingCostPerDistanceUnit => Math.Abs(Configuration.PathImpedance_AutoOperatingCostPerDistanceUnit) > Constants.EPSILON
                ? Configuration.PathImpedance_AutoOperatingCostPerDistanceUnit
                : Configuration.PathImpedance_AutoOperatingCostPerMile;

    public static double Policy_IncomeMultiplier => Math.Abs(Configuration.Policy_IncomeMultiplier) > Constants.EPSILON
                ? Configuration.Policy_IncomeMultiplier
                : 1.0;

    public static char SkimDelimiter => Configuration.SkimDelimiter == 0 ? ' ' : Configuration.SkimDelimiter;

    public static bool TextSkimFilesContainHeaderRecord => Configuration.TextSkimFilesContainHeaderRecord;

    public static string SamplingWeightsSettingsType => string.IsNullOrEmpty(Configuration.SamplingWeightsSettingsType) ? "SamplingWeightsSettings" : Configuration.SamplingWeightsSettingsType;

    public static int MaximumHouseholdSize => Configuration.MaximumHouseholdSize != 0
                    ? Configuration.MaximumHouseholdSize
                    : 20;

    private static string GetSubpath(string file, string subPath) {
      if (file.Contains(":\\")) {
        return file;
      }

      if (Configuration.BasePath == null) {
        return
            subPath == null
                ? file
                : Path.Combine(subPath, file);
      }

      return
          subPath == null
              ? Path.Combine(Configuration.BasePath, file)
              : Path.Combine(Configuration.BasePath, subPath, file);
    }

    public static string GetInputPath(string file) {
      return GetSubpath(file, "");
    }

    public static string GetInputPath<TModel>() where TModel : IModel {
      string path = GetConfigurationValue<TModel, string>("Input", "Path");

      return GetInputPath(path);
    }

    public static char GetInputDelimiter<TModel>() where TModel : IModel {
      return GetConfigurationValue<TModel, char>("Input", "Delimiter");
    }

    public static string GetOutputPath(string file) {
      return GetSubpath(file, Configuration.OutputSubpath);
    }

    public static string GetOutputPath<TModel>() where TModel : IModel {
      string path = GetConfigurationValue<TModel, string>("Output", "Path");

      return GetOutputPath(path);
    }

    public static char GetOutputDelimiter<TModel>() where TModel : IModel {
      return GetConfigurationValue<TModel, char>("Output", "Delimiter");
    }

    public static string GetWorkingPath<TModel>() where TModel : IModel {
      return GetGlobalValue<TModel, string>("Working", "Path");
    }

    private static TType GetConfigurationValue<TModel, TType>(string prefix, string suffix) where TModel : IModel {
      Type type1 = typeof(TModel);
      Type type2 = typeof(Configuration);

      PropertyInfo property = type2.GetProperty(prefix + type1.Name + suffix, BindingFlags.Public | BindingFlags.Instance);

      return (TType)property.GetValue(Configuration, null);
    }

    private static TType GetGlobalValue<TModel, TType>(string prefix, string suffix) where TModel : IModel {
      Type type1 = typeof(TModel);
      Type type2 = typeof(Global);

      PropertyInfo property = type2.GetProperty(prefix + type1.Name + suffix, BindingFlags.Public | BindingFlags.Static);

      return (TType)property.GetValue(null, null);
    }

    public static string GetEstimationPath(string file) {
      return GetSubpath(file, Configuration.EstimationSubpath);
    }

    public static string GetWorkingPath(string file) {
      return
          GetSubpath(file, string.IsNullOrEmpty(Configuration.WorkingSubpath)
              ? Configuration.WorkingDirectory
              : Configuration.WorkingSubpath);
    }

    public static void InitializeNodeIndex() {
      DomainModels.Persisters.IPersisterReader<IParcelNode> reader =
                Kernel
                    .Get<IPersistenceFactory<IParcelNode>>()
                    .Reader;

      NodeIndex = new Dictionary<int, int>();

      foreach (IParcelNode node in reader) {
        NodeIndex.Add(node.Id, node.NodeId);
      }

      NodeNodePreviousOriginParcelId = new int[ParallelUtility.NBatches]; //Constants.DEFAULT_VALUE;
      NodeNodePreviousDestinationParcelId = new int[ParallelUtility.NBatches]; // = Constants.DEFAULT_VALUE;
      NodeNodePreviousDistance = new double[ParallelUtility.NBatches]; // = Constants.DEFAULT_VALUE;

      for (int i = 0; i < ParallelUtility.NBatches; i++) {
        NodeNodePreviousOriginParcelId[i] = Constants.DEFAULT_VALUE;
        NodeNodePreviousDestinationParcelId[i] = Constants.DEFAULT_VALUE;
        NodeNodePreviousDistance[i] = Constants.DEFAULT_VALUE;
      }
    }

  }
}