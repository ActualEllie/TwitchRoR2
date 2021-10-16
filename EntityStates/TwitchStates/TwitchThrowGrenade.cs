using System;
using RoR2;
using RoR2.Projectile;
using Twitch;
using UnityEngine;

namespace EntityStates.TwitchStates
{
	// Token: 0x02000009 RID: 9
	public class TwitchThrowGrenade : BaseSkillState
	{
		// Token: 0x06000030 RID: 48 RVA: 0x00003A8C File Offset: 0x00001C8C
		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration + this.throwDuration;
			this.animator = base.GetModelAnimator();
			this.twitchController = base.gameObject.GetComponent<TwitchController>();
			base.PlayAnimation("Grenade", "ReadyGrenade", "ReadyGrenade.playbackRate", this.baseDuration - 0.2f);
			Util.PlaySound(Sounds.TwitchGrenadeStart, base.gameObject);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003B04 File Offset: 0x00001D04
		public override void OnExit()
		{
			base.OnExit();
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003B10 File Offset: 0x00001D10
		private void ThrowGrenade()
		{
			bool flag = !this.hasFired;
			if (flag)
			{
				this.hasFired = true;
				Util.PlaySound(Sounds.TwitchThrowGrenade, base.gameObject);
				base.characterBody.AddSpreadBloom(1f);
				Ray aimRay = base.GetAimRay();
				bool isAuthority = base.isAuthority;
				if (isAuthority)
				{
					bool flag2 = this.twitchController;
					if (flag2)
					{
						this.twitchController.UpdateGrenadeLifetime(this.grenadeLifetime);
					}
					ProjectileManager.instance.FireProjectile(Twitch.grenadeProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), 0, null, -1f);
				}
			}
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003BE8 File Offset: 0x00001DE8
		private void ReleaseGrenade()
		{
			bool flag = !this.hasThrown;
			if (flag)
			{
				this.hasThrown = true;
				base.PlayAnimation("Grenade", "ThrowGrenade", "ThrowGrenade.playbackRate", this.throwDuration);
				float num = this.baseDuration;
				num -= base.fixedAge;
				bool flag2 = num <= 0f;
				if (flag2)
				{
					num = 0.1f;
				}
				this.grenadeLifetime = num;
				this.throwTime = base.fixedAge + this.throwOffset;
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003C68 File Offset: 0x00001E68
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			base.characterBody.SetAimTimer(0.5f);
			bool flag = base.fixedAge >= this.baseDuration;
			if (flag)
			{
				TwitchExplode nextState = new TwitchExplode();
				this.outer.SetNextState(nextState);
			}
			bool flag2 = base.inputBank;
			if (flag2)
			{
				bool flag3 = !base.inputBank.skill2.down;
				if (flag3)
				{
					this.ReleaseGrenade();
				}
			}
			bool flag4 = base.fixedAge >= this.throwTime && this.hasThrown;
			if (flag4)
			{
				this.ThrowGrenade();
			}
			bool flag5 = this.hasFired && base.isAuthority;
			if (flag5)
			{
				this.outer.SetNextStateToMain();
			}
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003D34 File Offset: 0x00001F34
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 2;
		}

		// Token: 0x0400004B RID: 75
		public float damageCoefficient = 7.5f;

		// Token: 0x0400004C RID: 76
		public float baseDuration = 3f;

		// Token: 0x0400004D RID: 77
		public float throwDuration = 0.25f;

		// Token: 0x0400004E RID: 78
		public float throwOffset = 0.1f;

		// Token: 0x0400004F RID: 79
		private float duration;

		// Token: 0x04000050 RID: 80
		private bool hasFired;

		// Token: 0x04000051 RID: 81
		private bool hasThrown;

		// Token: 0x04000052 RID: 82
		private Animator animator;

		// Token: 0x04000053 RID: 83
		private TwitchController twitchController;

		// Token: 0x04000054 RID: 84
		private float grenadeLifetime;

		// Token: 0x04000055 RID: 85
		private float throwTime;
	}
}
