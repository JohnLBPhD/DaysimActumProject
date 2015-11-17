﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

//using Daysim.DomainModels.Actum.Models.Interfaces;
//using Daysim.DomainModels.Actum.Wrappers.Interfaces;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.Factories;
using Daysim.Framework.DomainModels.Wrappers;

namespace Daysim.DomainModels.Actum.Wrappers {
	[Factory(Factory.WrapperFactory, Category = Category.Wrapper, DataType = DataType.Actum)]
	public class ParkAndRideNodeWrapper : Default.Wrappers.ParkAndRideNodeWrapper, IParkAndRideNodeWrapper {
		private readonly IParkAndRideNode _parkAndRideNode;

		[UsedImplicitly]
		public ParkAndRideNodeWrapper(IParkAndRideNode parkAndRideNode) : base(parkAndRideNode) {
			_parkAndRideNode = (IParkAndRideNode) parkAndRideNode;
		}

		#region domain model properies

		//public string TerminalName {
		//	get { return _parkAndRideNode.TerminalName; }
		//	set { _parkAndRideNode.TerminalName = value; }
		//}

		public int ParkingTypeId {
			get { return _parkAndRideNode.ParkingTypeId; }
			set { _parkAndRideNode.ParkingTypeId = value; }
		}

		public double CostPerHour08_18 {
			get { return _parkAndRideNode.CostPerHour08_18; }
			set { _parkAndRideNode.CostPerHour08_18 = value; }
		}

		public double CostPerHour18_23 {
			get { return _parkAndRideNode.CostPerHour18_23; }
			set { _parkAndRideNode.CostPerHour18_23 = value; }
		}

		public double CostPerHour23_08 {
			get { return _parkAndRideNode.CostPerHour23_08; }
			set { _parkAndRideNode.CostPerHour23_08 = value; }
		}

		public double CostAnnual {
			get { return _parkAndRideNode.CostAnnual; }
			set { _parkAndRideNode.CostAnnual = value; }
		}

		public int PRFacility {
			get { return _parkAndRideNode.PRFacility; }
			set { _parkAndRideNode.PRFacility = value; }
		}

		public int LengthToStopArea {
			get { return _parkAndRideNode.LengthToStopArea; }
			set { _parkAndRideNode.LengthToStopArea = value; }
		}

		public int Auto {
			get { return _parkAndRideNode.Auto; }
			set { _parkAndRideNode.Auto = value; }
		}

		#endregion
	
	
	
	
	
	}
}