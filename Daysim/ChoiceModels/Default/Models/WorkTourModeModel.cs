// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Linq;
using DaySim.Framework.ChoiceModels;
using DaySim.Framework.Coefficients;
using DaySim.Framework.Core;
using DaySim.Framework.DomainModels.Creators;
using DaySim.Framework.DomainModels.Wrappers;
using DaySim.Framework.Factories;
using DaySim.Framework.Roster;
using DaySim.PathTypeModels;
using Ninject;

namespace DaySim.ChoiceModels.Default.Models {
  public class WorkTourModeModel : ChoiceModel {
    private const string CHOICE_MODEL_NAME = "WorkTourModeModel";
    private const int TOTAL_NESTED_ALTERNATIVES = 4;
    private const int TOTAL_LEVELS = 2;
    private const int MAX_PARAMETER = 199;
    private const int THETA_PARAMETER = 99;

    private readonly int[] _nestedAlternativeIds = new[] { 0, 19, 19, 20, 21, 21, 22, 22, 0 };
    private readonly int[] _nestedAlternativeIndexes = new[] { 0, 0, 0, 1, 2, 2, 3, 3, 0 };

    private readonly ITourCreator _creator =
        Global
        .Kernel
        .Get<IWrapperFactory<ITourCreator>>()
        .Creator;

    public override void RunInitialize(ICoefficientsReader reader = null) {
      Initialize(CHOICE_MODEL_NAME, Global.Configuration.WorkTourModeModelCoefficients, Global.Settings.Modes.TotalModes, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
    }

    public void Run(ITourWrapper tour) {
      if (tour == null) {
        throw new ArgumentNullException("tour");
      }

      tour.PersonDay.ResetRandom(40 + tour.Sequence - 1);

      if (Global.Configuration.IsInEstimationMode) {
        if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
          return;
        }
      }

      ChoiceProbabilityCalculator choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(tour.Id);

      if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
        if (tour.DestinationParcel == null || tour.Mode <= Global.Settings.Modes.None || tour.Mode > Global.Settings.Modes.ParkAndRide) {
          return;
        }

        IEnumerable<dynamic> pathTypeModels =
            PathTypeModelFactory.Model.RunAllPlusParkAndRide(
            tour.Household.RandomUtility,
                tour.OriginParcel,
                tour.DestinationParcel,
                tour.DestinationArrivalTime,
                tour.DestinationDepartureTime,
                tour.DestinationPurpose,
                tour.CostCoefficient,
                tour.TimeCoefficient,
                tour.Person.IsDrivingAge,
                tour.Household.VehiclesAvailable,
                tour.Person.GetTransitFareDiscountFraction(),
                false);

        dynamic pathTypeModel = pathTypeModels.First(x => x.Mode == tour.Mode);

        if (!pathTypeModel.Available) {
          return;
        }

        RunModel(choiceProbabilityCalculator, tour, pathTypeModels, tour.DestinationParcel, tour.Household.VehiclesAvailable, tour.Mode);

        choiceProbabilityCalculator.WriteObservation();
      } else {
        IEnumerable<dynamic> pathTypeModels =
            PathTypeModelFactory.Model.RunAllPlusParkAndRide(
            tour.Household.RandomUtility,
                tour.OriginParcel,
                tour.DestinationParcel,
                tour.DestinationArrivalTime,
                tour.DestinationDepartureTime,
                tour.DestinationPurpose,
                tour.CostCoefficient,
                tour.TimeCoefficient,
                tour.Person.IsDrivingAge,
                tour.Household.VehiclesAvailable,
                tour.Person.GetTransitFareDiscountFraction(),
                false);

        RunModel(choiceProbabilityCalculator, tour, pathTypeModels, tour.DestinationParcel, tour.Household.VehiclesAvailable);

        ChoiceProbabilityCalculator.Alternative chosenAlternative = choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);

        if (chosenAlternative == null) {
          Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", tour.PersonDay.Id);
          tour.Mode = Global.Settings.Modes.Hov3;
          tour.PersonDay.IsValid = false;
          return;
        }

        int choice = (int)chosenAlternative.Choice;

