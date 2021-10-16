using System;
using EntityStates.ClayBruiser.Weapon;
using EntityStates.Commando.CommandoWeapon;
using EntityStates.GolemMonster;
using RoR2;
using RoR2.Projectile;
using Twitch;
using UnityEngine;

namespace EntityStates.TwitchStates
{
	// Token: 0x02000002 RID: 2
	public class TwitchFireBolt : BaseSkillState
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			this.fireDuration = 0.15f * this.duration;
			base.characterBody.SetAimTimer(2f);
			this.animator = base.GetModelAnimator();
			this.muzzleString = "Muzzle";
			this.twitchController = base.GetComponent<TwitchController>();
			bool flag = this.twitchController;
			if (flag)
			{
				this.muzzleString = this.twitchController.GetMuzzleName();
			}
			bool flag2 = base.characterBody.HasBuff(Twitch.ambushBuff);
			if (flag2)
			{
				base.PlayAnimation("Gesture, Override", "FireEmpoweredBolt", "FireBolt.playbackRate", 1.5f * this.duration);
			}
			else
			{
				base.PlayAnimation("Gesture, Override", "FireBolt", "FireBolt.playbackRate", 2f * this.duration);
			}
			bool flag3 = base.characterBody.HasBuff(Twitch.ambushBuff) && this.twitchController;
			if (flag3)
			{
				this.twitchController.AmbushAttack();
			}
			else
			{
				Util.PlaySound(Sounds.TwitchAttackStart, base.gameObject);
			}
		}

		// Token: 0x06000002 RID: 2 RVA: 0x0000217F File Offset: 0x0000037F
		public override void OnExit()
		{
			base.OnExit();
		}

		// Token: 0x06000003 RID: 3 RVA: 0x0000218C File Offset: 0x0000038C
		private void FireBolt()
		{
			bool flag = !this.hasFired;
			if (flag)
			{
				this.hasFired = true;
				bool flag2 = base.characterBody.HasBuff(Twitch.ambushBuff);
				if (flag2)
				{
					Util.PlaySound(Sounds.TwitchAttackLaser, base.gameObject);
					EffectManager.SimpleMuzzleFlash(FireLaser.effectPrefab, base.gameObject, this.muzzleString, false);
					base.AddRecoil(-2f * this.recoil, -3f * this.recoil, -1f * this.recoil, 1f * this.recoil);
					bool isAuthority = base.isAuthority;
					if (isAuthority)
					{
						float damage = TwitchFireBolt.damageCoefficient * this.damageStat;
						float force = 50f;
						float procCoefficient = 1f;
						bool isCrit = base.RollCrit();
						Ray aimRay = base.GetAimRay();
						BulletAttack bulletAttack = new BulletAttack();
						bulletAttack.bulletCount = 1U;
						bulletAttack.aimVector = aimRay.direction;
						bulletAttack.origin = aimRay.origin;
						bulletAttack.damage = damage;
						bulletAttack.damageColorIndex = 0;
						bulletAttack.damageType = 1048576;
						bulletAttack.falloffModel = 0;
						bulletAttack.maxDistance = 512f;
						bulletAttack.force = force;
						bulletAttack.hitMask = LayerIndex.CommonMasks.bullet;
						bulletAttack.minSpread = 0f;
						bulletAttack.maxSpread = 3f;
						bulletAttack.isCrit = isCrit;
						bulletAttack.owner = base.gameObject;
						bulletAttack.muzzleName = this.muzzleString;
						bulletAttack.smartCollision = false;
						bulletAttack.procChainMask = default(ProcChainMask);
						bulletAttack.procCoefficient = procCoefficient;
						bulletAttack.radius = 1f;
						bulletAttack.sniper = false;
						LayerIndex background = LayerIndex.background;
						bulletAttack.stopperMask = background.collisionMask;
						bulletAttack.weapon = null;
						bulletAttack.tracerEffectPrefab = TwitchFireBolt.tracerEffectPrefab;
						bulletAttack.spreadPitchScale = 0.25f;
						bulletAttack.spreadYawScale = 0.25f;
						bulletAttack.queryTriggerInteraction = 0;
						bulletAttack.hitEffectPrefab = MinigunFire.bulletHitEffectPrefab;
						bulletAttack.HitEffectNormal = MinigunFire.bulletHitEffectNormal;
						bulletAttack.Fire();
					}
				}
				else
				{
					Util.PlaySound(Sounds.TwitchAttack, base.gameObject);
					base.characterBody.AddSpreadBloom(0.75f);
					Ray aimRay2 = base.GetAimRay();
					EffectManager.SimpleMuzzleFlash(FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);
					bool isAuthority2 = base.isAuthority;
					if (isAuthority2)
					{
						ProjectileManager.instance.FireProjectile(Twitch.boltProjectile, aimRay2.origin, Util.QuaternionSafeLookRotation(aimRay2.direction), base.gameObject, TwitchFireBolt.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), 0, null, TwitchFireBolt.projectileSpeed);
					}
				}
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x0000243C File Offset: 0x0000063C
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

		// Token: 0x06000005 RID: 5 RVA: 0x00002498 File Offset: 0x00000698
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 1;
		}

		// Token: 0x04000001 RID: 1
		public static float damageCoefficient = 2.25f;

		// Token: 0x04000002 RID: 2
		public float baseDuration = 0.75f;

		// Token: 0x04000003 RID: 3
		public float recoil = 1f;

		// Token: 0x04000004 RID: 4
		public static float projectileSpeed = 120f;

		// Token: 0x04000005 RID: 5
		public static GameObject tracerEffectPrefab = Twitch.laserTracer;

		// Token: 0x04000006 RID: 6
		private float duration;

		// Token: 0x04000007 RID: 7
		private float fireDuration;

		// Token: 0x04000008 RID: 8
		private bool hasFired;

		// Token: 0x04000009 RID: 9
		private Animator animator;

		// Token: 0x0400000A RID: 10
		private string muzzleString;

		// Token: 0x0400000B RID: 11
		private TwitchController twitchController;
	}
}
