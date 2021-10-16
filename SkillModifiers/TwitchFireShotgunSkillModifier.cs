using System;
using EntityStates.TwitchStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace Twitch.SkillModifiers
{
	// Token: 0x0200001B RID: 27
	[SkillLevelModifier("TWITCH_PRIMARY_SHOTGUN_NAME", new Type[]
	{
		typeof(TwitchFireShotgun)
	})]
	public class TwitchFireShotgunSkillModifier : SimpleSkillModifier<TwitchFireShotgun>
	{
		// Token: 0x0600009E RID: 158 RVA: 0x0001023B File Offset: 0x0000E43B
		public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
		{
			base.OnSkillLeveledUp(level, characterBody, skillDef);
			TwitchFireShotgun.damageCoefficient = BaseSkillModifier.MultScaling(0.9f, 0.05f, level);
		}
	}
}
