
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace ToolsForHaul
{
    public class Gizmo_BackpackEquipment : Gizmo
    {
        private const string txtNoDoctor = "NoDoctor";
        private const string txtCannotEatAnymore = "CannotEatAnymore";

        private const string txtNoItem = "NoItem";

        //Links
        public Apparel_Backpack backpack;

        //Constants
        private static readonly Texture2D FilledTex = SolidColorMaterials.NewSolidColorTexture(1f, 1f, 1f, 0.10f);
        private static readonly Texture2D EmptyTex = SolidColorMaterials.NewSolidColorTexture(0.4f, 0.4f, 0.4f, 0.15f);
        private static readonly Texture2D NoAvailableTex = SolidColorMaterials.NewSolidColorTexture(0.0f, 0.0f, 0.0f, 1.0f);
        //private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        //private static readonly ThingCategoryDef weaponMelee = DefDatabase<ThingCategoryDef>.GetNamed("WeaponsMelee");
        //private static readonly ThingCategoryDef weaponRanged = DefDatabase<ThingCategoryDef>.GetNamed("WeaponsRanged");
        private static readonly ThingCategoryDef medicine = DefDatabase<ThingCategoryDef>.GetNamed("Medicine");
        private static readonly ThingCategoryDef foodMeals = DefDatabase<ThingCategoryDef>.GetNamed("FoodMeals");
        

        //Properties
        private const int textHeight = 30;
        private const int textWidth = 60;

        //EquipmentSlot Properties
        private const int numOfRow = 2;
        private const int numOfMaxItemsPerRow = 4;
        private const float widthPerItem = 140.0f / 4.0f;
        private const float curWidth = widthPerItem * numOfMaxItemsPerRow;
        public override float Width { get { return curWidth; } }

        //IconClickSound
        private SoundDef thingIconSound;



        public override GizmoResult GizmoOnGUI(UnityEngine.Vector2 topLeft)
        {
            #if DEBUG
            Log.Message("In " + System.Reflection.MethodBase.GetCurrentMethod() + " Memory usage: " + GC.GetTotalMemory(false));
            #endif
            Rect overRect = new Rect(topLeft.x, topLeft.y, curWidth, Height);
            Widgets.DrawWindowBackground(overRect);

            //Equipment slot
            Pawn wearer = backpack.wearer;
            ThingWithComps dummy;

            Rect thingIconRect = new Rect(topLeft.x, topLeft.y, widthPerItem, Height / 2);
            int numOfCurItem = 0;

            List<Thing> things = wearer.inventory.container.ToList();

            //Draw Gizmo
            for (int i = 0;i < numOfMaxItemsPerRow * numOfRow; i++)
            {
                if ( i >= backpack.maxItem )
                {
                    thingIconRect.x = topLeft.x + widthPerItem * (i % numOfMaxItemsPerRow);
                    thingIconRect.y = topLeft.y + (Height / 2) * (i / numOfMaxItemsPerRow);
                    Widgets.DrawTextureFitted(thingIconRect, NoAvailableTex, 1.0f);
                    continue;
                }

                if (i >= things.Count)
                {
                    thingIconRect.x = topLeft.x + widthPerItem * (i % numOfMaxItemsPerRow);
                    thingIconRect.y = topLeft.y + (Height / 2) * (i / numOfMaxItemsPerRow);
                    Widgets.DrawTextureFitted(thingIconRect, EmptyTex, 1.0f);
                    continue;
                }

                Thing item = things[i];
                Widgets.DrawBox(thingIconRect, 1);
                Widgets.ThingIcon(thingIconRect, item);
                if (thingIconRect.Contains(Event.current.mousePosition))
                {
                    Widgets.DrawTextureFitted(thingIconRect, FilledTex, 1.0f);
                }
                //Interaction with item
                if (Widgets.InvisibleButton(thingIconRect))
                {
                    thingIconSound = SoundDefOf.Click;
                    if (Event.current.button == 0)
                    {
                        //Weapon
                        if (item.def.equipmentType == EquipmentType.Primary)
                        {
                            if (wearer.equipment.Primary != null)
                                wearer.equipment.TryTransferEquipmentToContainer(wearer.equipment.Primary, wearer.inventory.container, out dummy);
                            wearer.equipment.AddEquipment(item as ThingWithComps);
                            wearer.inventory.container.Remove(item as ThingWithComps);
                            if (wearer.jobs.curJob != null)
                                wearer.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                        //Medicine
                        else if (item.def.thingCategories.Contains(medicine))
                        {
                            if (wearer.workSettings.WorkIsActive(WorkTypeDefOf.Doctor))
                            {
                                Designator_ApplyMedicine designator = new Designator_ApplyMedicine();
                                designator.medicine = item;
                                designator.doctor = wearer;
                                designator.icon = item.def.uiIcon;
                                designator.activateSound = SoundDef.Named("Click");

                                DesignatorManager.Select(designator);
                            }
                            else
                            {
                                Messages.Message(txtNoDoctor.Translate(), MessageSound.RejectInput);
                                Messages.Update();
                                thingIconSound = SoundDefOf.ClickReject;
                            }
                        }
                        //Food
                        else if (item.def.thingCategories.Contains(foodMeals))
                        {
                            if (wearer.needs.food.CurCategory != HungerCategory.Fed)
                            {
                                Job jobNew = new Job(JobDefOf.Ingest, item);
                                jobNew.maxNumToCarry = 1;
                                jobNew.ignoreForbidden = true;
                                wearer.drafter.TakeOrderedJob(jobNew);
                            }
                            else
                            {
                                Messages.Message(txtCannotEatAnymore.Translate(), MessageSound.RejectInput);
                                Messages.Update();
                                thingIconSound = SoundDefOf.ClickReject;
                            }
                        }
                        //Apparel
                        else if (item is Apparel)
                        {
                            //if (!wearer.apparel.CanWearWithoutDroppingAnything(item.def))
                            //    wearer.apparel.WornApparel.Find(apparel => apparel.def.apparel.layers.Any);
                            for (int index = wearer.apparel.WornApparel.Count - 1; index >= 0; --index)
                            {
                                Apparel ap = wearer.apparel.WornApparel[index];
                                if (!ApparelUtility.CanWearTogether(item.def, ap.def))
                                {
                                    Apparel resultingAp;
                                    wearer.apparel.TryDrop(ap, out resultingAp, wearer.Position, false);
                                    wearer.inventory.container.TryAdd(resultingAp);
                                }
                            }
                            wearer.apparel.Wear(item as Apparel);
                            wearer.inventory.container.Remove(item as ThingWithComps);
                        }
                        //Installing minifiedThing
                        else if (item is MinifiedThing)
                        {
                            Designator_InstallForBackpack install = new Designator_InstallForBackpack();
                            install.miniThing = item;
                            install.wearer = wearer;

                            DesignatorManager.Select(install);
                        }

                        else
                        {
                            //Add another type of item you want to interact
                        }
                    }
                    else if (Event.current.button == 1)
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        options.Add(new FloatMenuOption(Translator.Translate("ThingInfo"), () =>
                        {
                            Find.WindowStack.Add((Window)new Dialog_InfoCard(item));
                        }));
                        options.Add(new FloatMenuOption(Translator.Translate("DropThing"), () =>
                        {
                            Thing dummy1;
                            wearer.inventory.container.TryDrop(item, wearer.Position, ThingPlaceMode.Near, out dummy1);
                        }));

                        Find.WindowStack.Add((Window)new FloatMenu(options, item.LabelCap, false, false));
                    }

                    SoundStarter.PlayOneShotOnCamera(thingIconSound);
                }

                numOfCurItem++;
                thingIconRect.x = topLeft.x + widthPerItem * (numOfCurItem % numOfMaxItemsPerRow);
                thingIconRect.y = topLeft.y + (Height / 2) * (numOfCurItem / numOfMaxItemsPerRow);
            }

            if (numOfCurItem == 0 )
            {
                Rect textRect = new Rect(topLeft.x + Width / 2 - textWidth / 2, topLeft.y + Height / 2 - textHeight / 2, textWidth, textHeight);
                Widgets.Label(textRect, txtNoItem.Translate());
            }         

            #if DEBUG
            Log.Message("In End of " + System.Reflection.MethodBase.GetCurrentMethod() + " Memory usage: " + GC.GetTotalMemory(false));
            #endif
            return new GizmoResult(GizmoState.Clear);
        }


    }

}