using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x0200001C RID: 28
	[SkillLevelModifier("TWITCH_SECONDARY_CASK_NAME", new Type[]
	{
		typeof(TwitchThrowBomb)
	})]
	public class TwitchThrowBombSkillModifier : SimpleSkillModifier<TwitchThrowBomb>
	{
		// Token: 0x060000A0 RID: 160 RVA: 0x00010266 File Offset: 0x0000E466
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			TwitchThrowBomb.baseDuration = BaseSkillModifier.MultScaling(1.1f, -0.15f, level);
		}
	}
}
