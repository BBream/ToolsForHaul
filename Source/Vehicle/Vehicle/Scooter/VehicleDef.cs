using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;


namespace Vehicle
{
    public class VehicleDef : ThingDef
    {
        public VehiclProperties vehicle;

        public override void PostLoad()
        {
            base.PostLoad();
            if (this.vehicle != null)
                this.vehicle.PostLoadSpecial(this);
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (this.vehicle != null)
                this.vehicle.ResolveReferencesSpecial();
        }
    }

    public class VehiclProperties
    {
        //Data of mounting
        public IntVec3           mountPosOffset;
        public BodyPartDef       mountedPart;
        public int               maxNumOfBoarding;

        //Data of Parts
        public List<Parts_TurretGunDef> turretGunDefs;
        //public List<Parts_Component> component;

        //draw setting
        public Vector3           driverOffset;
        public PawnVisible     driverVisible;
        public List<Vector3>     crewsOffset;
        public List<PawnVisible> crewsVisible;
        public List<ExtraGraphicData> extraGraphicDefs;

        public void PostLoadSpecial(ThingDef parentDef)
        {
        }

        public void ResolveReferencesSpecial()
        {
        }
    }

    public enum PawnVisible
    {
        Always = 2,
        IfSelected = 1,
        Never = 0
    }

    public class Parts_TurretGunDef
    {
        public ExtraGraphicData turretTopExtraGraphicData;
        public Vector3 partsOffset;
        public ThingDef turretGunDef;
        public ThingDef turretShellDef;
        public int turretBurstWarmupTicks;
        public int turretBurstCooldownTicks;
    }

    public class ExtraGraphicData
    {
        public string         graphicPath;
        public Vector3        drawingOffset;
        public bool           InvisibleWhenSelected;
    }
}
