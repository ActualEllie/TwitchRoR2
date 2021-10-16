using System;
using EntityStates.ClayBruiser.Weapon;
using EntityStates.Commando.CommandoWeapon;
using EntityStates.GolemMonster;
using RoR2;
using Twitch;
using UnityEngine;

namespace EntityStates.TwitchStates
{
	// Token: 0x02000003 RID: 3
	public class TwitchFireSMG : BaseSkillState
	{
		// Token: 0x06000008 RID: 8 RVA: 0x000024EC File Offset: 0x000006EC
		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = TwitchFireSMG.baseDuration / this.attackSpeedStat;
			this.fireDuration = 0.2f * this.duration;
			base.characterBody.SetAimTimer(2f);
			this.animator = base.GetModelAnimator();
			this.muzzleString = "Muzzle";
			this.hasFired = 0;
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

		// Token: 0x06000009 RID: 9 RVA: 0x00002621 File Offset: 0x00000821
		public override void OnExit()
		{
			base.OnExit();
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000262C File Offset: 0x0000082C
		private void FireBullet()
		{
			bool flag = this.hasFired < TwitchFireSMG.projectileCount;
			if (flag)
			{
				bool flag2 = this.hasFired == 0 && !base.characterBody.HasBuff(Twitch.ambushBuff);
				if (flag2)
				{
					Util.PlaySound(Sounds.TwitchAttackGun, base.gameObject);
				}
				this.hasFired++;
				this.lastFired = Time.time + this.fireInterval / this.attackSpeedStat;
				bool flag3 = base.characterBody.HasBuff(Twitch.ambushBuff);
				if (flag3)
				{
					Util.PlaySound(Sounds.TwitchAttackGunLaser, base.gameObject);
					EffectManager.SimpleMuzzleFlash(FireLaser.effectPrefab, base.gameObject, this.muzzleString, false);
					base.AddRecoil(-2f * this.beamRecoil, -3f * this.beamRecoil, -1f * this.beamRecoil, 1f * this.beamRecoil);
					bool isAuthority = base.isAuthority;
					if (isAuthority)
					{
						float damage = this.damageCoefficient * this.damageStat;
						float force = 0f;
						float procCoefficient = 0.75f;
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
						bulletAttack.maxSpread = 10f;
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
						bulletAttack.tracerEffectPrefab = TwitchFireSMG.beamTracerEffectPrefab;
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
					base.AddRecoil(-2f * this.bulletRecoil, -3f * this.bulletRecoil, -1f * this.bulletRecoil, 1f * this.bulletRecoil);
					base.characterBody.AddSpreadBloom(0.33f * this.bulletRecoil);
					EffectManager.SimpleMuzzleFlash(FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);
					bool isAuthority2 = base.isAuthority;
					if (isAuthority2)
					{
						float damage2 = this.damageCoefficient * this.damageStat;
						float force2 = 10f;
						float procCoefficient2 = 0.75f;
						bool isCrit2 = base.RollCrit();
						Ray aimRay2 = base.GetAimRay();
						new BulletAttack
						{
							bulletCount = 1U,
							aimVector = aimRay2.direction,
							origin = aimRay2.origin,
							damage = damage2,
							damageColorIndex = 0,
							damageType = 1048576,
							falloffModel = 1,
							maxDistance = 256f,
							force = force2,
							hitMask = LayerIndex.CommonMasks.bullet,
							minSpread = 0f,
							maxSpread = 10f,
							isCrit = isCrit2,
							owner = base.gameObject,
							muzzleName = this.muzzleString,
							smartCollision = false,
							procChainMask = default(ProcChainMask),
							procCoefficient = procCoefficient2,
							radius = 0.75f,
							sniper = false,
							stopperMask = LayerIndex.CommonMasks.bullet,
							weapon = null,
							tracerEffectPrefab = TwitchFireSMG.bulletTracerEffectPrefab,
							spreadPitchScale = 0.25f,
							spreadYawScale = 0.25f,
							queryTriggerInteraction = 0,
							hitEffectPrefab = MinigunFire.bulletHitEffectPrefab,
							HitEffectNormal = MinigunFire.bulletHitEffectNormal
						}.Fire();
					}
				}
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002A54 File Offset: 0x00000C54
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			bool flag = base.fixedAge >= this.fireDuration && Time.time > this.lastFired;
			if (flag)
			{
				this.FireBullet();
			}
			bool flag2 = base.fixedAge >= this.duration && base.isAuthority;
			if (flag2)
			{
				this.outer.SetNextStateToMain();
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002AC0 File Offset: 0x00000CC0
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 1;
		}

		// Token: 0x0400000C RID: 12
		public float damageCoefficient = 0.85f;

		// Token: 0x0400000D RID: 13
		public static float baseDuration = 1f;

		// Token: 0x0400000E RID: 14
		public float fireInterval = 0.1f;

		// Token: 0x0400000F RID: 15
		public static int projectileCount = 3;

		// Token: 0x04000010 RID: 16
		public float bulletRecoil = 0.75f;

		// Token: 0x04000011 RID: 17
		public float beamRecoil = 1f;

		// Token: 0x04000012 RID: 18
		public static GameObject bulletTracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerEngiTurret");

		// Token: 0x04000013 RID: 19
		public static GameObject beamTracerEffectPrefab = Twitch.laserTracer;

		// Token: 0x04000014 RID: 20
		private float duration;

		// Token: 0x04000015 RID: 21
		private float fireDuration;

		// Token: 0x04000016 RID: 22
		private int hasFired;

		// Token: 0x04000017 RID: 23
		private float lastFired;

		// Token: 0x04000018 RID: 24
		private Animator animator;

		// Token: 0x04000019 RID: 25
		private string muzzleString;

		// Token: 0x0400001A RID: 26
		private TwitchController twitchController;
	}
}
