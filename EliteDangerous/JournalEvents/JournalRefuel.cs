﻿/*
 * Copyright © 2016-2023 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 *
 *
 */
using QuickJSON;
using System.Linq;

namespace EliteDangerousCore.JournalEvents
{
    [JournalEntryType(JournalTypeEnum.RefuelAll)]
    public class JournalRefuelAll : JournalEntry, ILedgerJournalEntry, IShipInformation
    {
        public JournalRefuelAll(JObject evt ) : base(evt, JournalTypeEnum.RefuelAll)
        {
            Cost = evt["Cost"].Long();
            Amount = evt["Amount"].Double();
        }

        public long Cost { get; set; }
        public double Amount { get; set; }

        public void Ledger(Ledger mcl)
        {
            mcl.AddEvent(Id, EventTimeUTC, EventTypeID, Amount.ToString("0.0") + "t", -Cost);
        }

        public override void FillInformation(out string info, out string detailed) 
        {
            info = BaseUtils.FieldBuilder.Build("Cost: ; cr;N0".T(EDCTx.JournalEntry_Cost), Cost, "Fuel: ; tons;0.0".T(EDCTx.JournalEntry_Fuel), Amount);
            detailed = "";
        }

        public void ShipInformation(ShipList shp, string whereami, ISystem system)
        {
            shp.RefuelAll(this);
        }
    }

    [JournalEntryType(JournalTypeEnum.RefuelPartial)]
    public class JournalRefuelPartial : JournalEntry, ILedgerJournalEntry, IShipInformation
    {
        public JournalRefuelPartial(JObject evt) : base(evt, JournalTypeEnum.RefuelPartial)
        {
            Cost = evt["Cost"].Long();
            Amount = evt["Amount"].Int();
        }

        public long Cost { get; set; }
        public int Amount { get; set; }

        public void Ledger(Ledger mcl)
        {
            mcl.AddEvent(Id, EventTimeUTC, EventTypeID, Amount.ToString("0.0") + "t", -Cost);
        }

        public override void FillInformation(out string info, out string detailed)
        {
            info = BaseUtils.FieldBuilder.Build("Cost: ; cr;N0".T(EDCTx.JournalEntry_Cost), Cost, "Fuel: ; tons;0.0".T(EDCTx.JournalEntry_Fuel), Amount);
            detailed = "";
        }

        public void ShipInformation(ShipList shp, string whereami, ISystem system)
        {
            shp.RefuelPartial(this);
        }
    }


    [JournalEntryType(JournalTypeEnum.FuelScoop)]
    public class JournalFuelScoop : JournalEntry, IShipInformation
    {
        public JournalFuelScoop(JObject evt) : base(evt, JournalTypeEnum.FuelScoop)
        {
            Scooped = evt["Scooped"].Double();
            Total = evt["Total"].Double();
        }
        public double Scooped { get; set; }
        public double Total { get; set; }

        public override void FillInformation(out string info, out string detailed)
        {
            info = BaseUtils.FieldBuilder.Build(";t;0.0", Scooped, "Total: ;t;0.0".T(EDCTx.JournalFuelScoop_Total), Total);
            detailed = "";
        }

        public void ShipInformation(ShipList shp, string whereami, ISystem system)
        {
            shp.FuelScoop(this);
        }
    }

    [JournalEntryType(JournalTypeEnum.ReservoirReplenished)]
    public class JournalReservoirReplenished : JournalEntry, IShipInformation
    {
        public JournalReservoirReplenished(JObject evt) : base(evt, JournalTypeEnum.ReservoirReplenished)
        {
            FuelMain = evt["FuelMain"].Double();
            FuelReservoir = evt["FuelReservoir"].Double();
        }

        public double FuelMain { get; set; }
        public double FuelReservoir { get; set; }

        public override void FillInformation(out string info, out string detailed)
        {
            info = BaseUtils.FieldBuilder.Build("Main: ;t;0.0".T(EDCTx.JournalReservoirReplenished_Main), FuelMain, "Reservoir: ;t;0.0".T(EDCTx.JournalReservoirReplenished_Reservoir), FuelReservoir);
            detailed = "";
        }

        public void ShipInformation(ShipList shp, string whereami, ISystem system)
        {
            shp.FuelReservoirReplenished(this);
        }
    }

}
