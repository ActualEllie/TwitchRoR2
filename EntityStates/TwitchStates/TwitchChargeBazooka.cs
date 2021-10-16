using System;
using EntityStates.LemurianBruiserMonster;
using RoR2;
using RoR2.Projectile;
using Twitch;
using UnityEngine;

namespace EntityStates.TwitchStates
{
	// Token: 0x02000006 RID: 6
	public class TwitchChargeBazooka : BaseSkillState
	{
		// Token: 0x0600001D RID: 29 RVA: 0x0000331C File Offset: 0x0000151C
		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.minimumDuration / this.attackSpeedStat;
			this.releaseDuration = this.maximumDuration / this.attackSpeedStat;
			this.animator = base.GetModelAnimator();
			this.muzzleString = "Muzzle";
			this.twitchController = base.GetComponent<TwitchController>();
			this.hasFired = false;
			bool flag = this.twitchController;
			if (flag)
			{
				this.muzzleString = this.twitchController.GetMuzzleName();
			}
			Transform modelTransform = base.GetModelTransform();
			bool flag2 = modelTransform;
			if (flag2)
			{
				ChildLocator component = modelTransform.GetComponent<ChildLocator>();
				bool flag3 = component;
				if (flag3)
				{
					Transform transform = component.FindChild(this.muzzleString);
					bool flag4 = transform && ChargeMegaFireball.chargeEffectPrefab;
					if (flag4)
					{
						this.chargeInstance = Object.Instantiate<GameObject>(ChargeMegaFireball.chargeEffectPrefab, transform.position, transform.rotation);
						this.chargeInstance.transform.parent = transform;
						this.chargeInstance.transform.localScale *= 0.25f;
						this.chargeInstance.transform.localPosition = Vector3.zero;
						ScaleParticleSystemDuration component2 = this.chargeInstance.GetComponent<ScaleParticleSystemDuration>();
						bool flag5 = component2;
						if (flag5)
						{
							component2.newDuration = this.releaseDuration;
						}
					}
				}
			}
			Util.PlayScaledSound(Sounds.TwitchCharge, base.gameObject, this.attackSpeedStat);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000034A8 File Offset: 0x000016A8
		public override void OnExit()
		{
			base.OnExit();
			bool flag = this.chargeInstance;
			if (flag)
			{
				EntityState.Destroy(this.chargeInstance);
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000034DC File Offset: 0x000016DC
		private void FireBazooka()
		{
			bool flag = !this.hasFired;
			if (flag)
			{
				float num = (base.fixedAge - this.duration) / this.releaseDuration;
				Util.PlaySound(Sounds.TwitchAttackBazooka, base.gameObject);
				EffectManager.SimpleMuzzleFlash(FireMegaFireball.muzzleflashEffectPrefab, base.gameObject, this.muzzleString, true);
				bool flag2 = num >= 0.75f;
				if (flag2)
				{
					base.PlayAnimation("Gesture, Override", "FireEmpoweredBolt", "FireBolt.playbackRate", this.duration * 2f);
				}
				else
				{
					base.PlayAnimation("Gesture, Override", "FireExplosive", "FireExplosive.playbackRate", this.duration * 2f);
				}
				Ray aimRay = base.GetAimRay();
				bool isAuthority = base.isAuthority;
				if (isAuthority)
				{
					float num2 = Mathf.Lerp(this.minDamageCoefficient, this.maxDamageCoefficient, num);
					float num3 = Mathf.Lerp(this.minProcCoefficient, this.maxProcCoefficient, num);
					float num4 = Mathf.Lerp(this.minSpeed, this.maxSpeed, num);
					float num5 = Mathf.Lerp(this.minRecoil, this.maxRecoil, num);
					base.AddRecoil(-2f * num5, -3f * num5, -1f * num5, 1f * num5);
					base.characterBody.AddSpreadBloom(0.33f * num5);
					ProjectileManager.instance.FireProjectile(Twitch.bazookaProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, num2 * this.damageStat, this.force, Util.CheckRoll(this.critStat, base.characterBody.master), 0, null, num4);
				}
				TwitchFireBazooka nextState = new TwitchFireBazooka();
				this.outer.SetNextState(nextState);
			}
			this.hasFired = true;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000036A4 File Offset: 0x000018A4
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			base.characterBody.SetAimTimer(0.5f);
			bool flag = base.fixedAge >= this.releaseDuration;
			if (flag)
			{
				this.FireBazooka();
			}
			bool flag2 = base.inputBank;
			if (flag2)
			{
				bool flag3 = base.fixedAge >= this.duration && base.isAuthority && !base.inputBank.skill1.down;
				if (flag3)
				{
					this.FireBazooka();
				}
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00003734 File Offset: 0x00001934
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 2;
		}

		// Token: 0x04000031 RID: 49
		public float maximumDuration = 2.25f;

		// Token: 0x04000032 RID: 50
		public float minimumDuration = 0.5f;

		// Token: 0x04000033 RID: 51
		public float maxDamageCoefficient = 22.5f;

		// Token: 0x04000034 RID: 52
		public float minDamageCoefficient = 1.5f;

		// Token: 0x04000035 RID: 53
		public float maxProcCoefficient = 0.8f;

		// Token: 0x04000036 RID: 54
		public float minProcCoefficient = 0.1f;

		// Token: 0x04000037 RID: 55
		public float maxSpeed = 200f;

		// Token: 0x04000038 RID: 56
		public float minSpeed = 10f;

		// Token: 0x04000039 RID: 57
		public float maxRecoil = 15f;

		// Token: 0x0400003A RID: 58
		public float minRecoil = 0.5f;

		// Token: 0x0400003B RID: 59
		public float force = 500f;

		// Token: 0x0400003C RID: 60
		private float releaseDuration;

		// Token: 0x0400003D RID: 61
		private float duration;

		// Token: 0x0400003E RID: 62
		private bool hasFired;

		// Token: 0x0400003F RID: 63
		private Animator animator;

		// Token: 0x04000040 RID: 64
		private string muzzleString;

		// Token: 0x04000041 RID: 65
		private TwitchController twitchController;

		// Token: 0x04000042 RID: 66
		private GameObject chargeInstance;
	}
}
