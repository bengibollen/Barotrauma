﻿using Barotrauma.Items.Components;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Barotrauma
{
    class AIObjectivePumpWater : AIObjectiveLoop<Pump>
    {
        public override string DebugTag => "pump water";
        public override bool KeepDivingGearOn => true;
        private readonly IEnumerable<Pump> pumpList;

        public AIObjectivePumpWater(Character character, string option, float priorityModifier = 1) : base(character, option, priorityModifier)
        {
            pumpList = character.Submarine.GetItems(true).Select(i => i.GetComponent<Pump>()).Where(p => p != null);
        }

        public override bool IsDuplicate(AIObjective otherObjective) => otherObjective is AIObjectivePumpWater && otherObjective.Option == Option;

        //availablePumps = allPumps.Where(p => !p.Item.HasTag("ballast") && p.Item.Connections.None(c => c.IsPower && p.Item.GetConnectedComponentsRecursive<Steering>(c).None())).ToList();
        protected override void FindTargets()
        {
            if (option == null) { return; }
            foreach (Item item in Item.ItemList)
            {
                if (item.HasTag("ballast")) { continue; }
                if (item.Submarine == null) { continue; }
                if (item.Submarine.TeamID != character.TeamID) { continue; }
                if (character.Submarine != null && !character.Submarine.IsEntityFoundOnThisSub(item, true)) { continue; }
                var pump = item.GetComponent<Pump>();
                if (pump != null)
                {
                    if (!ignoreList.Contains(pump))
                    {
                        if (option == "stoppumping")
                        {
                            if (!pump.IsActive || pump.FlowPercentage == 0.0f) { continue; }
                        }
                        else
                        {
                            if (!pump.Item.InWater) { continue; }
                            if (pump.IsActive && pump.FlowPercentage <= -90.0f) { continue; }
                        }
                        if (!targets.Contains(pump))
                        {
                            targets.Add(pump);
                        }
                    }
                }
            }
        }

        protected override bool Filter(Pump pump) => true;
        protected override IEnumerable<Pump> GetList() => pumpList;
        protected override float Average(Pump target) => MathHelper.Lerp(0, 100, target.CurrFlow / target.MaxFlow);
        protected override AIObjective ObjectiveConstructor(Pump pump) => new AIObjectiveOperateItem(pump, character, Option, false) { IsLoop = true };
    }
}
