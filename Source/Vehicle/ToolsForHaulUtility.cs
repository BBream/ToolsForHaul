#define DEBUG

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
        private const int NearbyCell = 10;

        static ToolsForHaulUtility()
        {
            ToolsForHaulUtility.NoHaulable = Translator.Translate("NoHaulable");
            ToolsForHaulUtility.NoEmptyPlaceLowerTrans = Translator.Translate("NoEmptyPlaceLower");
        }
        public static Apparel_Backpack TryGetBackpack(Pawn pawn)
        {
            foreach (Apparel apparel in pawn.apparel.WornApparel)
                if (apparel is Apparel_Backpack)
                    return apparel as Apparel_Backpack;
            return null;
        }
        public static Thing TryGetBackpackLastItem(Pawn pawn)
        {
            Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
            if (backpack == null)
                return null;
            Thing lastItem = null;
            Thing foodInInventory = FoodUtility.FoodInInventory(pawn);
            if (pawn.inventory.container.Count > 0 && backpack.numOfSavedItems > 0)
                lastItem = pawn.inventory.container[((backpack.numOfSavedItems > pawn.inventory.container.Count)?pawn.inventory.container.Count : backpack.numOfSavedItems) - 1];
            if (foodInInventory != null && backpack.numOfSavedItems < pawn.inventory.container.Count)
                lastItem = foodInInventory;
            return lastItem;
        }

        public static List<Thing> Cart() { return Find.ListerThings.AllThings.FindAll((Thing thing) => (thing is Vehicle_Cart)); }
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
            TargetInfo targetC;
            int maxItem;
            int reservedMaxItem;
            IEnumerable<Thing> remainingItems;
            bool ShouldDrop = true;
            Thing lastItem = ToolsForHaulUtility.TryGetBackpackLastItem(pawn);
            if (cart == null)
            {
                Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
                jobDef = jobDefHaulWithBackpack;
                targetC = backpack;
                maxItem = backpack.maxItem;
                reservedMaxItem = pawn.inventory.container.Count;
                remainingItems = pawn.inventory.container;
                if (lastItem != null)
                    for (int i = 0; i < pawn.inventory.container.Count; i++)
                        if (pawn.inventory.container[i] == lastItem && (reservedMaxItem - (i + 1)) <= 0)
                        {
                            ShouldDrop = false;
                            break;
                        }
            }
            else
            {
                jobDef = (cart.TryGetComp<CompMountable>().IsMounted && cart.TryGetComp<CompMountable>().Driver.RaceProps.Animal)? 
                    jobDefHaulWithAnimalCart : jobDefHaulWithCart;
                targetC = cart;
                maxItem = cart.MaxItem;
                reservedMaxItem = cart.storage.Count;
                remainingItems = cart.storage;
            }
            Job job = new Job(jobDef);
            job.targetQueueA = new List<TargetInfo>();
            job.targetQueueB = new List<TargetInfo>();
            job.targetC = targetC;

            #if DEBUG
            Log.Message(pawn.LabelCap + " In HaulWithTools: " + jobDef.defName + "\n"
                + "MaxItem: " + maxItem + " reservedMaxItem: " + reservedMaxItem);
            #endif

            //Drop remaining item
            if (reservedMaxItem >= Math.Ceiling(maxItem * 0.5) && ShouldDrop)
            {
                bool startDrop = false;
                for (int i = 0; i < remainingItems.Count(); i++)
                {
                    if (startDrop == false)
                        if (remainingItems.ElementAt(i) == lastItem) 
                            startDrop = true;
                        else
                            continue;
                    IntVec3 storageCell = FindStorageCell(pawn, remainingItems.ElementAt(i), job.targetQueueB);
                    if (storageCell == IntVec3.Invalid) break;
                    job.targetQueueB.Add(storageCell);
                }
                if (!job.targetQueueB.NullOrEmpty())
                    return job;
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                #if DEBUG
                Log.Message("No Job. Reason: " + ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                #endif
                return (Job)null;
            }

            //Collect and drop item
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
            JobFailReason.Is((job.targetQueueA.NullOrEmpty()) ? ToolsForHaulUtility.NoHaulable : ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
            #if DEBUG
            Log.Message("No Job. Reason: " + ((job.targetQueueA.NullOrEmpty()) ? ToolsForHaulUtility.NoHaulable : ToolsForHaulUtility.NoEmptyPlaceLowerTrans));
            #endif
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

            //Vanila code is not worked item on container.
            foreach (var slotGroup in Find.SlotGroupManager.AllGroupsListInPriorityOrder)
            {
                foreach (var cell in slotGroup.CellsList.Where(cell =>
                            !targetQueue.Contains(cell) && StoreUtility.IsValidStorageFor(cell, closestHaulable) && pawn.CanReserve(cell)))
                    if (cell != IntVec3.Invalid)
                        return cell;
            }

            return IntVec3.Invalid;
        }
        
        #if DEBUG
        public static void DebugWriteHaulingPawn(Pawn pawn)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(pawn.LabelCap + " Report: Cart " + Cart().Count + " Job: " + ((pawn.CurJob != null)? pawn.CurJob.def.defName : "No Job")
                + " Backpack: " + ((ToolsForHaulUtility.TryGetBackpack(pawn) != null) ? "True" : "False")
                + " lastGivenWorkType: " + pawn.mindState.lastGivenWorkType);
            foreach (Pawn other in Find.ListerPawns.FreeColonistsSpawned)
            {
                //Vanilla haul or Haul with backpack
                if (other.CurJob != null && (other.CurJob.def == JobDefOf.HaulToCell || other.CurJob.def == jobDefHaulWithBackpack))
                    stringBuilder.AppendLine(other.LabelCap + " Job: " + other.CurJob.def.defName
                        + " Backpack: " + ((ToolsForHaulUtility.TryGetBackpack(other) != null) ? "True" : "False")
                        + " lastGivenWorkType: " + other.mindState.lastGivenWorkType);
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
