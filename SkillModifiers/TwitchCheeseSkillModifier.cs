using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x0200001F RID: 31
	[SkillLevelModifier("TWITCH_UTILITY_CHEESE_NAME", new Type[]
	{
		typeof(TwitchCheese)
	})]
	public class TwitchCheeseSkillModifier : SimpleSkillModifier<TwitchCheese>
	{
		// Token: 0x060000A6 RID: 166 RVA: 0x000102D9 File Offset: 0x0000E4D9
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			skillDef.baseMaxStock = BaseSkillModifier.AdditiveScaling(1, 1, level);
		}
	}
}
