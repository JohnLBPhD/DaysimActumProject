﻿namespace DaySim.PathTypeModels {
  public interface IPathTypeModel {
    int Mode { get; set; }

    double GeneralizedTimeLogsum { get; }

    double GeneralizedTimeChosen { get; }

    double PathTime { get; }

    double PathDistance { get; }

    double PathCost { get; }

    int PathType { get; }

    int PathParkAndRideNodeId { get; }

    double PathTransitTime { get; }
    double PathTransitDistance { get; }
    double PathTransitCost { get; }
    double PathTransitGeneralizedTime { get; }

    int PathOriginStopAreaKey { get; }
    int PathDestinationStopAreaKey { get; }
    double PathWalkTime { get; }
    double PathWalkDistance { get; }
    double PathBikeTime { get; }
    double PathBikeDistance { get; }
    double PathBikeCost { get; }

    bool Available { get; }

    //		List<IPathTypeModel> RunAllPlusParkAndRide (IRandomUtility randomUtility, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, int transitPassOwnership, double transitDiscountFraction, bool randomChoice);

    //		List<IPathTypeModel> RunAll(IRandomUtility randomUtility, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, int transitPassOwnership, double transitDiscountFraction, bool randomChoice);

    //		List<IPathTypeModel> Run(IRandomUtility randomUtility, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, int transitPassOwnership, double transitDiscountFraction, bool randomChoice, params int[] modes);

    //		List<IPathTypeModel> Run(IRandomUtility randomUtility, int originZoneId, int destinationZoneId, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, int transitPassOwnership, double transitDiscountFraction, bool randomChoice, params int[] modes);
  }
}
