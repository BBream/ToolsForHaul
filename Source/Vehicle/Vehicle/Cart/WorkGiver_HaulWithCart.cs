using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;


namespace ToolsForHaul
{
    public class WorkGiver_HaulWithCart : WorkGiver_Scanner
    {
        private static string NoAvailableCart = Translator.Translate("NoAvailableCart");
        private static string BurningLowerTrans = Translator.Translate("BurningLower");

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return ToolsForHaulUtility.Cart() as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            #if DEBUG
            ToolsForHaulUtility.DebugWriteHaulingPawn(pawn);
            #endif
            if (ListerHaulables.ThingsPotentiallyNeedingHauling().Count == 0)
                return true;
            if (ToolsForHaulUtility.Cart().Count == 0)
                return true;
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Vehicle_Cart cart = t as Vehicle_Cart;
            if (cart == null)
                return (Job)null;
            if (cart.IsForbidden(pawn.Faction) || !ReservationUtility.CanReserveAndReach(pawn, cart, PathEndMode.ClosestTouch, DangerUtility.NormalMaxDanger(pawn)))
                return (Job)null;
            if (FireUtility.IsBurning(cart))
            {
                JobFailReason.Is(WorkGiver_HaulWithCart.BurningLowerTrans);
                return (Job)null;
            }
            if (ToolsForHaulUtility.AvailableAnimalCart(cart) || ToolsForHaulUtility.AvailableCart(cart, pawn))
                return ToolsForHaulUtility.HaulWithTools(pawn, cart);
            JobFailReason.Is(WorkGiver_HaulWithCart.NoAvailableCart);
            return (Job)null;
        }

    }

}