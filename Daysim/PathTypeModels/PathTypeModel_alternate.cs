﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Linq;
using Daysim.ChoiceModels;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Wrappers;
using Daysim.Framework.Exceptions;
using Daysim.Framework.Roster;

namespace Daysim.PathTypeModels {
	public class PathTypeModel_alternate : IPathTypeModel {
		private const double MAX_UTILITY = 80D;
		private const double MIN_UTILITY = -80D;

		private IParcelWrapper _originParcel;
		private IParcelWrapper _destinationParcel;
		private int _originZoneId;
		private int _destinationZoneId;
		private int _outboundTime;
		private int _returnTime;
		private int _purpose;
		private double _tourCostCoefficient;
		private double _tourTimeCoefficient;
		private bool _isDrivingAge;
		private int _householdCars;
		private double _transitDiscountFraction;
		private bool _randomChoice;
		private int _choice;

		// model variables
		private readonly double[] _utility = new double[Global.Settings.PathTypes.TotalPathTypes];
		private readonly double[] _expUtility = new double[Global.Settings.PathTypes.TotalPathTypes];
		private readonly double[] _pathTime = new double[Global.Settings.PathTypes.TotalPathTypes];
		private readonly double[] _pathDistance = new double[Global.Settings.PathTypes.TotalPathTypes];
		private readonly double[] _pathCost = new double[Global.Settings.PathTypes.TotalPathTypes];
		private readonly int[] _pathParkAndRideNodeId = new int[Global.Settings.PathTypes.TotalPathTypes];

		public PathTypeModel_alternate() {
			GeneralizedTimeLogsum = Global.Settings.GeneralizedTimeUnavailable;
			GeneralizedTimeChosen = Global.Settings.GeneralizedTimeUnavailable;
		}

		public virtual int Mode { get; set; }

		public virtual double GeneralizedTimeLogsum { get; protected set; }

		public virtual double GeneralizedTimeChosen { get; protected set; }

		public virtual double PathTime { get; protected set; }

		public virtual double PathDistance { get; protected set; }

		public virtual double PathCost { get; protected set; }

		public virtual int PathType { get; protected set; }

		public virtual int PathParkAndRideNodeId { get; protected set; }

		public virtual bool Available { get; protected set; }

		public virtual List<IPathTypeModel> RunAllPlusParkAndRide(IRandomUtility randomUtility, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice) {
			var modes = new List<int>();

			for (var mode = Global.Settings.Modes.Walk; mode <= Global.Settings.Modes.ParkAndRide; mode++) {
				modes.Add(mode);
			}

			return Run(randomUtility, originParcel, destinationParcel, outboundTime, returnTime, purpose, tourCostCoefficient, tourTimeCoefficient, isDrivingAge, householdCars, transitDiscountFraction, randomChoice, modes.ToArray());
		}

		public virtual List<IPathTypeModel> RunAll(IRandomUtility randomUtility, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice) {
			var modes = new List<int>();

			for (var mode = Global.Settings.Modes.Walk; mode <= Global.Settings.Modes.Transit; mode++) {
				modes.Add(mode);
			}

			return Run(randomUtility, originParcel, destinationParcel, outboundTime, returnTime, purpose, tourCostCoefficient, tourTimeCoefficient, isDrivingAge, householdCars, transitDiscountFraction, randomChoice, modes.ToArray());
		}

		public virtual List<IPathTypeModel> Run(IRandomUtility randomUtility, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice, params int[] modes) {
			var list = new List<IPathTypeModel>();

			foreach (int mode in modes) {
				var pathTypeModel = new PathTypeModel_alternate { _originParcel = originParcel, _destinationParcel = destinationParcel, _outboundTime = outboundTime, _returnTime = returnTime, _purpose = purpose, _tourCostCoefficient = tourCostCoefficient, _tourTimeCoefficient = tourTimeCoefficient, _isDrivingAge = isDrivingAge, _householdCars = householdCars, _transitDiscountFraction = transitDiscountFraction, _randomChoice = randomChoice, Mode = mode };
				pathTypeModel.RunModel(randomUtility);

				list.Add(pathTypeModel);
			}

			return list;
		}

		public virtual List<IPathTypeModel> Run(IRandomUtility randomUtility, int originZoneId, int destinationZoneId, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice, params int[] modes) {
			var list = new List<IPathTypeModel>();

			foreach (var pathTypeModel in modes.Select(mode => new PathTypeModel_alternate { _originZoneId = originZoneId, _destinationZoneId = destinationZoneId, _outboundTime = outboundTime, _returnTime = returnTime, _purpose = purpose, _tourCostCoefficient = tourCostCoefficient, _tourTimeCoefficient = tourTimeCoefficient, _isDrivingAge = isDrivingAge, _householdCars = householdCars, _transitDiscountFraction = transitDiscountFraction, _randomChoice = randomChoice, Mode = mode })) {
				pathTypeModel.RunModel(randomUtility, true);

				list.Add(pathTypeModel);
			}

			return list;
		}

