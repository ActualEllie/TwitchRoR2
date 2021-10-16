using System;
using EntityStates.Commando;
using EntityStates.Commando.CommandoWeapon;
using RoR2;
using Twitch;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.TwitchStates
{
	// Token: 0x0200000D RID: 13
	public class TwitchScurry : BaseState
	{
		// Token: 0x0600004A RID: 74 RVA: 0x00004548 File Offset: 0x00002748
		public override void OnEnter()
		{
			base.OnEnter();
			this.modelTransform = base.GetModelTransform();
			this.twitchController = base.GetComponent<TwitchController>();
			bool flag = this.modelTransform;
			if (flag)
			{
				this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
			}
			this.animator = base.GetModelAnimator();
			ChildLocator component = this.animator.GetComponent<ChildLocator>();
			bool flag2 = base.isAuthority && base.inputBank && base.characterDirection;
			if (flag2)
			{
				this.forwardDirection = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
			}
			Vector3 vector = base.characterDirection ? base.characterDirection.forward : this.forwardDirection;
			Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
			float num = Vector3.Dot(this.forwardDirection, vector);
			float num2 = Vector3.Dot(this.forwardDirection, vector2);
			this.animator.SetFloat("forwardSpeed", num, 0.1f, Time.fixedDeltaTime);
			this.animator.SetFloat("rightSpeed", num2, 0.1f, Time.fixedDeltaTime);
			bool flag3 = base.characterBody && NetworkServer.active;
			if (flag3)
			{
				base.characterBody.AddBuff(7);
				base.characterBody.AddBuff(8);
			}
			bool flag4 = this.twitchController;
			if (flag4)
			{
				this.twitchController.EnterStealth();
			}
			this.CastSmoke();
			this.RecalculateSpeed();
			bool flag5 = base.characterMotor && base.characterDirection;
			if (flag5)
			{
				CharacterMotor characterMotor = base.characterMotor;
				characterMotor.velocity.y = characterMotor.velocity.y * 0.2f;
				base.characterMotor.velocity = this.forwardDirection * this.rollSpeed;
			}
			base.PlayAnimation("FullBody, Override", "Scurry");
			Vector3 vector3 = base.characterMotor ? base.characterMotor.velocity : Vector3.zero;
			this.previousPosition = base.transform.position - vector3;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x0000479F File Offset: 0x0000299F
		private void RecalculateSpeed()
		{
			this.rollSpeed = (2f + 0.5f * this.moveSpeedStat) * Mathf.Lerp(TwitchScurry.initialSpeedCoefficient, TwitchScurry.finalSpeedCoefficient, base.fixedAge / this.duration);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000047D8 File Offset: 0x000029D8
		private void CastSmoke()
		{
			bool flag = !this.hasCastSmoke;
			if (flag)
			{
				Util.PlaySound(Sounds.TwitchEnterStealth, base.gameObject);
				this.hasCastSmoke = true;
			}
			else
			{
				Util.PlaySound(Sounds.TwitchExitStealth, base.gameObject);
			}
			EffectManager.SpawnEffect(CastSmokescreenNoDelay.smokescreenEffectPrefab, new EffectData
			{
				origin = base.transform.position
			}, false);
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00004848 File Offset: 0x00002A48
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			this.RecalculateSpeed();
			bool flag = base.cameraTargetParams;
			if (flag)
			{
				base.cameraTargetParams.fovOverride = Mathf.Lerp(DodgeState.dodgeFOV, 60f, base.fixedAge / this.duration);
			}
			Vector3 normalized = (base.transform.position - this.previousPosition).normalized;
			bool flag2 = base.characterMotor && base.characterDirection && normalized != Vector3.zero;
			if (flag2)
			{
				Vector3 vector = normalized * this.rollSpeed;
				float y = vector.y;
				vector.y = 0f;
				float num = Mathf.Max(Vector3.Dot(vector, this.forwardDirection), 0f);
				vector = this.forwardDirection * num;
				vector.y += Mathf.Max(y, 0f);
				base.characterMotor.velocity = vector;
			}
			this.previousPosition = base.transform.position;
			bool flag3 = base.fixedAge >= this.duration && base.isAuthority;
			if (flag3)
			{
				this.outer.SetNextStateToMain();
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00004998 File Offset: 0x00002B98
		public override void OnExit()
		{
			bool flag = base.cameraTargetParams;
			if (flag)
			{
				base.cameraTargetParams.fovOverride = -1f;
			}
			bool flag2 = base.characterBody && NetworkServer.active;
			if (flag2)
			{
				bool flag3 = base.characterBody.HasBuff(7);
				if (flag3)
				{
					base.characterBody.RemoveBuff(7);
				}
				bool flag4 = base.characterBody.HasBuff(8);
				if (flag4)
				{
					base.characterBody.RemoveBuff(8);
				}
				base.characterBody.AddTimedBuff(Twitch.ambushBuff, 2f);
				base.characterBody.RecalculateStats();
				bool flag5 = this.modelTransform;
				if (flag5)
				{
					TemporaryOverlay temporaryOverlay = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
					temporaryOverlay.duration = 2f;
					temporaryOverlay.animateShaderAlpha = true;
					temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 2.5f, 0f);
					temporaryOverlay.destroyComponentOnEnd = true;
					temporaryOverlay.originalMaterial = Resources.Load<Material>("Materials/matPoisoned");
					temporaryOverlay.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
				}
			}
			bool flag6 = this.twitchController;
			if (flag6)
			{
				this.twitchController.GetAmbushBuff(2f);
			}
			bool flag7 = !this.outer.destroying;
			if (flag7)
			{
				this.CastSmoke();
			}
			bool flag8 = TwitchAmbush.destealthMaterial;
			if (flag8)
			{
				TemporaryOverlay temporaryOverlay2 = this.animator.gameObject.AddComponent<TemporaryOverlay>();
				temporaryOverlay2.duration = 1f;
				temporaryOverlay2.destroyComponentOnEnd = true;
				temporaryOverlay2.originalMaterial = TwitchAmbush.destealthMaterial;
				temporaryOverlay2.inspectorCharacterModel = this.animator.gameObject.GetComponent<CharacterModel>();
				temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlay2.animateShaderAlpha = true;
			}
			bool flag9 = this.twitchController;
			if (flag9)
			{
				this.twitchController.ExitStealth();
			}
			base.PlayAnimation("FullBody, Override", "BufferEmpty");
			base.OnExit();
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00004BC1 File Offset: 0x00002DC1
		public override void OnSerialize(NetworkWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(this.forwardDirection);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00004BD9 File Offset: 0x00002DD9
		public override void OnDeserialize(NetworkReader reader)
		{
			base.OnDeserialize(reader);
			this.forwardDirection = reader.ReadVector3();
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004BF0 File Offset: 0x00002DF0
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return 2;
		}

		// Token: 0x04000069 RID: 105
		public float duration = 0.4f;

		// Token: 0x0400006A RID: 106
		public static GameObject dodgeEffect;

		// Token: 0x0400006B RID: 107
		public static float initialSpeedCoefficient = 8f;

		// Token: 0x0400006C RID: 108
		public static float finalSpeedCoefficient = 0.25f;

		// Token: 0x0400006D RID: 109
		private float rollSpeed;

		// Token: 0x0400006E RID: 110
		private bool hasCastSmoke;

		// Token: 0x0400006F RID: 111
		private Vector3 forwardDirection;

		// Token: 0x04000070 RID: 112
		private Animator animator;

		// Token: 0x04000071 RID: 113
		private Vector3 previousPosition;

		// Token: 0x04000072 RID: 114
		private Transform modelTransform;

		// Token: 0x04000073 RID: 115
		private CharacterModel characterModel;

		// Token: 0x04000074 RID: 116
		private TwitchController twitchController;
	}
}
