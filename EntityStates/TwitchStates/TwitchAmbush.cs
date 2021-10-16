using System;
using EntityStates.Commando.CommandoWeapon;
using RoR2;
using Twitch;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.TwitchStates
{
	// Token: 0x0200000C RID: 12
	public class TwitchAmbush : BaseState
	{
		// Token: 0x06000043 RID: 67 RVA: 0x0000410C File Offset: 0x0000230C
		public override void OnEnter()
		{
			base.OnEnter();
			this.animator = base.GetModelAnimator();
			this.modelTransform = base.GetModelTransform();
			this.twitchController = base.GetComponent<TwitchController>();
			this.CastSmoke();
			bool flag = base.characterBody && NetworkServer.active;
			if (flag)
			{
				base.characterBody.AddBuff(7);
				base.characterBody.AddBuff(8);
			}
			bool flag2 = this.twitchController;
			if (flag2)
			{
				this.twitchController.EnterStealth();
			}
			bool flag3 = base.skillLocator;
			if (flag3)
			{
				base.skillLocator.utility.SetSkillOverride(base.skillLocator.utility, Twitch.ambushActiveDef, 3);
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000041D0 File Offset: 0x000023D0
		public override void OnExit()
		{
			bool flag = base.characterBody && NetworkServer.active;
			if (flag)
			{
				bool flag2 = base.characterBody.HasBuff(7);
				if (flag2)
				{
					base.characterBody.RemoveBuff(7);
				}
				bool flag3 = base.characterBody.HasBuff(8);
				if (flag3)
				{
					base.characterBody.RemoveBuff(8);
				}
				base.characterBody.AddTimedBuff(Twitch.ambushBuff, 5f);
				base.characterBody.RecalculateStats();
				bool flag4 = this.modelTransform;
				if (flag4)
				{
					TemporaryOverlay temporaryOverlay = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
					temporaryOverlay.duration = 5f;
					temporaryOverlay.animateShaderAlpha = true;
					temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 7.5f, 0f);
					temporaryOverlay.destroyComponentOnEnd = true;
					temporaryOverlay.originalMaterial = Resources.Load<Material>("Materials/matPoisoned");
					temporaryOverlay.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
				}
			}
			bool flag5 = this.twitchController;
			if (flag5)
			{
				this.twitchController.GetAmbushBuff(5f);
			}
			bool flag6 = !this.outer.destroying;
			if (flag6)
			{
				this.CastSmoke();
			}
			bool flag7 = TwitchAmbush.destealthMaterial;
			if (flag7)
			{
				TemporaryOverlay temporaryOverlay2 = this.animator.gameObject.AddComponent<TemporaryOverlay>();
				temporaryOverlay2.duration = 1f;
				temporaryOverlay2.destroyComponentOnEnd = true;
				temporaryOverlay2.originalMaterial = TwitchAmbush.destealthMaterial;
				temporaryOverlay2.inspectorCharacterModel = this.animator.gameObject.GetComponent<CharacterModel>();
				temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlay2.animateShaderAlpha = true;
			}
			bool flag8 = this.twitchController;
			if (flag8)
			{
				this.twitchController.ExitStealth();
			}
			bool flag9 = base.skillLocator;
			if (flag9)
			{
				base.skillLocator.utility.UnsetSkillOverride(base.skillLocator.utility, Twitch.ambushActiveDef, 3);
			}
			base.OnExit();
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000043FC File Offset: 0x000025FC
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			this.stopwatch += Time.fixedDeltaTime;
			bool flag = this.stopwatch >= TwitchAmbush.duration && base.isAuthority;
			if (flag)
			{
				this.outer.SetNextStateToMain();
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x0000444C File Offset: 0x0000264C
		private void CastSmoke()
		{
			bool flag = !this.hasCastSmoke;
			if (flag)
			{
				Util.PlaySound(Sounds.TwitchEnterStealth, base.gameObject);
				this.hasCastSmoke = true;
				bool flag2 = this.animator;
				if (flag2)
				{
					this.animator.SetBool("isSneaking", true);
				}
			}
			else
			{
				Util.PlaySound(Sounds.TwitchExitStealth, base.gameObject);
				bool flag3 = this.animator;
				if (flag3)
				{
					this.animator.SetBool("isSneaking", false);
				}
			}
			EffectManager.SpawnEffect(CastSmokescreenNoDelay.smokescreenEffectPrefab, new EffectData
			{
				origin = base.transform.position
			}, false);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000044FC File Offset: 0x000026FC
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			bool flag = this.stopwatch <= TwitchAmbush.minimumStateDuration;
			InterruptPriority result;
			if (flag)
			{
				result = 2;
			}
			else
			{
				result = 0;
			}
			return result;
		}

		// Token: 0x0400005E RID: 94
		public static float duration = 6f;

		// Token: 0x0400005F RID: 95
		public static float minimumStateDuration = 0.25f;

		// Token: 0x04000060 RID: 96
		public static string startCloakSoundString;

		// Token: 0x04000061 RID: 97
		public static string stopCloakSoundString;

		// Token: 0x04000062 RID: 98
		public static GameObject smokescreenEffectPrefab;

		// Token: 0x04000063 RID: 99
		public static Material destealthMaterial;

		// Token: 0x04000064 RID: 100
		private float stopwatch;

		// Token: 0x04000065 RID: 101
		private bool hasCastSmoke;

		// Token: 0x04000066 RID: 102
		private Animator animator;

		// Token: 0x04000067 RID: 103
		private Transform modelTransform;

		// Token: 0x04000068 RID: 104
		private TwitchController twitchController;
	}
}