		private void RunModel(IRandomUtility randomUtility, bool useZones = false) {
			if (Mode == Global.Settings.Modes.Hov2) {
				_tourCostCoefficient
					= _tourCostCoefficient /
					  (_purpose == Global.Settings.Purposes.Work
						  ? Global.Configuration.Coefficients_HOV2CostDivisor_Work
						  : Global.Configuration.Coefficients_HOV2CostDivisor_Other);
			}
			else if (Mode == Global.Settings.Modes.Hov3) {
				_tourCostCoefficient
					= _tourCostCoefficient /
					  (_purpose == Global.Settings.Purposes.Work
						  ? Global.Configuration.Coefficients_HOV3CostDivisor_Work
						  : Global.Configuration.Coefficients_HOV3CostDivisor_Other);
			}

			//			 some test code 
			//			 for zone 1063 to 680 in Base_2_SKM_NM.MTX.TXT  - there is no record - zone 680 is not connected, so it shouldn't be a sampled destination 
			//			_originParcel = ChoiceModelRunner.Parcels[478318];
			//			_destinationParcel = ChoiceModelRunner.Parcels[81523];
			//			_outboundTime = 300;
			//			_returnTime = 1020;

			var votValue = (60.0 * _tourTimeCoefficient) / _tourCostCoefficient; // in $/hour

			//ACTUM Begin
			if (useZones == false && (Global.Configuration.PathImpedance_UtilityForm_Auto == 1
				|| Global.Configuration.PathImpedance_UtilityForm_Transit == 1)) {
				if (_purpose == Global.Settings.Purposes.Work || _purpose == Global.Settings.Purposes.School || _purpose == Global.Settings.Purposes.Escort) {
					votValue = Global.Configuration.VotMediumHigh - 0.5;
				}
				else if (_purpose == Global.Settings.Purposes.Business) {
					votValue = Global.Configuration.VotHighVeryHigh - 0.5;
				}
				else votValue = Global.Configuration.VotLowMedium - 0.5;
			}
			//ACTUM End

			var skimMode = (Mode == Global.Settings.Modes.ParkAndRide) ? Global.Settings.Modes.Transit : Mode;
			var availablePathTypes = 0;
			var expUtilitySum = 0D;
			var bestExpUtility = 0D;
			var bestPathType = Constants.DEFAULT_VALUE;
			int batchNumber = ParallelUtility.GetBatchFromThreadId();

			// loop on all relevant path types for the mode
			for (var pathType = Global.Settings.PathTypes.FullNetwork; pathType < Global.Settings.PathTypes.TotalPathTypes; pathType++) {
				_utility[pathType] = 0D;

				if (!ImpedanceRoster.IsActualCombination(skimMode, pathType)) {
					continue;
				}

				// set path type utility and impedance, depending on the mode
				if (Mode == Global.Settings.Modes.Bike || Mode == Global.Settings.Modes.Walk) {
					RunWalkBikeModel(skimMode, pathType, votValue, useZones, batchNumber);
				}
				else if (Mode == Global.Settings.Modes.Hov3 || Mode == Global.Settings.Modes.Hov2 || Mode == Global.Settings.Modes.Sov) {
					if (Mode != Global.Settings.Modes.Sov || (_isDrivingAge && _householdCars > 0)) {
						RunAutoModel(skimMode, pathType, votValue, useZones);
					}
				}
				else if (Mode == Global.Settings.Modes.Transit) {
					if (Global.StopAreaIsEnabled) {
						RunStopAreaWalkTransitModel(skimMode, pathType, votValue, useZones);
					}
					else {
						RunSimpleWalkTransitModel(skimMode, pathType, votValue, useZones);
					}
				}
				else if (Mode == Global.Settings.Modes.ParkAndRide) {
					RunSimpleParkAndRideModel(skimMode, pathType, votValue, useZones);
				}

				if (_expUtility[pathType] < Constants.EPSILON) {
					continue;
				}

				// add to total utility and see if it is the best so far
				availablePathTypes++;
				expUtilitySum += _expUtility[pathType];

				if (_expUtility[pathType] <= bestExpUtility) {
					continue;
				}

				// make the best current path type and utility
				bestPathType = pathType;
				bestExpUtility = _expUtility[pathType];
			}

			if (expUtilitySum < Constants.EPSILON) {
				Available = false;

				return;
			}

			// set the generalized time logsum
			var logsum = Math.Log(expUtilitySum);
			var tourTimeCoefficient = (Global.Configuration.PathImpedance_PathChoiceScaleFactor * _tourTimeCoefficient);

			if (Double.IsNaN(expUtilitySum) || Double.IsNaN(logsum) || Double.IsNaN(tourTimeCoefficient)) {
				throw new ValueIsNaNException(string.Format("Value is NaN for utilitySum: {0}, logsum: {1}, tourTimeCoefficient: {2}.", expUtilitySum, logsum, tourTimeCoefficient));
			}

			GeneralizedTimeLogsum = logsum / tourTimeCoefficient; // need to make sure _tourTimeCoefficient is not 0

			if (Double.IsNaN(GeneralizedTimeLogsum)) {
				throw new ValueIsNaNException(string.Format("Value is NaN for GeneralizedTimeLogsum where utilitySum: {0}, logsum: {1}, tourTimeCoefficient: {2}.", expUtilitySum, logsum, tourTimeCoefficient));
			}

			// draw a choice using a random number if requested (and in application mode), otherwise return best utility
			if (_randomChoice && availablePathTypes > 1 && !Global.Configuration.IsInEstimationMode) {
				var random = randomUtility.Uniform01();

				for (var pathType = Global.Settings.PathTypes.FullNetwork; pathType <= Global.Settings.PathTypes.TotalPathTypes; pathType++) {
					_choice = pathType;
					random -= _expUtility[pathType] / expUtilitySum;

					if (random < 0) {
						break;
					}
				}
			}
			else {
				_choice = bestPathType;
			}

			Available = true;
			PathType = _choice;
			PathTime = _pathTime[_choice];
			PathDistance = _pathDistance[_choice];
			PathCost = _pathCost[_choice];
			GeneralizedTimeChosen = _utility[_choice] / tourTimeCoefficient;
			PathParkAndRideNodeId = _pathParkAndRideNodeId[_choice];
		}


		private void RunWalkBikeModel(int skimMode, int pathType, double votValue, bool useZones, int batch) {
			var zzDist = ImpedanceRoster.GetValue("distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable;
			var circuityDistance =
				(zzDist > Global.Configuration.MaximumBlendingDistance)
					? Constants.DEFAULT_VALUE
					: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
						? _originParcel.NodeToNodeDistance(_destinationParcel, batch)
						: (!useZones && Global.Configuration.UseShortDistanceCircuityMeasures)
							? _originParcel.CircuityDistance(_destinationParcel)
							: Constants.DEFAULT_VALUE;
			//test output
			//var orth=(Math.Abs(_originParcel.XCoordinate - _destinationParcel.XCoordinate) + Math.Abs(_originParcel.YCoordinate - _destinationParcel.YCoordinate)) / 5280.0;
			//Global.PrintFile.WriteLine("Circuity distance for parcels {0} to {1} is {2} vs {3}",_originParcel.Id, _destinationParcel.Id, circuityDistance, orth);

			var skimValue =
				useZones
					? ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
					: ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);

			_pathTime[pathType] = skimValue.Variable;
			_pathDistance[pathType] = skimValue.BlendVariable;
			_pathCost[pathType] = 0;
			_pathParkAndRideNodeId[pathType] = 0;

			if (_returnTime > 0) {

				skimValue =
					useZones
						? ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId)
						: ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel, circuityDistance);

