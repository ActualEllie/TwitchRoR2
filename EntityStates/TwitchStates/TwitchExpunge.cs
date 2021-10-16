using System;
using EntityStates.LemurianBruiserMonster;
using RoR2;
using RoR2.Projectile;
using Twitch;
using UnityEngine;

namespace EntityStates.TwitchStates
{
	// Token: 0x02000005 RID: 5
	public class TwitchExpunge : BaseSkillState
	{
		// Token: 0x06000016 RID: 22 RVA: 0x0000311C File Offset: 0x0000131C
		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			this.fireDuration = 0.2f * this.duration;
			base.characterBody.SetAimTimer(2f);
			this.animator = base.GetModelAnimator();
			this.muzzleString = "HandL";
			this.twitchController = base.GetComponent<TwitchController>();
			base.PlayAnimation("Gesture, Override", "Expunge", "Expunge.playbackRate", this.duration);
			Util.PlaySound(Sounds.TwitchExpunge, base.gameObject);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000031B7 File Offset: 0x000013B7
		public override void OnExit()
		{
			base.OnExit();
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000031C4 File Offset: 0x000013C4
		private void FireBolt()
		{
			bool flag = !this.hasFired;
			if (flag)
			{
				this.hasFired = true;
				base.characterBody.AddSpreadBloom(1f);
				Ray aimRay = base.GetAimRay();
				EffectManager.SimpleMuzzleFlash(FireMegaFireball.muzzleflashEffectPrefab, base.gameObject, this.muzzleString, false);
				bool isAuthority = base.isAuthority;
				if (isAuthority)
				{
					ProjectileManager.instance.FireProjectile(Twitch.expungeProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, TwitchExpunge.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), 0, null, -1f);
				}
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00003280 File Offset: 0x00001480
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			bool flag = base.fixedAge >= this.fireDuration;
			if (flag)
			{
				this.FireBolt();
			}
			bool flag2 = base.fixedAge >= this.duration && base.isAuthority;
			if (flag2)
			{
				this.outer.SetNextStateToMain();
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000032DC File Offset: 0x000014DC
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 2;
		}

		// Token: 0x04000028 RID: 40
		public static float damageCoefficient = 4f;

		// Token: 0x04000029 RID: 41
		public static float damageBonus = 0.7f;

		// Token: 0x0400002A RID: 42
		public float baseDuration = 0.75f;

		// Token: 0x0400002B RID: 43
		private float duration;

		// Token: 0x0400002C RID: 44
		private float fireDuration;

		// Token: 0x0400002D RID: 45
		private bool hasFired;

		// Token: 0x0400002E RID: 46
		private Animator animator;

		// Token: 0x0400002F RID: 47
		private string muzzleString;

		// Token: 0x04000030 RID: 48
		private TwitchController twitchController;
	}
}
