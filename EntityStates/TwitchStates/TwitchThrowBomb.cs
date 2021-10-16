using System;
using RoR2;
using RoR2.Projectile;
using Twitch;
using UnityEngine;

namespace EntityStates.TwitchStates
{
	// Token: 0x02000008 RID: 8
	public class TwitchThrowBomb : BaseSkillState
	{
		// Token: 0x06000029 RID: 41 RVA: 0x00003864 File Offset: 0x00001A64
		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = TwitchThrowBomb.baseDuration / this.attackSpeedStat;
			this.fireDuration = 0.35f * this.duration;
			base.characterBody.SetAimTimer(2f);
			this.animator = base.GetModelAnimator();
			base.PlayAnimation("FullBody, Override", "ThrowBomb", "ThrowBomb.playbackRate", this.duration);
			Util.PlayScaledSound(Sounds.TwitchCaskStart, base.gameObject, this.attackSpeedStat);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000038ED File Offset: 0x00001AED
		public override void OnExit()
		{
			base.OnExit();
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000038F8 File Offset: 0x00001AF8
		private void ThrowBomb()
		{
			bool flag = !this.hasFired;
			if (flag)
			{
				this.hasFired = true;
				Util.PlaySound(Sounds.TwitchThrowCask, base.gameObject);
				base.characterBody.AddSpreadBloom(1f);
				Ray aimRay = base.GetAimRay();
				bool isAuthority = base.isAuthority;
				if (isAuthority)
				{
					ProjectileManager.instance.FireProjectile(Twitch.caskProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, TwitchThrowBomb.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), 0, null, -1f);
				}
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x000039AC File Offset: 0x00001BAC
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			bool flag = base.characterMotor;
			if (flag)
			{
				bool flag2 = base.characterMotor.velocity.y < 0f && !this.hasFired;
				if (flag2)
				{
					base.characterMotor.velocity.y = 0f;
				}
			}
			bool flag3 = base.fixedAge >= this.fireDuration;
			if (flag3)
			{
				this.ThrowBomb();
			}
			bool flag4 = base.fixedAge >= this.duration && base.isAuthority;
			if (flag4)
			{
				this.outer.SetNextStateToMain();
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003A58 File Offset: 0x00001C58
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 2;
		}

		// Token: 0x04000045 RID: 69
		public static float damageCoefficient = 3f;

		// Token: 0x04000046 RID: 70
		public static float baseDuration = 1.1f;

		// Token: 0x04000047 RID: 71
		private float duration;

		// Token: 0x04000048 RID: 72
		private float fireDuration;

		// Token: 0x04000049 RID: 73
		private bool hasFired;

		// Token: 0x0400004A RID: 74
		private Animator animator;
	}
}
