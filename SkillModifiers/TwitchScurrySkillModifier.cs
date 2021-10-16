using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x0200001E RID: 30
	[SkillLevelModifier("TWITCH_UTILITY_SCURRY_NAME", new Type[]
	{
		typeof(TwitchScurry)
	})]
	public class TwitchScurrySkillModifier : SimpleSkillModifier<TwitchScurry>
	{
		// Token: 0x060000A4 RID: 164 RVA: 0x000102B5 File Offset: 0x0000E4B5
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			skillDef.baseMaxStock = BaseSkillModifier.AdditiveScaling(1, 1, level);
		}
	}
}
