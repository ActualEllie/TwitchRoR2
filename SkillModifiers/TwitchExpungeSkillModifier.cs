using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x02000020 RID: 32
	[SkillLevelModifier("TWITCH_SPECIAL_EXPUNGE_NAME", new Type[]
	{
		typeof(TwitchExpunge)
	})]
	public class TwitchExpungeSkillModifier : SimpleSkillModifier<TwitchExpunge>
	{
		// Token: 0x060000A8 RID: 168 RVA: 0x000102FD File Offset: 0x0000E4FD
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			TwitchExpunge.damageCoefficient = BaseSkillModifier.MultScaling(4f, 0.3f, level);
			skillDef.baseMaxStock = BaseSkillModifier.AdditiveScaling(1, 1, level);
		}
	}
}
