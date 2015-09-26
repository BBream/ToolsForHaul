using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicle
{
    public class CompCarrier : ThingComp
    {
        public Pawn slave = null;
        public JobGiver_Carrier carrier;

        public CompCarrier(): base() {carrier = new JobGiver_Carrier();}

        public Job GetJob{ get {return carrier.TryGetJobFor(slave);} }

        public void MakeDoJob()
        {
            Job newJob = GetJob;
            slave.playerController.TakeOrderedJob(newJob);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (slave != null && !slave.playerController.Drafted)
                MakeDoJob();
        }
    }
}