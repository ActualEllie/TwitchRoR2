using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x0200001A RID: 26
	[SkillLevelModifier("TWITCH_PRIMARY_SMG_NAME", new Type[]
	{
		typeof(TwitchFireSMG)
	})]
	public class TwitchFireSMGSkillModifier : SimpleSkillModifier<TwitchFireSMG>
	{
		// Token: 0x0600009C RID: 156 RVA: 0x00010203 File Offset: 0x0000E403
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			TwitchFireSMG.projectileCount = BaseSkillModifier.AdditiveScaling(3, 1, level);
			TwitchFireSMG.baseDuration = BaseSkillModifier.MultScaling(1f, -0.1f, level);
		}
	}
}
