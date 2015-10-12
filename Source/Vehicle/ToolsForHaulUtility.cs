using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    static class ToolsForHaulUtility
    {
        public static string NoHaulable;
        public static string NoEmptyPlaceLowerTrans;
        private static readonly JobDef jobDefHaulWithBackpack = DefDatabase<JobDef>.GetNamed("HaulWithBackpack");
        private static readonly JobDef jobDefHaulWithAnimalCart = DefDatabase<JobDef>.GetNamed("HaulWithAnimalCart");
        private static readonly JobDef jobDefHaulWithCart = DefDatabase<JobDef>.GetNamed("HaulWithCart");
        private static IntVec3 invalidCell = new IntVec3(0, 0, 0);
        private const int NearbyCell = 10;

        public static void Reset()
        {
            ToolsForHaulUtility.NoHaulable = Translator.Translate("NoHaulable");
            ToolsForHaulUtility.NoEmptyPlaceLowerTrans = Translator.Translate("NoEmptyPlaceLower");
        }
        public static List<Thing> Cart() { return Find.ListerThings.AllThings.FindAll((Thing thing) => (thing is Vehicle_Cart)); }
        public static Apparel_Backpack TryGetBackpack(Pawn pawn)
        {
            foreach (Apparel apparel in pawn.apparel.WornApparel)
                if (apparel is Apparel_Backpack)
                    return apparel as Apparel_Backpack;
            return null;
        }
        public static bool AvailableCart(Vehicle_Cart cart, Pawn pawn)
        {
            return (!cart.TryGetComp<CompMountable>().IsMounted || cart.TryGetComp<CompMountable>().Driver == pawn);
        }
        public static bool AvailableAnimalCart(Vehicle_Cart cart)
        {
            Pawn Driver = (cart.TryGetComp<CompMountable>().IsMounted) ? cart.TryGetComp<CompMountable>().Driver : null;
            if (Driver == null)
                return false;

            return Driver.RaceProps.Animal && PawnUtility.CasualInterruptibleNow(Driver)
                && Driver.needs.food.CurCategory < HungerCategory.Starving 
                && Driver.needs.rest.CurCategory < RestCategory.VeryTired
                && !Driver.health.ShouldBeTreatedNow;
        }
        public static Job HaulWithTools(Pawn pawn, Vehicle_Cart cart = null)
        {

            //Job Setting
            JobDef jobDef;
            Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
            if (cart == null)
                jobDef = jobDefHaulWithBackpack;
            else if (cart.TryGetComp<CompMountable>().IsMounted && cart.TryGetComp<CompMountable>().Driver.RaceProps.Animal)
                jobDef = jobDefHaulWithAnimalCart;
            else
                jobDef = jobDefHaulWithCart;
            Job job = new Job(jobDef);
            job.targetQueueA = new List<TargetInfo>();
            job.targetQueueB = new List<TargetInfo>();
            job.targetC = (cart != null) ? (Thing)cart : (Thing)backpack;

            //Drop remaining item
            int numRemainingItems = (cart != null)? cart.storage.Count : pawn.inventory.container.Count;
            if (numRemainingItems > 0)
            {
                IEnumerable<Thing> remainingItems = (cart != null) ? cart.storage : pawn.inventory.container;
                foreach (Thing remainingItem in remainingItems)
                {
                    IntVec3 storageCell = FindStorageCell(pawn, remainingItem, job.targetQueueB);
                    if (!storageCell.IsValid) break;

                    ReservationUtility.Reserve(pawn, storageCell);
                    job.targetQueueB.Add(storageCell);
                }
                if (!job.targetQueueB.NullOrEmpty())
                    return job;
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                return (Job)null;
            }

            //Collect and drop item
            int reservedMaxItem = 0;
            int maxItem = (cart != null) ? cart.MaxItem : (backpack != null) ? backpack.maxItem : 0;
            //List<Thing> deniedThings = new List<Thing>();
            while (reservedMaxItem < maxItem)
            {
                //ClosestThing_Global_Reachable Configuration
                Predicate<Thing> predicate = item
                    => !job.targetQueueA.Contains(item) && !FireUtility.IsBurning(item) //&& !deniedThings.Contains(item)
                    && pawn.CanReserveAndReach(item, PathEndMode.Touch, DangerUtility.NormalMaxDanger(pawn));
                IntVec3 searchPos = (!job.targetQueueA.NullOrEmpty() && job.targetQueueA.First().Thing.Position != IntVec3.Invalid) ?
                    job.targetQueueA.First().Thing.Position : (cart != null) ? cart.Position : pawn.Position;
                int maxDistance = (!job.targetQueueA.NullOrEmpty() && job.targetQueueA.First().Thing.Position != IntVec3.Invalid) ?
                    NearbyCell : 99999;

                //Find Haulable
                Thing closestHaulable = GenClosest.ClosestThing_Global_Reachable(searchPos,
                                                                            ListerHaulables.ThingsPotentiallyNeedingHauling(),
                                                                            PathEndMode.Touch,
                                                                            TraverseParms.For(pawn, Danger.Some),
                                                                            maxDistance,
                                                                            predicate);
                //Check it can be hauled
                /*
                if ((closestHaulable is UnfinishedThing && ((UnfinishedThing)closestHaulable).BoundBill != null)
                    || (closestHaulable.def.IsNutritionSource && !SocialProperness.IsSociallyProper(closestHaulable, pawn, false, true)))
                {
                    deniedThings.Add(closestHaulable);
                    continue;
                }*/
                if (closestHaulable == null) break;

                //Find StorageCell
                IntVec3 storageCell = FindStorageCell(pawn, closestHaulable, job.targetQueueB);
                if (storageCell == IntVec3.Invalid) break;

                //Add Queue & Reserve
                job.targetQueueA.Add(closestHaulable);
                job.targetQueueB.Add(storageCell);
                reservedMaxItem++;
            }
            if (!job.targetQueueA.NullOrEmpty() && !job.targetQueueB.NullOrEmpty())
                return job;
            if (job.targetQueueA.NullOrEmpty()) 
                JobFailReason.Is(ToolsForHaulUtility.NoHaulable);
            else
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
            return (Job)null;
        }


        private static IntVec3 FindStorageCell(Pawn pawn, Thing closestHaulable, List<TargetInfo> targetQueue)
        {
            //Find closest cell in queue.
            if (!targetQueue.NullOrEmpty())
                foreach (TargetInfo target in targetQueue)
                    foreach (var adjCell in GenAdjFast.AdjacentCells8Way(target))
                        if (!targetQueue.Contains(adjCell) && adjCell.IsValidStorageFor(closestHaulable) && pawn.CanReserve(adjCell))
                            return adjCell;
            
            StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(closestHaulable.Position, closestHaulable);
            IntVec3 foundCell;
            if (StoreUtility.TryFindBestBetterStoreCellFor(closestHaulable, pawn, currentPriority, pawn.Faction, out foundCell, true))
                return foundCell;

            /*
            foreach (var slotGroup in Find.SlotGroupManager.AllGroupsListInPriorityOrder)
                foreach (var cell in slotGroup.CellsList.Where(cell =>
                            !targetQueue.Contains(cell) && StoreUtility.IsValidStorageFor(cell, closestHaulable) && pawn.CanReserve(cell)))
                    if (cell != invalidCell && cell != IntVec3.Invalid)
                        return cell;
            */

            return IntVec3.Invalid;
        }
        
        #if DEBUG
        public static void DebugWriteHaulingPawn(Pawn pawn)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(pawn.LabelCap + " Report: Cart " + Cart().Count);
            foreach (Pawn other in Find.ListerPawns.FreeColonistsSpawned)
            {
                //Vanilla haul or Haul with backpack
                if (other.CurJob != null && (other.CurJob.def == JobDefOf.HaulToCell || other.CurJob.def == jobDefHaulWithBackpack))
                    stringBuilder.AppendLine(other.LabelCap + " Job: " + other.CurJob.def.defName + " lastGivenWorkType: " + other.mindState.lastGivenWorkType);
            }
            foreach (Vehicle_Cart cart in Cart())
            {
                string driver = ((cart.mountableComp.IsMounted) ? cart.mountableComp.Driver.LabelCap : "No Driver");
                string state = "";
                if (cart.IsForbidden(pawn.Faction))
                    state = string.Concat(state, "Forbidden ");
                if (pawn.CanReserveAndReach(cart, PathEndMode.Touch, Danger.Some))
                    state = string.Concat(state, "CanReserveAndReach ");
                if (AvailableCart(cart, pawn))
                    state = string.Concat(state, "AvailableCart ");
                if (AvailableAnimalCart(cart))
                    state = string.Concat(state, "AvailableAnimalCart ");
                Pawn reserver = Find.Reservations.FirstReserverOf(cart, Faction.OfColony);
                if (reserver != null)
                    state = string.Concat(state, reserver.LabelCap, " Job: ", reserver.CurJob.def.defName);
                stringBuilder.AppendLine(cart.LabelCap + "- " + driver + ": " + state);

            }
            Log.Message(stringBuilder.ToString());
        }
        #endif
    }
}
