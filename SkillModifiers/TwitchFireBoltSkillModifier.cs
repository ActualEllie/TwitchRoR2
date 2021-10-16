using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x02000019 RID: 25
	[SkillLevelModifier("TWITCH_PRIMARY_CROSSBOW_NAME", new Type[]
	{
		typeof(TwitchFireBolt)
	})]
	public class TwitchFireBoltSkillModifier : SimpleSkillModifier<TwitchFireBolt>
	{
		// Token: 0x0600009A RID: 154 RVA: 0x000101C3 File Offset: 0x0000E3C3
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			TwitchFireBolt.damageCoefficient = BaseSkillModifier.MultScaling(2.25f, 0.15f, level);
			TwitchFireBolt.projectileSpeed = BaseSkillModifier.MultScaling(120f, 0.1f, level);
		}
	}
}