				_pathTime[pathType] += skimValue.Variable;
				_pathDistance[pathType] += skimValue.BlendVariable;
			}

			// sacog-specific adjustment of generalized time for bike mode
			if (_pathDistance[pathType] > Constants.EPSILON && skimMode == Global.Settings.Modes.Bike && Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions) {
				var d1 =
					Math.Abs(Global.Configuration.PathImpedance_BikeType1DistanceFractionAdditiveWeight) < Constants.EPSILON
						? 0D
						: useZones
							  ? ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
							  : ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var d2 =
					Math.Abs(Global.Configuration.PathImpedance_BikeType2DistanceFractionAdditiveWeight) < Constants.EPSILON
						? 0D
						: useZones
							  ? ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
							  : ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var d3 =
					Math.Abs(Global.Configuration.PathImpedance_BikeType3DistanceFractionAdditiveWeight) < Constants.EPSILON
						? 0D
						: useZones
							  ? ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
							  : ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var d4 = Math.Abs(Global.Configuration.PathImpedance_BikeType4DistanceFractionAdditiveWeight) < Constants.EPSILON
								? 0D
								: useZones
									  ? ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
									  : ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				if (_returnTime > 0) {
					d1 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType1DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					d2 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType2DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					d3 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType3DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					d4 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType4DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;
				}

				var adjFactor =
					1.0
					+ d1 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType1DistanceFractionAdditiveWeight
					+ d2 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType2DistanceFractionAdditiveWeight
					+ d3 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType3DistanceFractionAdditiveWeight
					+ d4 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType4DistanceFractionAdditiveWeight;

				_pathTime[pathType] *= adjFactor;
			}

			// a fix for unconnected parcels/zones (sampling should be fixed to not sample them)
			//			if (_pathTime[pathType] < Constants.EPSILON && _pathDistance[pathType] >= Constants.EPSILON ) {
			//				_pathTime[pathType] = _pathDistance[pathType] * (skimMode == Global.Settings.Modes.Walk ? 20.0 : 6.0) ; 
			//			}

