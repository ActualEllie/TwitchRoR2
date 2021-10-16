using System;
using RoR2;
using Twitch;
using UnityEngine;

namespace EntityStates.TwitchStates
{
	// Token: 0x0200000A RID: 10
	public class TwitchExplode : BaseSkillState
	{
		// Token: 0x06000037 RID: 55 RVA: 0x00003D7C File Offset: 0x00001F7C
		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			this.animator = base.GetModelAnimator();
			base.PlayAnimation("Grenade", "ThrowGrenade", "ThrowGrenade.playbackRate", this.duration);
			this.Explode();
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00003DD3 File Offset: 0x00001FD3
		public override void OnExit()
		{
			base.OnExit();
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003DE0 File Offset: 0x00001FE0
		private void Explode()
		{
			Util.PlaySound(Sounds.TwitchCaskHit, base.gameObject);
			bool flag = base.healthComponent;
			if (flag)
			{
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = base.healthComponent.fullCombinedHealth * 0.5f;
				damageInfo.position = base.characterBody.corePosition;
				damageInfo.force = Vector3.zero;
				damageInfo.damageColorIndex = 5;
				damageInfo.crit = false;
				damageInfo.attacker = null;
				damageInfo.inflictor = null;
				damageInfo.damageType = 32;
				damageInfo.procCoefficient = 0f;
				damageInfo.procChainMask = default(ProcChainMask);
				base.healthComponent.TakeDamage(damageInfo);
			}
			BlastAttack blastAttack = new BlastAttack
			{
				attacker = base.gameObject,
				inflictor = base.gameObject,
				teamIndex = 1,
				baseForce = 50f,
				position = base.characterBody.corePosition,
				radius = 12f,
				falloffModel = 1,
				crit = base.RollCrit(),
				baseDamage = 5f * this.damageStat,
				procCoefficient = 1f
			};
			blastAttack.Fire();
			EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/MagmaOrbExplosion"), new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = 12f
			}, true);
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003F4C File Offset: 0x0000214C
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			bool flag = base.fixedAge >= this.duration && base.isAuthority;
			if (flag)
			{
				this.outer.SetNextStateToMain();
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003F8C File Offset: 0x0000218C
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 0;
		}

		// Token: 0x04000056 RID: 86
		public float baseDuration = 2f;

		// Token: 0x04000057 RID: 87
		private float duration;

		// Token: 0x04000058 RID: 88
		private Animator animator;
	}
}
