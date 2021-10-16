using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x0200001D RID: 29
	[SkillLevelModifier("TWITCH_UTILITY_AMBUSH_NAME", new Type[]
	{
		typeof(TwitchAmbush)
	})]
	public class TwitchAmbushSkillModifier : SimpleSkillModifier<TwitchAmbush>
	{
		// Token: 0x060000A2 RID: 162 RVA: 0x00010291 File Offset: 0x0000E491
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			TwitchAmbush.duration = (float)BaseSkillModifier.AdditiveScaling(6, 2, level);
		}
	}
}
