using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
//using Verse.Sound;       // Needed when you do something with Sound
//using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
//using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace ToolsForHaul
{
    class Itab_Pawn_Vehicle_Storage : ITab_Storage
    {
        private const float TopAreaHeight = 35f;
        private Vector2 scrollPosition;
        private static readonly Vector2 WinSize;

        static Itab_Pawn_Vehicle_Storage()
        {
            Itab_Pawn_Vehicle_Storage.WinSize = new Vector2(300f, 480f);
        }

		public override bool IsVisible
		{
			get 
            {
                Vehicle_Cart cart = Enumerable.First<object>(Find.Selector.SelectedObjects) as Vehicle_Cart;
                return (cart != null)? true : false;
            }
		}

        protected override void FillTab()
        {
            ThingFilter allowances;
            ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.StorageTab, KnowledgeAmount.GuiFrame);
            ConceptDecider.TeachOpportunity(ConceptDefOf.StorageTabCategories, OpportunityType.GuiFrame);
            ConceptDecider.TeachOpportunity(ConceptDefOf.StoragePriority, OpportunityType.GuiFrame);
            allowances = ((Vehicle_Cart)Enumerable.First<object>(Find.Selector.SelectedObjects)).allowances;
            Rect position = GenUI.ContractedBy(new Rect(0.0f, 0.0f, Itab_Pawn_Vehicle_Storage.WinSize.x, Itab_Pawn_Vehicle_Storage.WinSize.y), 10f);
            GUI.BeginGroup(position);

            ThingFilterUI.DoThingFilterConfigWindow(new Rect(0.0f, 35f, position.width, position.height - 35f), ref this.scrollPosition, allowances, null);
            GUI.EndGroup();
        }
    }
}