        tour.Mode = choice;
        dynamic chosenPathType = pathTypeModels.First(x => x.Mode == choice);
        tour.PathType = chosenPathType.PathType;
        tour.ParkAndRideNodeId = choice == Global.Settings.Modes.ParkAndRide ? chosenPathType.PathParkAndRideNodeId : 0;
        if (Global.StopAreaIsEnabled && choice == Global.Settings.Modes.ParkAndRide) {
          tour.ParkAndRideOriginStopAreaKey = chosenPathType.PathOriginStopAreaKey;
          tour.ParkAndRideDestinationStopAreaKey = chosenPathType.PathDestinationStopAreaKey;
          tour.ParkAndRidePathType = chosenPathType.PathType;
          tour.ParkAndRideTransitTime = chosenPathType.PathParkAndRideTransitTime;
          tour.ParkAndRideTransitDistance = chosenPathType.PathParkAndRideTransitDistance;
          tour.ParkAndRideTransitCost = chosenPathType.PathParkAndRideTransitCost;
          tour.ParkAndRideTransitGeneralizedTime = chosenPathType.PathParkAndRideTransitGeneralizedTime;
        }
      }
    }

    public ChoiceProbabilityCalculator.Alternative RunNested(IPersonWrapper person, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int householdCars) {
      if (person == null) {
        throw new ArgumentNullException("person");
      }

      ITourWrapper tour = _creator.CreateWrapper(person, null, originParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, Global.Settings.Purposes.Work);

      return RunNested(tour, destinationParcel, householdCars, 0.0);
    }

    public ChoiceProbabilityCalculator.Alternative RunNested(IPersonDayWrapper personDay, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int householdCars) {
      if (personDay == null) {
        throw new ArgumentNullException("personDay");
      }

      ITourWrapper tour = _creator.CreateWrapper(personDay.Person, personDay, originParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, Global.Settings.Purposes.Work);

      return RunNested(tour, destinationParcel, householdCars, personDay.Person.GetTransitFareDiscountFraction());
    }

    public ChoiceProbabilityCalculator.Alternative RunNested(ITourWrapper tour, IParcelWrapper destinationParcel, int householdCars, double transitDiscountFraction) {
      ChoiceProbabilityCalculator choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetNestedChoiceProbabilityCalculator();

      IEnumerable<dynamic> pathTypeModels =
          PathTypeModelFactory.Model.RunAll(
          tour.Household.RandomUtility,
              tour.OriginParcel,
              destinationParcel,
              tour.DestinationArrivalTime,
              tour.DestinationDepartureTime,
              tour.DestinationPurpose,
              tour.CostCoefficient,
              tour.TimeCoefficient,
              tour.Person.IsDrivingAge,
              householdCars,
              transitDiscountFraction,
              false);

      RunModel(choiceProbabilityCalculator, tour, pathTypeModels, destinationParcel, householdCars);

      return choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);
    }

    private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITourWrapper tour, IEnumerable<dynamic> pathTypeModels, IParcelWrapper destinationParcel, int householdCars, int choice = Constants.DEFAULT_VALUE) {
      IHouseholdWrapper household = tour.Household;
      Framework.DomainModels.Models.IHouseholdTotals householdTotals = household.HouseholdTotals;
      IPersonDayWrapper personDay = tour.PersonDay;
      IPersonWrapper person = tour.Person;

      // household inputs
      int childrenUnder5 = householdTotals.ChildrenUnder5;
      int childrenAge5Through15 = householdTotals.ChildrenAge5Through15;
      //			var nonworkingAdults = householdTotals.NonworkingAdults;
      //			var retiredAdults = householdTotals.RetiredAdults;
      int onePersonHouseholdFlag = household.IsOnePersonHousehold.ToFlag();
      int twoPersonHouseholdFlag = household.IsTwoPersonHousehold.ToFlag();
      int noCarsInHouseholdFlag = household.GetFlagForNoCarsInHousehold(householdCars);
      int carsLessThanDriversFlag = household.GetFlagForCarsLessThanDrivers(householdCars);
      int carsLessThanWorkersFlag = household.GetFlagForCarsLessThanWorkers(householdCars);
      int income0To25KFlag = household.Has0To25KIncome.ToFlag();

      // person inputs
      int maleFlag = person.IsMale.ToFlag();
      int ageBetween51And98Flag = person.AgeIsBetween51And98.ToFlag();

      IParcelWrapper originParcel = tour.OriginParcel;
      int parkingDuration = ChoiceModelUtility.GetParkingDuration(person.IsFulltimeWorker);
      // parking at work is free if no paid parking at work and tour goes to usual workplace
      double destinationParkingCost = (Global.Configuration.ShouldRunPayToParkAtWorkplaceModel && tour.Person.UsualWorkParcel != null
                                          && destinationParcel == tour.Person.UsualWorkParcel && person.PaidParkingAtWorkplace == 0) ? 0.0 : destinationParcel.ParkingCostBuffer1(parkingDuration);

      ChoiceModelUtility.SetEscortPercentages(personDay, out double escortPercentage, out double nonEscortPercentage);

      //			var timeWindow = (originParcel == tour.Household.ResidenceParcel) ? personDay.TimeWindow : tour.ParentTour.TimeWindow;
      //			var longestWindow = timeWindow.MaxAvailableMinutesAfter(1);
      //			var totalWindow = timeWindow.TotalAvailableMinutesAfter(1);
      //			var expectedDurationCurrentTour = person.IsFulltimeWorker ? Global.Settings.Times.EightHours : Global.Settings.Times.FourHours;
      //			var expectedDurationOtherTours = (personDay.TotalTours - personDay.TotalSimulatedTours) * Global.Settings.Times.TwoHours;
      //			var expectedDurationStops = (Math.Min(personDay.TotalStops,1) - Math.Min(personDay.TotalSimulatedStops,1)) * Global.Settings.Times.OneHour;
      //			var totalExpectedDuration = expectedDurationCurrentTour + expectedDurationOtherTours + expectedDurationStops;

      foreach (dynamic pathTypeModel in pathTypeModels) {
        dynamic mode = pathTypeModel.Mode;
        dynamic generalizedTime = pathTypeModel.GeneralizedTimeLogsum;
        //				var travelTime = pathTypeModel.PathTime;
        //				var travelCost = pathTypeModel.PathCost;

        dynamic available = pathTypeModel.Available; //&& (travelTime < longestWindow);

        dynamic alternative = choiceProbabilityCalculator.GetAlternative(mode, available, choice == mode);
        alternative.Choice = mode;

        alternative.AddNestedAlternative(_nestedAlternativeIds[pathTypeModel.Mode], _nestedAlternativeIndexes[pathTypeModel.Mode], THETA_PARAMETER);

        //				if (mode == Global.Settings.Modes.ParkAndRide) {
        //					Console.WriteLine("Park and ride logsum = {0}", generalizedTimeLogsum);
        //				}

        if (!available) {
          continue;
        }

        alternative.AddUtilityTerm(2, generalizedTime * tour.TimeCoefficient);
        //				alternative.AddUtility(3, Math.Log(1.0 - travelTime / longestWindow));
        //				alternative.AddUtility(4, travelTime < longestWindow - expectedDurationCurrentTour ? Math.Log(1.0 - travelTime / (longestWindow - expectedDurationCurrentTour)) : 0); 
        //				alternative.AddUtility(5, travelTime < longestWindow - expectedDurationCurrentTour ? 0 : 1); 
        //				alternative.AddUtility(6, travelTime < totalWindow - totalExpectedDuration ? Math.Log(1.0 - travelTime / (totalWindow - totalExpectedDuration)) : 0); 
        //				alternative.AddUtility(7, travelTime < totalWindow - totalExpectedDuration ? 0 : 1); 
        //				var vot = tour.TimeCoefficient / tour.CostCoefficient; 

        if (mode == Global.Settings.Modes.ParkAndRide) {
          alternative.AddUtilityTerm(10, 1);
          alternative.AddUtilityTerm(11, noCarsInHouseholdFlag);
          alternative.AddUtilityTerm(13, carsLessThanWorkersFlag);
          //						alternative.AddUtility(129, destinationParcel.MixedUse2Index1());
          alternative.AddUtilityTerm(130, Math.Log(destinationParcel.TotalEmploymentDensity1() + 1) * 2553.0 / Math.Log(2553.0));
          alternative.AddUtilityTerm(128, destinationParcel.TotalEmploymentDensity1());
          alternative.AddUtilityTerm(127, destinationParcel.NetIntersectionDensity1());
          //						alternative.AddUtility(123, Math.Log(destinationParcel.StopsTransitBuffer1+1));
        } else if (mode == Global.Settings.Modes.Transit) {
          alternative.AddUtilityTerm(20, 1);
          //						alternative.AddUtility(129, destinationParcel.MixedUse2Index1());
          alternative.AddUtilityTerm(130, Math.Log(destinationParcel.TotalEmploymentDensity1() + 1) * 2553.0 / Math.Log(2553.0));
          alternative.AddUtilityTerm(128, destinationParcel.TotalEmploymentDensity1());
          alternative.AddUtilityTerm(127, destinationParcel.NetIntersectionDensity1());
          //						alternative.AddUtility(126, originParcel.NetIntersectionDensity1());
          //						alternative.AddUtility(125, originParcel.HouseholdDensity1());
          alternative.AddUtilityTerm(124, originParcel.MixedUse2Index1());
          //						alternative.AddUtility(123, Math.Log(destinationParcel.StopsTransitBuffer1+1));
          //						alternative.AddUtility(122, Math.Log(originParcel.StopsTransitBuffer1+1));
        } else if (mode == Global.Settings.Modes.Hov3) {
          alternative.AddUtilityTerm(1, (destinationParkingCost * tour.CostCoefficient / Global.Configuration.Coefficients_HOV3CostDivisor_Work));
          alternative.AddUtilityTerm(30, 1);
          alternative.AddUtilityTerm(31, childrenUnder5);
          alternative.AddUtilityTerm(32, childrenAge5Through15);
          //						alternative.AddUtility(34, nonworkingAdults + retiredAdults);
          alternative.AddUtilityTerm(35, ((double)pathTypeModel.PathDistance).AlmostEquals(0) ? 0 : Math.Log(pathTypeModel.PathDistance));
          alternative.AddUtilityTerm(38, onePersonHouseholdFlag);
          alternative.AddUtilityTerm(39, twoPersonHouseholdFlag);
          alternative.AddUtilityTerm(41, noCarsInHouseholdFlag);
          alternative.AddUtilityTerm(42, carsLessThanDriversFlag);
          alternative.AddUtilityTerm(133, escortPercentage);
          alternative.AddUtilityTerm(134, nonEscortPercentage);
        } else if (mode == Global.Settings.Modes.Hov2) {
          alternative.AddUtilityTerm(1, (destinationParkingCost * tour.CostCoefficient / Global.Configuration.Coefficients_HOV2CostDivisor_Work));
          alternative.AddUtilityTerm(31, childrenUnder5);
          alternative.AddUtilityTerm(32, childrenAge5Through15);
          //						alternative.AddUtility(34, nonworkingAdults + retiredAdults);
          alternative.AddUtilityTerm(35, ((double)pathTypeModel.PathDistance).AlmostEquals(0) ? 0 : Math.Log(pathTypeModel.PathDistance));
          alternative.AddUtilityTerm(40, 1);
          alternative.AddUtilityTerm(41, noCarsInHouseholdFlag);
          alternative.AddUtilityTerm(42, carsLessThanDriversFlag);
          alternative.AddUtilityTerm(48, onePersonHouseholdFlag);
          alternative.AddUtilityTerm(133, escortPercentage);
          alternative.AddUtilityTerm(134, nonEscortPercentage);
        } else if (mode == Global.Settings.Modes.Sov) {
          alternative.AddUtilityTerm(1, (destinationParkingCost) * tour.CostCoefficient);
          alternative.AddUtilityTerm(50, 1);
          alternative.AddUtilityTerm(53, carsLessThanWorkersFlag);
          alternative.AddUtilityTerm(54, income0To25KFlag);
          alternative.AddUtilityTerm(131, escortPercentage);
          alternative.AddUtilityTerm(132, nonEscortPercentage);
        } else if (mode == Global.Settings.Modes.Bike) {
          double class1Dist
              = Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions
                  ? ImpedanceRoster.GetValue("class1distance", mode, Global.Settings.PathTypes.FullNetwork,
                      Global.Settings.ValueOfTimes.DefaultVot, tour.DestinationArrivalTime, originParcel, destinationParcel).Variable
                  : 0;

          double class2Dist =
              Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions
                  ? ImpedanceRoster.GetValue("class2distance", mode, Global.Settings.PathTypes.FullNetwork,
                      Global.Settings.ValueOfTimes.DefaultVot, tour.DestinationArrivalTime, originParcel, destinationParcel).Variable
                  : 0;

          //                  double worstDist = Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions ?
          //						 ImpedanceRoster.GetValue("worstdistance", mode, Global.Settings.PathTypes.FullNetwork, 
          //							Global.Settings.VotGroups.Medium, tour.DestinationArrivalTime,originParcel, destinationParcel).Variable : 0;

          alternative.AddUtilityTerm(60, 1);
          alternative.AddUtilityTerm(61, maleFlag);
          alternative.AddUtilityTerm(63, ageBetween51And98Flag);
          alternative.AddUtilityTerm(169, destinationParcel.MixedUse4Index1());
          alternative.AddUtilityTerm(168, destinationParcel.TotalEmploymentDensity1());
          //						alternative.AddUtility(167, destinationParcel.NetIntersectionDensity1());
          //						alternative.AddUtility(166, originParcel.NetIntersectionDensity1());
          //						alternative.AddUtility(165, originParcel.HouseholdDensity1());
          alternative.AddUtilityTerm(164, originParcel.MixedUse4Index1());
          alternative.AddUtilityTerm(162, (class1Dist > 0).ToFlag());
          alternative.AddUtilityTerm(162, (class2Dist > 0).ToFlag());
          //						alternative.AddUtility(163, (worstDist > 0).ToFlag());
        } else if (mode == Global.Settings.Modes.Walk) {
          alternative.AddUtilityTerm(71, maleFlag);
          //						alternative.AddUtility(73, ageBetween51And98Flag);
          alternative.AddUtilityTerm(179, destinationParcel.MixedUse4Index1());
          //						alternative.AddUtility(178, destinationParcel.TotalEmploymentDensity1());
          //						alternative.AddUtility(177, destinationParcel.NetIntersectionDensity1());
          //						alternative.AddUtility(176, originParcel.NetIntersectionDensity1());
          //						alternative.AddUtility(175, originParcel.HouseholdDensity1());
          alternative.AddUtilityTerm(179, originParcel.MixedUse4Index1());
        }
      }
    }
  }
}