			// a fix for intra-parcels, which happen once in a great while for school
			if (!useZones && _originParcel.Id == _destinationParcel.Id && skimMode == Global.Settings.Modes.Walk
				//JLB 20130628 added destination scale condition because ImpedanceRoster assigns time and cost values for intrazonals 
				&& Global.Settings.DestinationScale != Global.Settings.DestinationScales.Zone) {
				_pathTime[pathType] = 1.0;
				_pathDistance[pathType] = 0.01 * Global.Settings.DistanceUnitsPerMile;  // JLBscale.  multiplied by distance units per mile
			}



			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);

			if (_pathTime[pathType] > pathTimeLimit || _pathTime[pathType] < Constants.EPSILON) {
				return;
			}

			_utility[pathType] =
				Global.Configuration.PathImpedance_PathChoiceScaleFactor *
				(_tourTimeCoefficient * _pathTime[pathType]
				 * (skimMode == Global.Settings.Modes.Walk
						 ? Global.Configuration.PathImpedance_WalkTimeWeight
						 : Global.Configuration.PathImpedance_BikeTimeWeight));

			_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
		}

		private void RunAutoModel(int skimMode, int pathType, double votValue, bool useZones) {
			_pathCost[pathType] =
				useZones
					? ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
					: ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

			if (_returnTime > 0) {
				_pathCost[pathType] +=
					useZones
						? ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
						: ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;
			}

			//if full network path and no-tolls path exists check for duplicate
			var tollConstant = 0D;
			if (pathType == Global.Settings.PathTypes.FullNetwork && ImpedanceRoster.IsActualCombination(skimMode, Global.Settings.PathTypes.NoTolls)) {
				var noTollCost =
					useZones
						? ImpedanceRoster.GetValue("toll", skimMode, Global.Settings.PathTypes.NoTolls, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("toll", skimMode, Global.Settings.PathTypes.NoTolls, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				if (_returnTime > 0) {
					noTollCost +=
						useZones
							? ImpedanceRoster.GetValue("toll", skimMode, Global.Settings.PathTypes.NoTolls, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("toll", skimMode, Global.Settings.PathTypes.NoTolls, votValue, _returnTime, _destinationParcel, _originParcel).Variable;
				}
				// if the toll route doesn't have a higher cost than no toll route, than make it unavailable
				if (_pathCost[pathType] - noTollCost < Constants.EPSILON) {
					return;
				}
				// else it is a toll route with a higher cost than no toll route, add a toll constant also
				tollConstant = Global.Configuration.PathImpedance_AutoTolledPathConstant;
			}

			var zzDist = ImpedanceRoster.GetValue("distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable;
			int batchNumber = ParallelUtility.GetBatchFromThreadId();
			var circuityDistance =
				(zzDist > Global.Configuration.MaximumBlendingDistance)
					? Constants.DEFAULT_VALUE
					: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
						? _originParcel.NodeToNodeDistance(_destinationParcel, batchNumber)
						: (!useZones && Global.Configuration.UseShortDistanceCircuityMeasures)
							? _originParcel.CircuityDistance(_destinationParcel)
							: Constants.DEFAULT_VALUE;

			var skimValue =
				useZones
					? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
					: ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);

			_pathParkAndRideNodeId[pathType] = 0;
			_pathTime[pathType] = skimValue.Variable;
			_pathDistance[pathType] = skimValue.BlendVariable;

			//implement mileage-based pricing policy
			if (Global.Configuration.Policy_TestMilageBasedPricing) {
				var minutesAfterMidnight = _outboundTime + 180;
				var centsPerMile = (minutesAfterMidnight >= Global.Configuration.Policy_AMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_AMPricingPeriodEnd)
					 ? Global.Configuration.Policy_CentsPerMileInAMPeak :
						(minutesAfterMidnight >= Global.Configuration.Policy_PMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_PMPricingPeriodEnd)
						  ? Global.Configuration.Policy_CentsPerMileInPMPeak :
							 (minutesAfterMidnight > Global.Configuration.Policy_AMPricingPeriodEnd && minutesAfterMidnight < Global.Configuration.Policy_PMPricingPeriodStart)
								? Global.Configuration.Policy_CentsPerMileBetweenPeaks : Global.Configuration.Policy_CentsPerMileOutsidePeaks;
				_pathCost[pathType] += skimValue.BlendVariable * centsPerMile / 100.0;
			}
			if (_returnTime > 0) {

				skimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId)
						: ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel, circuityDistance);

				_pathTime[pathType] += skimValue.Variable;
				_pathDistance[pathType] += skimValue.BlendVariable;

				//implement mileage-based pricing policy
				if (Global.Configuration.Policy_TestMilageBasedPricing) {
					var minutesAfterMidnight = _returnTime + 180;
					var centsPerMile = (minutesAfterMidnight >= Global.Configuration.Policy_AMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_AMPricingPeriodEnd)
						 ? Global.Configuration.Policy_CentsPerMileInAMPeak :
							(minutesAfterMidnight >= Global.Configuration.Policy_PMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_PMPricingPeriodEnd)
							  ? Global.Configuration.Policy_CentsPerMileInPMPeak :
								 (minutesAfterMidnight > Global.Configuration.Policy_AMPricingPeriodEnd && minutesAfterMidnight < Global.Configuration.Policy_PMPricingPeriodStart)
									? Global.Configuration.Policy_CentsPerMileBetweenPeaks : Global.Configuration.Policy_CentsPerMileOutsidePeaks;
					_pathCost[pathType] += skimValue.BlendVariable * centsPerMile / 100.0;
				}
			}

			// a fix for unconnected parcels/zones (sampling should be fixed to not sample them in the first place)
			//			if (_pathTime[pathType] < Constants.EPSILON && _pathDistance[pathType] >= Constants.EPSILON ) {
			//				_pathTime[pathType] = _pathDistance[pathType] * 2.0 ;  // correct missing time with speed of 30 mph 
			//			}
			//			else if (_pathTime[pathType] < Constants.EPSILON && _pathDistance[pathType] < Constants.EPSILON ) {
			//				_pathDistance[pathType] = (Math.Abs(_originParcel.XCoordinate - _destinationParcel.XCoordinate) 
			//					                      + Math.Abs(_originParcel.YCoordinate - _destinationParcel.YCoordinate))/5280D;
			//				_pathTime[pathType] = _pathDistance[pathType] * 2.0 ;  // correct missing time with speed of 30 mph 
			//			}

			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);

			if (_pathTime[pathType] > pathTimeLimit || _pathTime[pathType] < Constants.EPSILON) {
				return;
			}

			_pathCost[pathType] += _pathDistance[pathType] * Global.PathImpedance_AutoOperatingCostPerDistanceUnit;

			//ACTUM Begin
			if (useZones == false && Global.Configuration.PathImpedance_UtilityForm_Auto == 1) {
				//calculate time utility
				var freeFlowSkimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
						: ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
				var freeFlowTime = freeFlowSkimValue.Variable;
				var extraSkimValue =
				useZones
					? ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
					: ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
				var extraTime = extraSkimValue.Variable;
				extraTime += _destinationParcel.ParkingOffStreetPaidHourlyPrice; //this property represents average search time per trip in Actum data
				if (_returnTime > 0) {
					freeFlowSkimValue =
						useZones
							? ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
							: ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
					freeFlowTime += freeFlowSkimValue.Variable;
					extraSkimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
						: ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
					extraTime += extraSkimValue.Variable;
				}
				var gammaFreeFlowTime = GammaFunction(freeFlowTime, Global.Configuration.PathImpedance_Gamma_InVehicleTime);

				extraTime = Global.Configuration.Policy_CongestedTravelTimeMultiplier != 0 ? extraTime * Global.Configuration.Policy_CongestedTravelTimeMultiplier : extraTime;
				var gammaExtraTime = GammaFunction(extraTime, Global.Configuration.PathImpedance_Gamma_ExtraTime);
				//determine time weight
				//extra time weight for driver and passenger
				double inVehicleExtraTimeWeight;
				if (skimMode == Global.Settings.Modes.HovPassenger) {
					inVehicleExtraTimeWeight = Global.Configuration.PathImpedance_InVehicleExtraTimeWeight_Passenger;
				}
				else {
					inVehicleExtraTimeWeight = Global.Configuration.PathImpedance_InVehicleExtraTimeWeight_Driver;
				}
				//weights for purpose x mode
				int aggregatePurpose = 0;
				double inVehicleTimeWeight;
				if (_purpose == Global.Settings.Purposes.Work || _purpose == Global.Settings.Purposes.School || _purpose == Global.Settings.Purposes.Escort) {
					aggregatePurpose = 1;
				}
				else if (_purpose == Global.Settings.Purposes.Business) {
					aggregatePurpose = 2;
				}
				if (skimMode == Global.Settings.Modes.Sov) {
					if (aggregatePurpose == 1) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_SOV;
					}
					else if (aggregatePurpose == 2) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_SOV;
					}
					else {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_SOV;
					}
				}
				else if (skimMode == Global.Settings.Modes.HovDriver) {
					if (aggregatePurpose == 1) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_HOVDriver;
					}
					else if (aggregatePurpose == 2) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_HOVDriver;
					}
					else {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_HOVDriver;
					}
				}
				else {
					if (aggregatePurpose == 1) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_HOVPassenger;
					}
					else if (aggregatePurpose == 2) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_HOVPassenger;
					}
					else {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_HOVPassenger;
					}
				}
				//calculate cost utility
				_pathCost[pathType] += _destinationParcel.ParkingOffStreetPaidDailyPrice; // this property represents avg parking cost per trip in Actum

				double gammaCost;
				double distanceCoefficient;
				double modeConstant;
				if (skimMode == Global.Settings.Modes.HovPassenger && !Global.Configuration.HOVPassengersIncurCosts) {
					gammaCost = 0;
				}
				else {
					gammaCost = GammaFunction(_pathCost[pathType], Global.Configuration.PathImpedance_Gamma_Cost);
				}
				//calculate distance utility
				//calculate utility
				_utility[pathType] = Global.Configuration.PathImpedance_PathChoiceScaleFactor
					* (_tourCostCoefficient * gammaCost
					+ _tourTimeCoefficient * inVehicleTimeWeight
					* (gammaFreeFlowTime + gammaExtraTime * inVehicleExtraTimeWeight)
					+ tollConstant);
			}
			//ACTUM End
			else {
				_utility[pathType] = Global.Configuration.PathImpedance_PathChoiceScaleFactor *
				(_tourCostCoefficient * _pathCost[pathType] +
				 _tourTimeCoefficient * _pathTime[pathType] +
				 tollConstant);
			}

			_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
		}

		private void RunSimpleWalkTransitModel(int skimMode, int pathType, double votValue, bool useZones) {

			if (!useZones) {
				// get zones associated with parcels for transit path
				_originZoneId = _originParcel.ZoneId;
				_destinationZoneId = _destinationParcel.ZoneId;
			}

			var transitPath = GetTransitPath(skimMode, pathType, votValue, _outboundTime, _returnTime, _originZoneId, _destinationZoneId);
			if (!transitPath.Available) {
				return;
			}

			//If not Actum, add walk times
			if (Global.Configuration.PathImpedance_UtilityForm_Transit != 1) {
				var originWalkTime = useZones ? 5.0 : GetTransitWalkTime(_originParcel, pathType, transitPath.Boardings1);
				var destinationWalkTime = useZones ? 5.0 : GetTransitWalkTime(_destinationParcel, pathType, transitPath.Boardings2);

				if (originWalkTime < -1 * Constants.EPSILON || destinationWalkTime < -1 * Constants.EPSILON) {
					return;
				}

				if (_returnTime > 0) {
					originWalkTime *= 2;
					destinationWalkTime *= 2;
				}

				//set final values
				_pathParkAndRideNodeId[pathType] = 0;
				_pathTime[pathType] = transitPath.Time + originWalkTime + destinationWalkTime;
				_pathCost[pathType] = transitPath.Cost;
				_utility[pathType] = transitPath.Utility +
					 Global.Configuration.PathImpedance_PathChoiceScaleFactor * _tourTimeCoefficient *
					 Global.Configuration.PathImpedance_TransitWalkAccessTimeWeight * (originWalkTime + destinationWalkTime);
			}
			else { // use as is
				//set final values
				_pathParkAndRideNodeId[pathType] = 0;
				_pathTime[pathType] = transitPath.Time;
				_pathCost[pathType] = transitPath.Cost;
				_utility[pathType] = transitPath.Utility;
			}

			_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);

			//for transit, use auto distance
			var distance = ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Hov2, Global.Settings.PathTypes.FullNetwork, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable;
			if (_returnTime > 0) {
				distance += ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Hov2, Global.Settings.PathTypes.FullNetwork, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable;
			}
			_pathDistance[pathType] = distance;
		}

		private void RunStopAreaWalkTransitModel(int skimMode, int pathType, double votValue, bool useZones) {

			if (useZones) {
				return;
			}
			
			//_originParcel. SetFirstLastStopAreaDistanceIndexes();
			var oFirst = _originParcel.FirstPositionInStopAreaDistanceArray;
			var oLast = _originParcel.LastPositionInStopAreaDistanceArray;

			//_destinationParcel. SetFirstLastStopAreaDistanceIndexes();
			var dFirst = _destinationParcel.FirstPositionInStopAreaDistanceArray;
			var dLast = _destinationParcel.LastPositionInStopAreaDistanceArray;

			if (oFirst <=0 || dFirst<=0) {
				return;
			}

			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);
			var bestPathUtility = -99999D;

			for (var oIndex = oFirst; oIndex <= oLast; oIndex++) {
  				var oStopArea = Global.ParcelStopAreaStopAreaIds[oIndex];
				var oWalkDistance = Global.ParcelStopAreaDistances[oIndex];

				for (var dIndex = dFirst; dIndex <=dLast; dIndex++) {
					var dStopArea = Global.ParcelStopAreaStopAreaIds[dIndex];
					var dWalkDistance = Global.ParcelStopAreaDistances[dIndex];
			
					var walkTime = Global.PathImpedance_WalkMinutesPerDistanceUnit * (oWalkDistance + dWalkDistance)*Global.Settings.LengthUnitsPerFoot/5280.0 * Global.Settings.DistanceUnitsPerMile;

					var transitPath = GetTransitPath(skimMode, pathType, votValue, _outboundTime, _returnTime, oStopArea, dStopArea);
					if (!transitPath.Available) {
						continue;
					}
	
					// set utility
					var fullPathTime = transitPath.Time + walkTime;
					var fullPathCost = transitPath.Cost ;
					

					if (fullPathTime > pathTimeLimit) {
						continue;
					}

					var fullPathUtility = transitPath.Utility +
					Global.Configuration.PathImpedance_PathChoiceScaleFactor *
					 _tourTimeCoefficient * Global.Configuration.PathImpedance_TransitWalkAccessTimeWeight * walkTime;


					// if the best path so far, reset pathType properties
					if (fullPathUtility <= bestPathUtility) {
						continue;
					}

					bestPathUtility = fullPathUtility;

					_pathParkAndRideNodeId[pathType] = 0;
					_pathTime[pathType] = fullPathTime;
					_pathCost[pathType] = fullPathCost;
					_utility[pathType] = fullPathUtility;
					_expUtility[pathType] = fullPathUtility > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : fullPathUtility < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(fullPathUtility);
				}
			}

			//for transit, use auto distance
			var distance = ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Hov2, Global.Settings.PathTypes.FullNetwork, votValue, _outboundTime, _originParcel , _destinationParcel).Variable;
			if (_returnTime > 0) {
				distance += ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Hov2, Global.Settings.PathTypes.FullNetwork, votValue, _returnTime, _destinationParcel, _originParcel).Variable;
			}
			_pathDistance[pathType] = distance;
		}


		private void RunSimpleParkAndRideModel(int skimMode, int pathType, double votValue, bool useZones) {
			if (ChoiceModelFactory.ParkAndRideNodeDao == null || _returnTime <= 0) {
				return;
			}
			int batchNumber = ParallelUtility.GetBatchFromThreadId();
			IEnumerable<IParkAndRideNodeWrapper> parkAndRideNodes;

			if (Global.Configuration.ShouldReadParkAndRideNodeSkim) {
				var nodeId =
					useZones
						? (int) ImpedanceRoster.GetValue("przone", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: (int) ImpedanceRoster.GetValue("przone", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var node = ChoiceModelFactory.ParkAndRideNodeDao.Get(nodeId);

				parkAndRideNodes = new List<IParkAndRideNodeWrapper> { node };
			}
			else {
				parkAndRideNodes = ChoiceModelFactory.ParkAndRideNodeDao.Nodes.Where(n => n.Capacity > 0);
			}

			// valid node(s), and tour-level call  
			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);
			var bestNodeUtility = -99999D;
			var originZoneId = useZones ? _originZoneId : _originParcel.ZoneId;
			var destinationZoneId = useZones ? _destinationZoneId : _destinationParcel.ZoneId;

			foreach (var node in parkAndRideNodes) {
				// only look at nodes with positive capacity
				if (node.Capacity < Constants.EPSILON) {
					continue;
				}

				// use the node rather than the nearest parcel for transit LOS, becuase more accurate, and distance blending is not relevant 
				var parkAndRideParcel = ChoiceModelFactory.Parcels[node.NearestParcelId];
				var parkAndRideZoneId = node.ZoneId;
				var parkAndRideParkingCost = node.Cost / 100.0; // converts hundredths of Monetary Units to Monetary Units  // JLBscale: changed comment from cents and dollars

				var transitPath = GetTransitPath(skimMode, pathType, votValue, _outboundTime, _returnTime, parkAndRideZoneId, destinationZoneId);
				if (!transitPath.Available) {
					continue;
				}

				var zzDist = ImpedanceRoster.GetValue("distance", Global.Settings.Modes.Sov, Global.Settings.PathTypes.FullNetwork, votValue, _outboundTime, originZoneId, parkAndRideZoneId).Variable;

				var circuityDistance =
					(zzDist > Global.Configuration.MaximumBlendingDistance)
						? Constants.DEFAULT_VALUE
						: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
							? _originParcel.NodeToNodeDistance(parkAndRideParcel, batchNumber)
							: (!useZones && Global.Configuration.UseShortDistanceCircuityMeasures)
								? _originParcel.CircuityDistance(parkAndRideParcel)
								: Constants.DEFAULT_VALUE;

				var skimValue
					= useZones
						  ? ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Sov, Global.Settings.PathTypes.FullNetwork, votValue, _outboundTime, _originZoneId, parkAndRideZoneId)
						  : ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Sov, Global.Settings.PathTypes.FullNetwork, votValue, _outboundTime, _originParcel, parkAndRideParcel, circuityDistance);

				var driveTime = skimValue.Variable;
				var driveDistance = skimValue.BlendVariable;
				var parkMinute = (int) (_outboundTime - (transitPath.Time / 2.0) - 3); // estimate of change mode activity time, same as assumed when setting trip departure time in ChoiceModelRunner.

				var transitDistance =
					useZones
						? ImpedanceRoster.GetValue("distance", Global.Settings.Modes.Hov2, Global.Settings.PathTypes.FullNetwork, votValue, _outboundTime, parkAndRideZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("distance", Global.Settings.Modes.Hov2, Global.Settings.PathTypes.FullNetwork, votValue, _outboundTime, parkAndRideParcel, _destinationParcel).Variable;

				var destinationWalkTime = useZones ? 5.0 : GetTransitWalkTime(_destinationParcel, pathType, transitPath.Boardings1);

				// add return LOS
				if (destinationWalkTime < -1 * Constants.EPSILON) {
					continue;
				}

				skimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Sov, Global.Settings.PathTypes.FullNetwork, votValue, _returnTime, parkAndRideZoneId, _originZoneId)
						: ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Sov, Global.Settings.PathTypes.FullNetwork, votValue, _returnTime, parkAndRideParcel, _originParcel, circuityDistance);

				driveTime += skimValue.Variable;
				driveDistance += skimValue.BlendVariable;
				transitDistance *= 2;
				destinationWalkTime *= 2;

				// set utility
				var nodePathTime = transitPath.Time + driveTime + destinationWalkTime;
				var nodePathDistance = driveDistance + transitDistance;
				var nodePathCost = transitPath.Cost + parkAndRideParkingCost;

				if (nodePathTime > pathTimeLimit) {
					continue;
				}

				var nodeUtility = transitPath.Utility +
					Global.Configuration.PathImpedance_PathChoiceScaleFactor *
					(_tourCostCoefficient * parkAndRideParkingCost +
					 _tourTimeCoefficient *
					 (Global.Configuration.PathImpedance_TransitDriveAccessTimeWeight * driveTime +
					  Global.Configuration.PathImpedance_TransitWalkAccessTimeWeight * destinationWalkTime));

				if (Global.Configuration.ShouldUseParkAndRideShadowPricing && !Global.Configuration.IsInEstimationMode) {
					nodeUtility += node.ShadowPrice[parkMinute];
				}

				// if the best path so far, reset pathType properties
				if (nodeUtility <= bestNodeUtility) {
					continue;
				}

				bestNodeUtility = nodeUtility;

				_pathParkAndRideNodeId[pathType] = node.Id;
				_pathTime[pathType] = nodePathTime;
				_pathDistance[pathType] = nodePathDistance;
				_pathCost[pathType] = nodePathCost;
				_utility[pathType] = nodeUtility;
				_expUtility[pathType] = nodeUtility > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : nodeUtility < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(nodeUtility);
			}

		}

		public class TransitPath {
			public bool Available { get; set; }
			public double Time { get; set; }
			public double Cost { get; set; }
			public double Boardings1 { get; set; }
			public double Boardings2 { get; set; }
			public double Utility { get; set; }
		}

		private PathTypeModel.TransitPath GetTransitPath(int skimMode, int pathType, double votValue, int outboundTime, int returnTime, int originZoneId, int destinationZoneId) {

			var path = new PathTypeModel.TransitPath();
			path.Available = true;

			// check for presence of valid path
			var outboundInVehicleTime = ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
			var returnInVehicleTime = returnTime > 0
					? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable
					: 0;

			if (outboundInVehicleTime < Constants.EPSILON || (returnTime > 0 && returnInVehicleTime < Constants.EPSILON)) {
				path.Available = false;
				return path;
			}
			// valid path(s).  Proceed.

			var pathTypeConstant =
					pathType == Global.Settings.PathTypes.LocalBus
						? Global.Configuration.PathImpedance_TransitLocalBusPathConstant
						: pathType == Global.Settings.PathTypes.LightRail
							  ? Global.Configuration.PathImpedance_TransitLightRailPathConstant
							  : pathType == Global.Settings.PathTypes.PremiumBus
									 ? Global.Configuration.PathImpedance_TransitPremiumBusPathConstant
									 : pathType == Global.Settings.PathTypes.CommuterRail
											? Global.Configuration.PathImpedance_TransitCommuterRailPathConstant
											: pathType == Global.Settings.PathTypes.Ferry
												  ? Global.Configuration.PathImpedance_TransitFerryPathConstant
												  : 0;

			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (returnTime > 0 ? 2 : 1);

			//ACTUM Begin
			if (Global.Configuration.PathImpedance_UtilityForm_Transit == 1) {
				//Actum:  nonlinear utility with submode-speciifc time components in a generic transit mode (labeled local bus)
				//get skim values
				var firstWaitTime = ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var transferWaitTime = ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var fare = ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var accessEgressTime = ImpedanceRoster.GetValue("accegrtime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var fTime = ImpedanceRoster.GetValue("ftime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var gTime = ImpedanceRoster.GetValue("gtime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var bTime = ImpedanceRoster.GetValue("btime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var pTime = ImpedanceRoster.GetValue("ptime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var rTime = ImpedanceRoster.GetValue("rtime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var sTime = ImpedanceRoster.GetValue("stime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var xTime = ImpedanceRoster.GetValue("xtime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var yTime = ImpedanceRoster.GetValue("ytime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var zTime = ImpedanceRoster.GetValue("ztime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;

				// add return LOS, if valid _departureTime passed			
				if (_returnTime > 0) {
					firstWaitTime += ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					transferWaitTime += ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					fare += ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					accessEgressTime += ImpedanceRoster.GetValue("accegrtime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					fTime += ImpedanceRoster.GetValue("ftime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					gTime += ImpedanceRoster.GetValue("gtime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					bTime += ImpedanceRoster.GetValue("btime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					pTime += ImpedanceRoster.GetValue("ptime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					rTime += ImpedanceRoster.GetValue("rtime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					sTime += ImpedanceRoster.GetValue("stime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					xTime += ImpedanceRoster.GetValue("xtime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					yTime += ImpedanceRoster.GetValue("ytime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					zTime += ImpedanceRoster.GetValue("ztime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
				}

				fare = fare * (1.0 - _transitDiscountFraction); //fare adjustment

				//determine utility components 
				//accessEgressTime = accessEgressTime
				var trainTime = pTime + rTime + sTime;
				var busTime = bTime + xTime + yTime + zTime;
				var metroTime = gTime;
				var lightRailTime = fTime;

				path.Time = accessEgressTime + firstWaitTime + transferWaitTime + trainTime + busTime + metroTime + lightRailTime;
				if (path.Time > pathTimeLimit) {
					path.Available = false;
					return path;
				}
				path.Cost = fare;

				//determine submode ivt time weights, and weighted IVT 
				int aggregatePurpose = 0;
				if (_purpose == Global.Settings.Purposes.Work || _purpose == Global.Settings.Purposes.School || _purpose == Global.Settings.Purposes.Escort) {
					aggregatePurpose = 1;
				}
				else if (_purpose == Global.Settings.Purposes.Business) {
					aggregatePurpose = 2;
				}

				double inVehicleTimeWeight_Train;
				double inVehicleTimeWeight_Bus;
				double inVehicleTimeWeight_Metro;
				double inVehicleTimeWeight_LightRail;

				if (aggregatePurpose == 1) {
					inVehicleTimeWeight_Train = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_Train;
					inVehicleTimeWeight_Bus = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_Bus;
					inVehicleTimeWeight_Metro = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_Metro;
					inVehicleTimeWeight_LightRail = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_LightRail;
				}
				else if (aggregatePurpose == 2) {
					inVehicleTimeWeight_Train = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_Train;
					inVehicleTimeWeight_Bus = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_Bus;
					inVehicleTimeWeight_Metro = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_Metro;
					inVehicleTimeWeight_LightRail = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_LightRail;
				}
				else {
					inVehicleTimeWeight_Train = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_Train;
					inVehicleTimeWeight_Bus = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_Bus;
					inVehicleTimeWeight_Metro = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_Metro;
					inVehicleTimeWeight_LightRail = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_LightRail;
				}
				var weightedInVehicleTime = trainTime * inVehicleTimeWeight_Train
					 + busTime * inVehicleTimeWeight_Bus
					 + metroTime * inVehicleTimeWeight_Metro
					 + lightRailTime * inVehicleTimeWeight_LightRail;

				//calculate time utility

				path.Utility =
					 Global.Configuration.PathImpedance_PathChoiceScaleFactor *
					 (_tourCostCoefficient * GammaFunction(fare, Global.Configuration.PathImpedance_Gamma_Cost)
					 + _tourTimeCoefficient *
					 (Global.Configuration.PathImpedance_TransitInVehicleTimeWeight * GammaFunction(weightedInVehicleTime, Global.Configuration.PathImpedance_Gamma_InVehicleTime)
					 + Global.Configuration.PathImpedance_TransitFirstWaitTimeWeight * firstWaitTime
					 + Global.Configuration.PathImpedance_TransitTransferWaitTimeWeight * transferWaitTime
					 + Global.Configuration.PathImpedance_TransitAccessEgressTimeWeight * accessEgressTime
				  ));
			} //ACTUM End

			else {
				// get outbound los
				var initialWaitTime = ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var transferWaitTime = ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var numberOfBoards1 = ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;
				var fare = ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable;

				double numberOfBoards2 = 0;
				// add return LOS, if valid _departureTime passed			
				if (returnTime > 0) {
					initialWaitTime += ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					transferWaitTime += ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					numberOfBoards2 = ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
					fare += ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable;
				}

				fare = fare * (1.0 - _transitDiscountFraction); //fare adjustment

				// set utility
				path.Time = outboundInVehicleTime + returnInVehicleTime + initialWaitTime + transferWaitTime;
				if (path.Time > pathTimeLimit) {
					path.Available = false;
					return path;
				}
				path.Cost = fare;
				path.Boardings1 = numberOfBoards1;
				path.Boardings2 = numberOfBoards2;

				// for sacog, use pathtype-specific time skims and weights
				var pathTypeSpecificTime = 0D;
				var pathTypeSpecificTimeWeight = 0D;

				if (Global.Configuration.PathImpedance_TransitUsePathTypeSpecificTime) {
					pathTypeSpecificTimeWeight =
							  pathType == Global.Settings.PathTypes.LightRail
									? Global.Configuration.PathImpedance_TransitLightRailTimeAdditiveWeight
									: pathType == Global.Settings.PathTypes.PremiumBus
											? Global.Configuration.PathImpedance_TransitPremiumBusTimeAdditiveWeight
											: pathType == Global.Settings.PathTypes.CommuterRail
													 ? Global.Configuration.PathImpedance_TransitCommuterRailTimeAdditiveWeight
													 : pathType == Global.Settings.PathTypes.Ferry
															  ? Global.Configuration.PathImpedance_TransitFerryTimeAdditiveWeight
															  : 0D;

					if (Math.Abs(pathTypeSpecificTimeWeight) > Constants.EPSILON) {
						pathTypeSpecificTime =
							 pathType == Global.Settings.PathTypes.LightRail
								  ? ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable
								  : pathType == Global.Settings.PathTypes.PremiumBus
										  ? ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, outboundTime, originZoneId, destinationZoneId).Variable
										  : 0D;

						if (returnTime > 0) {
							pathTypeSpecificTime +=
								 pathType == Global.Settings.PathTypes.LightRail
									  ? ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable
									  : pathType == Global.Settings.PathTypes.PremiumBus
											  ? ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, returnTime, destinationZoneId, originZoneId).Variable
											  : 0D;
						}
					}
				}
				var totalInVehicleTime = outboundInVehicleTime + returnInVehicleTime;
				path.Utility =
						  Global.Configuration.PathImpedance_PathChoiceScaleFactor *
						  (pathTypeConstant +
							_tourCostCoefficient * fare +
							_tourTimeCoefficient *
							(Global.Configuration.PathImpedance_TransitInVehicleTimeWeight * totalInVehicleTime +
							 Global.Configuration.PathImpedance_TransitFirstWaitTimeWeight * initialWaitTime +
							 Global.Configuration.PathImpedance_TransitTransferWaitTimeWeight * transferWaitTime +
							 Global.Configuration.PathImpedance_TransitNumberBoardingsWeight * (numberOfBoards1 + numberOfBoards2) +
							 pathTypeSpecificTime * pathTypeSpecificTimeWeight));

			}
			return path;
		}


		private static double GetTransitWalkTime(IParcelWrapper parcel, int pathType, double boardings) {
			var walkDist = parcel.DistanceToLocalBus; // default is local bus (feeder), for any submode

			double altDist;

			if (pathType == Global.Settings.PathTypes.LightRail) {
				altDist = parcel.DistanceToLightRail;
			}
			else if (pathType == Global.Settings.PathTypes.PremiumBus) {
				altDist = parcel.DistanceToExpressBus;
			}
			else if (pathType == Global.Settings.PathTypes.CommuterRail) {
				altDist = parcel.DistanceToCommuterRail;
			}
			else if (pathType == Global.Settings.PathTypes.Ferry) {
				altDist = parcel.DistanceToFerry;
			}
			else {
				altDist = Constants.DEFAULT_VALUE;
			}

			if ((altDist >= 0 && altDist < walkDist) || (boardings < Global.Configuration.PathImpedance_TransitSingleBoardingLimit && altDist >= 0 && altDist < Global.Configuration.PathImpedance_TransitWalkAccessDirectLimit)) {
				walkDist = altDist;
			}

			return
				(walkDist >= 0 && walkDist < Global.Configuration.PathImpedance_TransitWalkAccessDistanceLimit)
					? walkDist * Global.PathImpedance_WalkMinutesPerDistanceUnit
					: Constants.DEFAULT_VALUE; // -1 is "missing" value
		}

		double GammaFunction(double x, double gamma) {
			double xGamma;
			xGamma = gamma * x + (1 - gamma) * Math.Log(Math.Max(x, 1.0));
			return xGamma;
		}
	}

}
