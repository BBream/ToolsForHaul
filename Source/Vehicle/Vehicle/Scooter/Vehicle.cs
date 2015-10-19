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
    public class Vehicle : ThingWithComps
    {

        #region Variables and short method
        // ==================================
        public const int ThresholdAutoDismount = 4800;

        public bool visibleInside;
        public int autoDismountTick = 0;
        public VehicleDef vehicleDef;

        //Data of mounting
        public Vehicle_DriverTracker driver;
        public Vehicle_CrewsTracker crews;

        //Data of drawing
        public List<Graphic_Single> extraGraphics;

        //Data of Parts
        public List<Parts_TurretGun> turretGuns;

        //Method for mounting
        public IntVec3 MountPos { get { return (Position + vehicleDef.vehicle.mountPosOffset.RotatedBy(this.Rotation)); } }
        public bool IsMounted { get { return driver.IsMounted; } }
        public bool HasCrew { get { return crews.HasCrew; } }
        public Pawn Driver { get { return driver.Driver; } }

        #endregion

        #region Thing basic method
        // ==================================
        public Vehicle()
        {
            visibleInside = false;
            driver = new Vehicle_DriverTracker(this);
            crews = new Vehicle_CrewsTracker(this);
            turretGuns = new List<Parts_TurretGun>();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            vehicleDef = def as VehicleDef;
            if (!vehicleDef.vehicle.turretGunDefs.NullOrEmpty())
                for (int i = 0; i < vehicleDef.vehicle.turretGunDefs.Count; i++)
                {
                    Parts_TurretGun turretGun = new Parts_TurretGun();
                    turretGun.parent = this;
                    turretGun.parts_TurretGunDef = vehicleDef.vehicle.turretGunDefs[i];
                    turretGun.SpawnSetup();
                    turretGuns.Add(turretGun);
                }   
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep<Vehicle_DriverTracker>(ref driver, "driver");
            Scribe_Deep.LookDeep<Vehicle_CrewsTracker>(ref crews, "crews");
            Scribe_Collections.LookList<Parts_TurretGun>(ref turretGuns, "turretGuns", LookMode.Deep);
            Scribe_Values.LookValue<bool>(ref visibleInside, "visibleInside");
            Scribe_Values.LookValue<int>(ref autoDismountTick, "autoDismountTick");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            driver.Dismount();
            crews.UnboardAll();
            foreach (Parts_TurretGun turretGun in turretGuns)
                turretGun.Destroy(mode);
            //Thing dummy;

            //Drop resources
            /*foreach (ThingCount thingCount in def.costList)
            {
                Thing thing = ThingMaker.MakeThing(thingCount.thingDef);
                thing.stackCount = thingCount.count / 2;
                GenThing.TryDropAndSetForbidden(thing, this.Position, ThingPlaceMode.Near, out dummy, true);
            }*/
        }

        public override void Tick()
        {
            base.Tick();
            driver.DriverTick();
            crews.CrewsTick();
            if (IsMounted || HasCrew)
            {
                foreach (Parts_TurretGun turretGun in turretGuns)
                    turretGun.Tick();
            }
        }

        #endregion

        #region Thing graphic method
        // ==================================

        public override Vector3 DrawPos
        {
            get
            {
                if (!driver.IsMounted || !this.SpawnedInWorld)
                    return base.DrawPos;
                return driver.Position;
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            base.DrawAt(drawLoc);
            if (!Find.Selector.IsSelected(this))
                visibleInside = false;

            if (IsMounted && (vehicleDef.vehicle.driverVisible == PawnVisible.Always || (visibleInside && vehicleDef.vehicle.driverVisible == PawnVisible.IfSelected)))
            {
                Vector3 driverLoc = drawLoc; driverLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                Driver.Rotation = this.Rotation;
                Driver.DrawAt(drawLoc + vehicleDef.vehicle.driverOffset.RotatedBy(this.Rotation.AsAngle));
                Driver.DrawGUIOverlay();
            }
            if (vehicleDef.vehicle.maxNumOfBoarding > 0 && crews.container.Count(x => x is Pawn) > 0)
            {
                List<Thing> crewsInt = crews.Crews;
                for (int i = 0; i < crewsInt.Count; i++)
                    if (vehicleDef.vehicle.crewsVisible[i] == PawnVisible.Always || (visibleInside && vehicleDef.vehicle.crewsVisible[i] == PawnVisible.IfSelected))
                    {
                        Vector3 crewLoc = drawLoc; crewLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                        crewsInt[i].Rotation = this.Rotation;
                        crewsInt[i].DrawAt(crewLoc + vehicleDef.vehicle.crewsOffset[i].RotatedBy(this.Rotation.AsAngle));
                        crewsInt[i].DrawGUIOverlay();
                    }
            }
            if (extraGraphics.NullOrEmpty())
                UpdateGraphics();

            IEnumerable<ExtraGraphicData> partGraphicDefs = null;
            if (vehicleDef.vehicle.turretGunDefs != null)
                partGraphicDefs = vehicleDef.vehicle.turretGunDefs.Select(x => x.turretTopExtraGraphicData);
            for (int i = 0; i < turretGuns.Count(); i++)
                if (partGraphicDefs != null && !(partGraphicDefs.ElementAt(i).InvisibleWhenSelected && visibleInside))
                    turretGuns[i].DrawAt(drawLoc + partGraphicDefs.ElementAt(i).drawingOffset.RotatedBy(this.Rotation.AsAngle));

            for (int i = 0; i < extraGraphics.Count(); i++)
                if (!(vehicleDef.vehicle.extraGraphicDefs[i].InvisibleWhenSelected && visibleInside))
                    extraGraphics[i].Draw(drawLoc + vehicleDef.vehicle.extraGraphicDefs[i].drawingOffset.RotatedBy(this.Rotation.AsAngle), this.Rotation, this);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            foreach (Parts_TurretGun turretGun in turretGuns)
                turretGun.DrawExtraSelectionOverlays();
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!GenText.NullOrEmpty(inspectString))
                stringBuilder.Append(inspectString);
            foreach (Parts_TurretGun turretGun in turretGuns)
                stringBuilder.Append(turretGun.GetInspectString());
            return stringBuilder.ToString();
        }

        private void UpdateGraphics()
        {
            extraGraphics = new List<Graphic_Single>();

            foreach (ExtraGraphicData drawingDef in vehicleDef.vehicle.extraGraphicDefs)
            {
                Graphic_Single graphic = GraphicDatabase.Get<Graphic_Single>(drawingDef.graphicPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Single;
                extraGraphics.Add(graphic);
            }
        }
        #endregion

        public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(dinfo, out absorbed);
            //foreach (Parts_TurretGun turretGun in turretGuns)
            //    turretGun.PreApplyDamage(dinfo, out absorbed);
            //Vehicle's brain is mounting point. If it damaged, driver is also damaged.
            //if (dinfo.Part.Value)
            //    dinfo.Part.Value.Injury.Body.
        }

        #region Thing Gizmo FloatingOptionMenu

        public override IEnumerable<Gizmo> GetGizmos()
        {
            //Hunt Gizmo is not needed.
            //foreach (var baseGizmo in base.GetGizmos())
            //    yield return baseGizmo;

            if (this.Faction == Faction.OfColony && IsMounted)
            {
                Command_Action dismountGizmo = new Command_Action();

                dismountGizmo.defaultLabel = "Dismount";
                dismountGizmo.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnmount");
                dismountGizmo.activateSound = SoundDef.Named("Click");
                dismountGizmo.defaultDesc = "Dismount";
                dismountGizmo.action = () => { this.driver.Dismount(); };

                yield return dismountGizmo;

                Designator_Move designator = new Designator_Move();

                designator.vehicle = this;
                designator.defaultLabel = "Move";
                designator.defaultDesc = "Move vehicle";
                designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconMove");
                designator.activateSound = SoundDef.Named("Click");
                designator.hotKey = KeyBindingDefOf.Misc1;

                yield return designator;

                if (!turretGuns.NullOrEmpty())
                {
                    Designator_ForcedTarget designator2 = new Designator_ForcedTarget();

                    designator2.turretGuns = turretGuns;
                    designator2.defaultLabel = "Set forced target";
                    designator2.defaultDesc = "Set forced target";
                    designator2.icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack");
                    designator2.activateSound = SoundDef.Named("Click");
                    designator2.hotKey = KeyBindingDefOf.Misc2;

                    yield return designator2;

                    Command_Action haltGizmo = new Command_Action();

                    haltGizmo.defaultLabel = "Stop forced target";
                    haltGizmo.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt");
                    haltGizmo.activateSound = SoundDef.Named("Click");
                    haltGizmo.defaultDesc = "Stop forced target ";
                    haltGizmo.action = () =>
                    {
                        foreach (Parts_TurretGun turretGun in turretGuns)
                            turretGun.forcedTarget = null;
                    };

                    yield return haltGizmo;
                }
            }
            else if (!IsMounted && this.Faction == Faction.OfColony)
            {
                Designator_Mount designator = new Designator_Mount();

                designator.vehicle = this;
                designator.mountPos = MountPos;
                designator.defaultLabel = "Mount";
                designator.defaultDesc = "Mount";
                designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconMount");
                designator.activateSound = SoundDef.Named("Click");

                yield return designator;
            }
            else if (!IsMounted && this.Faction != Faction.OfColony)
            {
                Designator_Claim designatorClaim = new Designator_Claim();

                designatorClaim.vehicle = this;
                designatorClaim.defaultLabel = "Claim";
                designatorClaim.defaultDesc = "Claim";
                designatorClaim.icon = ContentFinder<Texture2D>.Get("UI/Commands/Claim");
                designatorClaim.activateSound = SoundDef.Named("Click");

                yield return designatorClaim;
            }

            if (this.Faction == Faction.OfColony && vehicleDef.vehicle.maxNumOfBoarding > 0 && crews.container.Count(x => x is Pawn) < vehicleDef.vehicle.maxNumOfBoarding)
            {
                Designator_Board designatorBoard = new Designator_Board();

                designatorBoard.vehicle = this;
                designatorBoard.mountPos = MountPos;
                designatorBoard.defaultLabel = "Board";
                designatorBoard.defaultDesc = "Board";
                designatorBoard.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconBoard");
                designatorBoard.activateSound = SoundDef.Named("Click");

                yield return designatorBoard;
            }

            if (this.Faction == Faction.OfColony && vehicleDef.vehicle.maxNumOfBoarding > 0 && crews.container.Count(x => x is Pawn) > 0)
            {
                Command_Action commandUnboardAll = new Command_Action();

                commandUnboardAll.defaultLabel = "UnboardAll";
                commandUnboardAll.defaultDesc = "UnboardAll";
                commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                commandUnboardAll.activateSound = SoundDef.Named("Click");
                commandUnboardAll.action = () => { crews.UnboardAll(); };

                yield return commandUnboardAll;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            if (myPawn.Faction != Faction.OfColony)
                yield break;

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            if (this.Faction == myPawn.Faction)
            {
                // order to mount
                FloatMenuOption fmoMount = new FloatMenuOption();

                fmoMount.label = "Mount on " + this.LabelBase;
                fmoMount.priority = MenuOptionPriority.High;
                fmoMount.action = () =>
                {
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"), this, MountPos);
                    myPawn.drafter.TakeOrderedJob(jobNew);
                };
                if (this.IsMounted)
                {
                    fmoMount.label = "Already mounted";
                    fmoMount.Disabled = true;
                }

                yield return fmoMount;

                if (vehicleDef.vehicle.maxNumOfBoarding > 0)
                {
                    // order to board
                    FloatMenuOption fmoBoard = new FloatMenuOption();

                    fmoBoard.label = "Board on " + this.LabelBase;
                    fmoBoard.priority = MenuOptionPriority.High;
                    fmoBoard.action = () =>
                    {
                        Job job = new Job(DefDatabase<JobDef>.GetNamed("Board"), this, MountPos);
                        myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    };
                    if (crews.CrewsCount >= vehicleDef.vehicle.maxNumOfBoarding)
                    {
                        fmoMount.label = "No space for boarding";
                        fmoMount.Disabled = true;
                    }

                    yield return fmoBoard;
                }
            }
            else
            {
                FloatMenuOption fmoClaim = new FloatMenuOption();

                fmoClaim.label = "Claim " + this.LabelBase;
                fmoClaim.priority = MenuOptionPriority.High;
                fmoClaim.action = () =>
                {
                    Job job = new Job(DefDatabase<JobDef>.GetNamed("ClaimVehicle"), this);
                    myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                };

                yield return fmoClaim;
            }
        }
        #endregion

    }
}