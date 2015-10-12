using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;


namespace ToolsForHaul
{
    public class WorkGiver_HaulWithTools : WorkGiver_Scanner
    {
        private static string NoAvailableCart;

        public WorkGiver_HaulWithTools()
        {
            WorkGiver_HaulWithTools.NoAvailableCart = Translator.Translate("NoAvailableCart");
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return ToolsForHaulUtility.Cart() as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            //ToolsForHaulUtility.DebugWriteHaulingPawn(pawn);
            if (ToolsForHaulUtility.Cart().Count == 0)
                return true;
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Vehicle_Cart cart = t as Vehicle_Cart;
            if (cart == null)
                return (Job)null;
            if (cart.IsForbidden(pawn.Faction) || !ReservationUtility.CanReserveAndReach(pawn, cart, PathEndMode.ClosestTouch, Danger.Some))
                return (Job)null;
            if (ToolsForHaulUtility.AvailableAnimalCart(cart) || ToolsForHaulUtility.AvailableCart(cart, pawn))
            {
                Job job = ToolsForHaulUtility.HaulWithTools(pawn, cart);
                if (job == null)
                    JobFailReason.Is(ToolsForHaulUtility.NoHaulWithTools);
                return job;
            }
            JobFailReason.Is(WorkGiver_HaulWithTools.NoAvailableCart);
            return (Job)null;
        }
    }
}