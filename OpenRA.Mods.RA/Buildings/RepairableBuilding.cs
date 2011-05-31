#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using System.Collections.Generic;
using OpenRA.Orders;

namespace OpenRA.Mods.RA.Buildings
{
	public class RepairableBuildingInfo : ITraitInfo, Requires<HealthInfo>
	{
		public readonly int RepairPercent = 20;
		public readonly int RepairInterval = 24;
		public readonly int RepairStep = 7;
		public object Create(ActorInitializer init) { return new RepairableBuilding(init.self, this); }
	}

	public class RepairableBuilding : ITick, IResolveOrder, ISync, IIssueOrder
	{
		[Sync]
		bool isRepairing = false;
		
		Health Health;
		RepairableBuildingInfo Info;
		public RepairableBuilding(Actor self, RepairableBuildingInfo info)
		{
			Health = self.Trait<Health>();
			Info = info;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Repair")
			{
				isRepairing = !isRepairing;
				if (isRepairing)
					Sound.PlayToPlayer(self.Owner, self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>().Repairing);
			}
		}

		int remainingTicks;
		public void Tick(Actor self)
		{
			if (!isRepairing) return;

			if (remainingTicks == 0)
			{
				var buildingValue = self.GetSellValue();

				var hpToRepair = Math.Min(Info.RepairStep, Health.MaxHP - Health.HP);
				var cost = (hpToRepair * Info.RepairPercent * buildingValue) / (Health.MaxHP * 100);
				if (!self.Owner.PlayerActor.Trait<PlayerResources>().TakeCash(cost))
				{
					remainingTicks = 1;
					return;
				}

				self.World.AddFrameEndTask(
                    w => w.Add(new RepairIndicator(self, Info.RepairInterval / 2)));

				self.InflictDamage(self, -hpToRepair, null);

				if (Health.DamageState == DamageState.Undamaged)
				{
					isRepairing = false;
					return;
				}

				remainingTicks = Info.RepairInterval;
			}
			else
				--remainingTicks;
		}

        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new PaletteOnlyOrderTargeter("Repair"); }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
        {
            throw new NotImplementedException();
        }
    }
	public class AllowsBuildingRepairInfo : TraitInfo<AllowsBuildingRepair> {}
	public class AllowsBuildingRepair {}

}
