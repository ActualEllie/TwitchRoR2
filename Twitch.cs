using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using EntityStates;
using EntityStates.TwitchStates;
using KinematicCharacterController;
using On.RoR2;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using SkillsPlusPlus;
using Twitch.Unlockables;
using UnityEngine;
using UnityEngine.Networking;

namespace Twitch
{
	// Token: 0x0200000E RID: 14
	[BepInDependency("com.bepis.r2api", 1)]
	[BepInPlugin("com.rob.Twitch", "Twitch", "2.1.1")]
	[NetworkCompatibility(1, 1)]
	[R2APISubmoduleDependency(new string[]
	{
		"PrefabAPI",
		"SoundAPI",
		"LanguageAPI",
		"SurvivorAPI",
		"LoadoutAPI",
		"ItemAPI",
		"BuffAPI",
		"EffectAPI",
		"UnlockablesAPI"
	})]
	public class Twitch : BaseUnityPlugin
	{
		// Token: 0x06000054 RID: 84 RVA: 0x00004C30 File Offset: 0x00002E30
		private void Awake()
		{
			this.ReadConfig();
			Assets.PopulateAssets();
			this.RegisterStates();
			Twitch.CreatePrefab();
			this.RegisterBuff();
			this.RegisterUnlockables();
			this.RegisterCharacter();
			bool flag = Chainloader.PluginInfos.ContainsKey("com.cwmlolzlz.skills");
			if (flag)
			{
				Debug.Log("Skills++ is installed. Loading skill modifiers.");
				this.RegisterSkillModifiers();
			}
			this.RegisterHooks();
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00004C9C File Offset: 0x00002E9C
		private void ReadConfig()
		{
			Twitch.how = base.Config.Bind<bool>(new ConfigDefinition("01 - General Settings", "HOW"), false, new ConfigDescription("HOW IS THIS RAT", null, Array.Empty<object>()));
			Twitch.boom = base.Config.Bind<bool>(new ConfigDefinition("01 - General Settings", "Boom"), false, new ConfigDescription("Enables Bazooka", null, Array.Empty<object>()));
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00004D0A File Offset: 0x00002F0A
		private void RegisterSkillModifiers()
		{
			SkillModifierManager.LoadSkillModifiers();
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00004D14 File Offset: 0x00002F14
		private void RegisterUnlockables()
		{
			LanguageAPI.Add("ROB_TWITCH_MASTERYUNLOCKABLE_ACHIEVEMENT_NAME", "Twitch: Mastery");
			LanguageAPI.Add("ROB_TWITCH_MASTERYUNLOCKABLE_ACHIEVEMENT_DESC", "As Twitch, beat the game or obliterate on Monsoon.");
			LanguageAPI.Add("ROB_TWITCH_MASTERYUNLOCKABLE_UNLOCKABLE_NAME", "Twitch: Mastery");
			LanguageAPI.Add("ROB_TWITCH_TARUNLOCKABLE_ACHIEVEMENT_NAME", "Twitch: Pest of Aphelia");
			LanguageAPI.Add("ROB_TWITCH_TARUNLOCKABLE_ACHIEVEMENT_DESC", "As Twitch, get killed by a Clay Dunestrider.");
			LanguageAPI.Add("ROB_TWITCH_TARUNLOCKABLE_UNLOCKABLE_NAME", "Twitch: Pest of Aphelia");
			LanguageAPI.Add("ROB_TWITCH_SIMPLEUNLOCKABLE_ACHIEVEMENT_NAME", "Twitch: Pestilence");
			LanguageAPI.Add("ROB_TWITCH_SIMPLEUNLOCKABLE_ACHIEVEMENT_DESC", "As Twitch, expunge 40 stacks of venom on a single target.");
			LanguageAPI.Add("ROB_TWITCH_SIMPLEUNLOCKABLE_UNLOCKABLE_NAME", "Twitch: Pestilence");
			UnlockablesAPI.AddUnlockable<MasteryUnlockable>(true);
			UnlockablesAPI.AddUnlockable<TarUnlockable>(true);
			UnlockablesAPI.AddUnlockable<SimpleUnlockable>(true);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00004DC7 File Offset: 0x00002FC7
		private void RegisterDot()
		{
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00004DCA File Offset: 0x00002FCA
		private void RegisterHooks()
		{
			CharacterBody.RecalculateStats += new CharacterBody.hook_RecalculateStats(this.CharacterBody_RecalculateStats);
			HealthComponent.TakeDamage += new HealthComponent.hook_TakeDamage(this.HealthComponent_TakeDamage);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00004DF4 File Offset: 0x00002FF4
		private void CharacterBody_RecalculateStats(CharacterBody.orig_RecalculateStats orig, CharacterBody self)
		{
			orig.Invoke(self);
			bool flag = self && self.HasBuff(Twitch.venomDebuff);
			if (flag)
			{
				float num = 1f - 0.035f * (float)self.GetBuffCount(Twitch.venomDebuff);
				bool flag2 = num < 0.1f;
				if (flag2)
				{
					num = 0.1f;
				}
				Reflection.SetPropertyValue<float>(self, "attackSpeed", self.attackSpeed * num);
				Reflection.SetPropertyValue<float>(self, "armor", self.armor - 1.5f * (float)self.GetBuffCount(Twitch.venomDebuff));
			}
			bool flag3 = self && self.HasBuff(Twitch.ambushBuff);
			if (flag3)
			{
				Reflection.SetPropertyValue<float>(self, "attackSpeed", self.attackSpeed + 1f);
			}
			bool flag4 = self && self.HasBuff(Twitch.expungeDebuff);
			if (flag4)
			{
				int buffCount = self.GetBuffCount(Twitch.expungeDebuff);
				float num2 = 1f - 0.045f * (float)buffCount;
				bool flag5 = num2 < 0.1f;
				if (flag5)
				{
					num2 = 0.1f;
				}
				Reflection.SetPropertyValue<float>(self, "attackSpeed", self.attackSpeed * num2);
				Reflection.SetPropertyValue<float>(self, "armor", self.armor - (float)(5 * buffCount));
			}
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00004F40 File Offset: 0x00003140
		private void HealthComponent_TakeDamage(HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo di)
		{
			bool flag = di.attacker != null;
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3 = self != null;
				bool flag4 = flag3;
				if (flag4)
				{
					bool flag5 = self.GetComponent<CharacterBody>() != null;
					bool flag6 = flag5;
					if (flag6)
					{
						bool flag7 = di.damageType.HasFlag(1048576);
						bool flag8 = flag7;
						if (flag8)
						{
							bool flag9 = di.attacker.GetComponent<CharacterBody>();
							bool flag10 = flag9;
							if (flag10)
							{
								bool flag11 = di.attacker.GetComponent<CharacterBody>().baseNameToken == "TWITCH_NAME";
								bool flag12 = flag11;
								if (flag12)
								{
									di.damageType = 0;
									bool flag13 = !self.GetComponent<CharacterBody>().HasBuff(Twitch.expungeDebuff);
									if (flag13)
									{
										self.GetComponent<CharacterBody>().AddTimedBuff(Twitch.venomDebuff, 5f * di.procCoefficient);
									}
									bool flag14 = di.attacker.GetComponent<TwitchController>();
									bool flag15 = flag14;
									if (flag15)
									{
										di.attacker.GetComponent<TwitchController>().RefundCooldown(di.procCoefficient);
									}
								}
							}
						}
						else
						{
							bool flag16 = di.damageType.HasFlag(4096);
							bool flag17 = flag16;
							if (flag17)
							{
								bool flag18 = di.attacker.GetComponent<CharacterBody>();
								bool flag19 = flag18;
								if (flag19)
								{
									bool flag20 = di.attacker.GetComponent<CharacterBody>().baseNameToken == "TWITCH_NAME";
									bool flag21 = flag20;
									if (flag21)
									{
										di.damageType = 0;
										Util.PlaySound(Sounds.TwitchExpungeHit, self.gameObject);
										bool flag22 = !self.GetComponent<CharacterBody>().HasBuff(Twitch.expungeDebuff);
										bool flag23 = flag22;
										if (flag23)
										{
											CharacterBody component = self.GetComponent<CharacterBody>();
											bool flag24 = component.HasBuff(Twitch.venomDebuff);
											if (flag24)
											{
												int buffCount = component.GetBuffCount(Twitch.venomDebuff);
												for (int i = 0; i < buffCount; i++)
												{
													component.AddBuff(Twitch.expungeDebuff);
													component.RemoveBuff(Twitch.venomDebuff);
													di.damage += di.attacker.GetComponent<CharacterBody>().damage * TwitchExpunge.damageBonus;
												}
												EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/BeetleSpitExplosion"), new EffectData
												{
													origin = self.transform.position,
													scale = (float)buffCount
												}, true);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			orig.Invoke(self, di);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x000051E0 File Offset: 0x000033E0
		private void RegisterBuff()
		{
			BuffDef buffDef = new BuffDef
			{
				buffColor = Twitch.poisonColor,
				buffIndex = 63,
				canStack = true,
				eliteIndex = -1,
				iconPath = "Textures/BuffIcons/texBuffBleedingIcon",
				isDebuff = true,
				name = "TwitchVenomDebuff"
			};
			Twitch.venomDebuff = BuffAPI.Add(new CustomBuff(buffDef));
			BuffDef buffDef2 = new BuffDef
			{
				buffColor = Twitch.characterColor,
				buffIndex = 63,
				canStack = false,
				eliteIndex = -1,
				iconPath = "Textures/MiscIcons/texAttackIcon",
				isDebuff = false,
				name = "TwitchAmbushBuff"
			};
			Twitch.ambushBuff = BuffAPI.Add(new CustomBuff(buffDef2));
			BuffDef buffDef3 = new BuffDef
			{
				buffColor = Twitch.characterColor,
				buffIndex = 63,
				canStack = true,
				eliteIndex = -1,
				iconPath = "Textures/BuffIcons/texBuffDeathMarkIcon",
				isDebuff = true,
				name = "TwitchExpungedDebuff"
			};
			Twitch.expungeDebuff = BuffAPI.Add(new CustomBuff(buffDef3));
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000052F0 File Offset: 0x000034F0
		private static GameObject CreateModel(GameObject main)
		{
			Object.Destroy(main.transform.Find("ModelBase").gameObject);
			Object.Destroy(main.transform.Find("CameraPivot").gameObject);
			Object.Destroy(main.transform.Find("AimOrigin").gameObject);
			return Assets.MainAssetBundle.LoadAsset<GameObject>("mdlTwitch");
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00005364 File Offset: 0x00003564
		internal static void CreatePrefab()
		{
			Twitch.characterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody"), "TwitchBody", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "CreatePrefab", 305);
			Twitch.characterPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
			GameObject gameObject = new GameObject("ModelBase");
			gameObject.transform.parent = Twitch.characterPrefab.transform;
			gameObject.transform.localPosition = new Vector3(0f, -0.81f, 0f);
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			GameObject gameObject2 = new GameObject("CameraPivot");
			gameObject2.transform.parent = gameObject.transform;
			gameObject2.transform.localPosition = new Vector3(0f, 1.6f, 0f);
			gameObject2.transform.localRotation = Quaternion.identity;
			gameObject2.transform.localScale = Vector3.one;
			GameObject gameObject3 = new GameObject("AimOrigin");
			gameObject3.transform.parent = gameObject.transform;
			gameObject3.transform.localPosition = new Vector3(0f, 1.4f, 0f);
			gameObject3.transform.localRotation = Quaternion.identity;
			gameObject3.transform.localScale = Vector3.one;
			GameObject gameObject4 = Twitch.CreateModel(Twitch.characterPrefab);
			Transform transform = gameObject4.transform;
			transform.parent = gameObject.transform;
			transform.localPosition = Vector3.zero;
			transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
			transform.localRotation = Quaternion.identity;
			CharacterDirection component = Twitch.characterPrefab.GetComponent<CharacterDirection>();
			component.moveVector = Vector3.zero;
			component.targetTransform = gameObject.transform;
			component.overrideAnimatorForwardTransform = null;
			component.rootMotionAccumulator = null;
			component.modelAnimator = gameObject4.GetComponentInChildren<Animator>();
			component.driveFromRootRotation = false;
			component.turnSpeed = 720f;
			CharacterBody component2 = Twitch.characterPrefab.GetComponent<CharacterBody>();
			component2.bodyIndex = -1;
			component2.name = "TwitchBody";
			component2.baseNameToken = "TWITCH_NAME";
			component2.subtitleNameToken = "TWITCH_SUBTITLE";
			component2.bodyFlags = 16;
			component2.rootMotionInMainState = false;
			component2.mainRootSpeed = 0f;
			component2.baseMaxHealth = 90f;
			component2.levelMaxHealth = 24f;
			component2.baseRegen = 0.5f;
			component2.levelRegen = 0.25f;
			component2.baseMaxShield = 0f;
			component2.levelMaxShield = 0f;
			component2.baseMoveSpeed = 7f;
			component2.levelMoveSpeed = 0f;
			component2.baseAcceleration = 80f;
			component2.baseJumpPower = 15f;
			component2.levelJumpPower = 0f;
			component2.baseDamage = 15f;
			component2.levelDamage = 3f;
			component2.baseAttackSpeed = 1f;
			component2.levelAttackSpeed = 0.02f;
			component2.baseCrit = 1f;
			component2.levelCrit = 0f;
			component2.baseArmor = 0f;
			component2.levelArmor = 0f;
			component2.baseJumpCount = 1;
			component2.sprintingSpeedMultiplier = 1.5f;
			component2.wasLucky = false;
			component2.hideCrosshair = false;
			component2.aimOriginTransform = gameObject3.transform;
			component2.hullClassification = 0;
			component2.portraitIcon = Assets.charPortrait;
			component2.isChampion = false;
			component2.currentVehicle = null;
			component2.preferredPodPrefab = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponent<CharacterBody>().preferredPodPrefab;
			component2.preferredInitialStateType = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponent<CharacterBody>().preferredInitialStateType;
			component2.skinIndex = 0U;
			CharacterMotor component3 = Twitch.characterPrefab.GetComponent<CharacterMotor>();
			component3.walkSpeedPenaltyCoefficient = 1f;
			component3.characterDirection = component;
			component3.muteWalkMotion = false;
			component3.mass = 100f;
			component3.airControl = 0.25f;
			component3.disableAirControlUntilCollision = false;
			component3.generateParametersOnAwake = true;
			InputBankTest component4 = Twitch.characterPrefab.GetComponent<InputBankTest>();
			component4.moveVector = Vector3.zero;
			CameraTargetParams component5 = Twitch.characterPrefab.GetComponent<CameraTargetParams>();
			component5.cameraParams = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponent<CameraTargetParams>().cameraParams;
			component5.cameraPivotTransform = null;
			component5.aimMode = 0;
			component5.recoil = Vector2.zero;
			component5.idealLocalCameraPos = Vector3.zero;
			component5.dontRaycastToPivot = false;
			ModelLocator component6 = Twitch.characterPrefab.GetComponent<ModelLocator>();
			component6.modelTransform = transform;
			component6.modelBaseTransform = gameObject.transform;
			component6.dontReleaseModelOnDeath = false;
			component6.autoUpdateModelTransform = true;
			component6.dontDetatchFromParent = false;
			component6.noCorpse = false;
			component6.normalizeToFloor = true;
			component6.preserveModel = false;
			ChildLocator component7 = gameObject4.GetComponent<ChildLocator>();
			CharacterModel characterModel = gameObject4.AddComponent<CharacterModel>();
			characterModel.body = component2;
			CharacterModel characterModel2 = characterModel;
			CharacterModel.RendererInfo[] array = new CharacterModel.RendererInfo[5];
			int num = 0;
			CharacterModel.RendererInfo rendererInfo = default(CharacterModel.RendererInfo);
			rendererInfo.defaultMaterial = gameObject4.GetComponentInChildren<SkinnedMeshRenderer>().material;
			rendererInfo.renderer = gameObject4.GetComponentInChildren<SkinnedMeshRenderer>();
			rendererInfo.defaultShadowCastingMode = 1;
			rendererInfo.ignoreOverlays = false;
			array[num] = rendererInfo;
			int num2 = 1;
			rendererInfo = default(CharacterModel.RendererInfo);
			rendererInfo.defaultMaterial = component7.FindChild("CheeseModel").GetComponentInChildren<MeshRenderer>().material;
			rendererInfo.renderer = component7.FindChild("CheeseModel").GetComponentInChildren<MeshRenderer>();
			rendererInfo.defaultShadowCastingMode = 1;
			rendererInfo.ignoreOverlays = false;
			array[num2] = rendererInfo;
			int num3 = 2;
			rendererInfo = default(CharacterModel.RendererInfo);
			rendererInfo.defaultMaterial = component7.FindChild("Gun").GetComponentInChildren<MeshRenderer>().material;
			rendererInfo.renderer = component7.FindChild("Gun").GetComponentInChildren<MeshRenderer>();
			rendererInfo.defaultShadowCastingMode = 1;
			rendererInfo.ignoreOverlays = false;
			array[num3] = rendererInfo;
			int num4 = 3;
			rendererInfo = default(CharacterModel.RendererInfo);
			rendererInfo.defaultMaterial = component7.FindChild("Bazooka").GetComponentInChildren<MeshRenderer>().material;
			rendererInfo.renderer = component7.FindChild("Bazooka").GetComponentInChildren<MeshRenderer>();
			rendererInfo.defaultShadowCastingMode = 1;
			rendererInfo.ignoreOverlays = false;
			array[num4] = rendererInfo;
			int num5 = 4;
			rendererInfo = default(CharacterModel.RendererInfo);
			rendererInfo.defaultMaterial = component7.FindChild("Shotgun").GetComponentInChildren<MeshRenderer>().material;
			rendererInfo.renderer = component7.FindChild("Shotgun").GetComponentInChildren<MeshRenderer>();
			rendererInfo.defaultShadowCastingMode = 1;
			rendererInfo.ignoreOverlays = false;
			array[num5] = rendererInfo;
			characterModel2.baseRendererInfos = array;
			characterModel.autoPopulateLightInfos = true;
			characterModel.invisibilityCount = 0;
			characterModel.temporaryOverlays = new List<TemporaryOverlay>();
			Reflection.SetFieldValue<SkinnedMeshRenderer>(characterModel, "mainSkinnedMeshRenderer", characterModel.baseRendererInfos[0].renderer.gameObject.GetComponent<SkinnedMeshRenderer>());
			bool flag = Twitch.characterPrefab.GetComponent<TeamComponent>() != null;
			TeamComponent component8;
			if (flag)
			{
				component8 = Twitch.characterPrefab.GetComponent<TeamComponent>();
			}
			else
			{
				component8 = Twitch.characterPrefab.GetComponent<TeamComponent>();
			}
			component8.hideAllyCardDisplay = false;
			component8.teamIndex = -1;
			HealthComponent component9 = Twitch.characterPrefab.GetComponent<HealthComponent>();
			component9.health = 120f;
			component9.shield = 0f;
			component9.barrier = 0f;
			component9.magnetiCharge = 0f;
			component9.body = null;
			component9.dontShowHealthbar = false;
			component9.globalDeathEventChanceCoefficient = 1f;
			Twitch.characterPrefab.GetComponent<Interactor>().maxInteractionDistance = 3f;
			Twitch.characterPrefab.GetComponent<InteractionDriver>().highlightInteractor = true;
			CharacterDeathBehavior component10 = Twitch.characterPrefab.GetComponent<CharacterDeathBehavior>();
			component10.deathStateMachine = Twitch.characterPrefab.GetComponent<EntityStateMachine>();
			component10.deathState = new SerializableEntityStateType(typeof(GenericCharacterDeath));
			SfxLocator component11 = Twitch.characterPrefab.GetComponent<SfxLocator>();
			component11.deathSound = "Play_ui_player_death";
			component11.barkSound = "";
			component11.openSound = "";
			component11.landingSound = "Play_char_land";
			component11.fallDamageSound = "Play_char_land_fall_damage";
			component11.aliveLoopStart = "";
			component11.aliveLoopStop = "";
			Rigidbody component12 = Twitch.characterPrefab.GetComponent<Rigidbody>();
			component12.mass = 100f;
			component12.drag = 0f;
			component12.angularDrag = 0f;
			component12.useGravity = false;
			component12.isKinematic = true;
			component12.interpolation = 0;
			component12.collisionDetectionMode = 0;
			component12.constraints = 0;
			CapsuleCollider component13 = Twitch.characterPrefab.GetComponent<CapsuleCollider>();
			component13.isTrigger = false;
			component13.material = null;
			component13.center = new Vector3(0f, 0f, 0f);
			component13.radius = 0.5f;
			component13.height = 1.82f;
			component13.direction = 1;
			KinematicCharacterMotor component14 = Twitch.characterPrefab.GetComponent<KinematicCharacterMotor>();
			component14.CharacterController = component3;
			component14.Capsule = component13;
			component14.Rigidbody = component12;
			component13.radius = 0.5f;
			component13.height = 1.82f;
			component13.center = new Vector3(0f, 0f, 0f);
			component13.material = null;
			component14.DetectDiscreteCollisions = false;
			component14.GroundDetectionExtraDistance = 0f;
			component14.MaxStepHeight = 0.2f;
			component14.MinRequiredStepDepth = 0.1f;
			component14.MaxStableSlopeAngle = 55f;
			component14.MaxStableDistanceFromLedge = 0.5f;
			component14.PreventSnappingOnLedges = false;
			component14.MaxStableDenivelationAngle = 55f;
			component14.RigidbodyInteractionType = 0;
			component14.PreserveAttachedRigidbodyMomentum = true;
			component14.HasPlanarConstraint = false;
			component14.PlanarConstraintAxis = Vector3.up;
			component14.StepHandling = 0;
			component14.LedgeHandling = true;
			component14.InteractiveRigidbodyHandling = true;
			component14.SafeMovement = false;
			HurtBoxGroup hurtBoxGroup = gameObject4.AddComponent<HurtBoxGroup>();
			HurtBox hurtBox = gameObject4.GetComponentInChildren<CapsuleCollider>().gameObject.AddComponent<HurtBox>();
			hurtBox.gameObject.layer = LayerIndex.entityPrecise.intVal;
			hurtBox.healthComponent = component9;
			hurtBox.isBullseye = true;
			hurtBox.damageModifier = 0;
			hurtBox.hurtBoxGroup = hurtBoxGroup;
			hurtBox.indexInGroup = 0;
			hurtBoxGroup.hurtBoxes = new HurtBox[]
			{
				hurtBox
			};
			hurtBoxGroup.mainHurtBox = hurtBox;
			hurtBoxGroup.bullseyeCount = 1;
			FootstepHandler footstepHandler = gameObject4.AddComponent<FootstepHandler>();
			footstepHandler.baseFootstepString = "Play_player_footstep";
			footstepHandler.sprintFootstepOverrideString = "";
			footstepHandler.enableFootstepDust = true;
			footstepHandler.footstepDustPrefab = Resources.Load<GameObject>("Prefabs/GenericFootstepDust");
			RagdollController ragdollController = gameObject4.AddComponent<RagdollController>();
			ragdollController.bones = null;
			ragdollController.componentsToDisableOnRagdoll = null;
			AimAnimator aimAnimator = gameObject4.AddComponent<AimAnimator>();
			aimAnimator.inputBank = component4;
			aimAnimator.directionComponent = component;
			aimAnimator.pitchRangeMax = 55f;
			aimAnimator.pitchRangeMin = -50f;
			aimAnimator.yawRangeMin = -44f;
			aimAnimator.yawRangeMax = 44f;
			aimAnimator.pitchGiveupRange = 30f;
			aimAnimator.yawGiveupRange = 10f;
			aimAnimator.giveupDuration = 8f;
			Twitch.characterPrefab.AddComponent<TwitchController>();
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00005EC8 File Offset: 0x000040C8
		private void FindComponents(GameObject obj)
		{
			bool flag = obj;
			if (flag)
			{
				Debug.Log("Listing components on " + obj.name);
				foreach (Component component in obj.GetComponentsInChildren<Component>())
				{
					bool flag2 = component;
					if (flag2)
					{
						Debug.Log(component.gameObject.name + " has component " + component.GetType().Name);
					}
				}
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00005F48 File Offset: 0x00004148
		private void RegisterCharacter()
		{
			this.characterDisplay = PrefabAPI.InstantiateClone(Twitch.characterPrefab.GetComponent<ModelLocator>().modelBaseTransform.gameObject, "TwitchDisplay", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 673);
			this.characterDisplay.AddComponent<NetworkIdentity>();
			this.characterDisplay.AddComponent<MenuAnim>();
			foreach (ParticleSystem particleSystem in Twitch.characterPrefab.GetComponentsInChildren<ParticleSystem>())
			{
				bool flag = particleSystem.transform.parent.name == "GrenadeFlash";
				if (flag)
				{
					particleSystem.gameObject.AddComponent<TwitchGrenadeTicker>();
				}
			}
			Twitch.boltProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/SyringeProjectile"), "Prefabs/Projectiles/CrossbowBoltProjectile", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 688);
			GameObject gameObject = PrefabAPI.InstantiateClone(Assets.arrowModel, "TwitchArrowModel", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 690);
			gameObject.AddComponent<NetworkIdentity>();
			gameObject.AddComponent<ProjectileGhostController>();
			Twitch.boltProjectile.GetComponent<ProjectileController>().ghostPrefab = gameObject;
			Twitch.boltProjectile.GetComponent<ProjectileSimple>().velocity *= 1.5f;
			Twitch.boltProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
			Twitch.boltProjectile.GetComponent<ProjectileDamage>().damage = 1f;
			Twitch.boltProjectile.GetComponent<ProjectileDamage>().damageType = 1048576;
			Twitch.expungeProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/SyringeProjectile"), "Prefabs/Projectiles/ExpungeProjectile", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 703);
			Twitch.expungeProjectile.transform.localScale *= 1.5f;
			Twitch.expungeProjectile.GetComponent<ProjectileSimple>().velocity *= 0.75f;
			Twitch.expungeProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
			Twitch.expungeProjectile.GetComponent<ProjectileDamage>().damage = 1f;
			Twitch.expungeProjectile.GetComponent<ProjectileDamage>().damageType = 4096;
			GameObject gameObject2 = PrefabAPI.InstantiateClone(Assets.knifeModel, "TwitchArrowModel", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 712);
			gameObject2.AddComponent<NetworkIdentity>();
			gameObject2.AddComponent<ProjectileGhostController>();
			gameObject2.transform.GetChild(0).localScale *= 2f;
			Twitch.expungeProjectile.GetComponent<ProjectileController>().ghostPrefab = gameObject2;
			Twitch.venomPool = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/CrocoLeapAcid"), "Prefabs/Projectiles/VenomPool", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 720);
			Twitch.venomPool.GetComponent<ProjectileDamage>().damageType = 1048576;
			Twitch.venomPool.GetComponent<ProjectileController>().procCoefficient = 0.6f;
			Twitch.caskProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/CommandoGrenadeProjectile"), "Prefabs/Projectiles/VenomCaskProjectile", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 725);
			Twitch.caskProjectile.transform.localScale *= 0.5f;
			Twitch.caskProjectile.GetComponent<ProjectileDamage>().damage = 1f;
			Twitch.caskProjectile.GetComponent<ProjectileDamage>().damageType = 1048576;
			Twitch.caskProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
			Twitch.caskProjectile.GetComponent<ProjectileSimple>().enableVelocityOverLifetime = Resources.Load<GameObject>("Prefabs/Projectiles/BeetleQueenSpit").GetComponent<ProjectileSimple>().enableVelocityOverLifetime;
			Twitch.caskProjectile.GetComponent<ProjectileSimple>().velocity = Resources.Load<GameObject>("Prefabs/Projectiles/BeetleQueenSpit").GetComponent<ProjectileSimple>().velocity;
			Twitch.caskProjectile.GetComponent<ProjectileSimple>().velocityOverLifetime = Resources.Load<GameObject>("Prefabs/Projectiles/BeetleQueenSpit").GetComponent<ProjectileSimple>().velocityOverLifetime;
			ProjectileImpactExplosion component = Twitch.caskProjectile.GetComponent<ProjectileImpactExplosion>();
			ProjectileImpactExplosion component2 = Resources.Load<GameObject>("Prefabs/Projectiles/BeetleQueenSpit").GetComponent<ProjectileImpactExplosion>();
			component.lifetimeExpiredSoundString = "";
			component.blastDamageCoefficient = 1f;
			component.blastProcCoefficient = 1f;
			component.blastRadius = 8f;
			component.bonusBlastForce = Vector3.zero;
			component.falloffModel = 0;
			component.childrenProjectilePrefab = Twitch.venomPool;
			component.fireChildren = component2.fireChildren;
			component.childrenDamageCoefficient = component2.childrenDamageCoefficient;
			component.childrenCount = component2.childrenCount;
			component.impactEffect = component2.impactEffect;
			component.maxAngleOffset = component2.maxAngleOffset;
			component.minAngleOffset = component2.minAngleOffset;
			component.destroyOnEnemy = true;
			component.destroyOnWorld = true;
			component.explosionSoundString = Sounds.TwitchCaskHit;
			GameObject gameObject3 = PrefabAPI.InstantiateClone(Assets.caskModel, "VenomCaskModel", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 754);
			gameObject3.AddComponent<NetworkIdentity>();
			gameObject3.AddComponent<ProjectileGhostController>();
			Twitch.caskProjectile.GetComponent<ProjectileController>().ghostPrefab = gameObject3;
			Twitch.grenadeProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/CommandoGrenadeProjectile"), "Prefabs/Projectiles/TwitchGrenadeProjectile", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 760);
			Twitch.grenadeProjectile.AddComponent<TwitchGrenadeMain>();
			Twitch.grenadeProjectile.GetComponent<ProjectileDamage>().damage = 1f;
			Twitch.grenadeProjectile.GetComponent<ProjectileDamage>().damageType = 0;
			Twitch.grenadeProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
			ProjectileImpactExplosion component3 = Twitch.grenadeProjectile.GetComponent<ProjectileImpactExplosion>();
			component3.blastDamageCoefficient = 1f;
			component3.blastProcCoefficient = 1f;
			component3.blastRadius = 12f;
			component3.bonusBlastForce = Vector3.zero;
			component3.falloffModel = 0;
			component3.lifetime = 4f;
			component3.timerAfterImpact = false;
			component3.lifetimeExpiredSoundString = "";
			GameObject gameObject4 = PrefabAPI.InstantiateClone(Assets.grenadeModel, "TwitchFragGrenadeModel", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 777);
			gameObject4.AddComponent<NetworkIdentity>();
			gameObject4.AddComponent<ProjectileGhostController>();
			gameObject4.AddComponent<TwitchGrenadeController>();
			Twitch.grenadeProjectile.GetComponent<ProjectileController>().ghostPrefab = gameObject4;
			Twitch.bazookaProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/CommandoGrenadeProjectile"), "Prefabs/Projectiles/TwitchBazookaProjectile", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 784);
			Twitch.bazookaProjectile.transform.localScale *= 3f;
			Twitch.bazookaProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
			Twitch.bazookaProjectile.GetComponent<ProjectileImpactExplosion>().blastDamageCoefficient = 1f;
			Twitch.bazookaProjectile.GetComponent<ProjectileImpactExplosion>().blastProcCoefficient = 1f;
			Twitch.bazookaProjectile.GetComponent<ProjectileImpactExplosion>().blastRadius = 8f;
			Twitch.bazookaProjectile.GetComponent<ProjectileImpactExplosion>().lifetimeAfterImpact = 0f;
			Twitch.bazookaProjectile.GetComponent<ProjectileImpactExplosion>().impactEffect = Resources.Load<GameObject>("Prefabs/Projectiles/LemurianBigFireball").GetComponent<ProjectileImpactExplosion>().impactEffect;
			GameObject gameObject5 = PrefabAPI.InstantiateClone(Assets.bazookaRocketModel, "BazookaRocketModel", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 797);
			gameObject5.AddComponent<NetworkIdentity>();
			gameObject5.AddComponent<ProjectileGhostController>();
			gameObject5.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 90f, 0f);
			gameObject5.transform.GetChild(0).localScale *= 0.35f;
			gameObject5.transform.GetChild(0).GetChild(0).gameObject.AddComponent<SeparateFromParent>();
			Twitch.bazookaProjectile.GetComponent<ProjectileController>().ghostPrefab = gameObject5;
			Twitch.laserTracer = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar"), "TwitchLaserTracer", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 808);
			bool flag2 = !Twitch.laserTracer.GetComponent<EffectComponent>();
			if (flag2)
			{
				Twitch.laserTracer.AddComponent<EffectComponent>();
			}
			bool flag3 = !Twitch.laserTracer.GetComponent<VFXAttributes>();
			if (flag3)
			{
				Twitch.laserTracer.AddComponent<VFXAttributes>();
			}
			bool flag4 = !Twitch.laserTracer.GetComponent<NetworkIdentity>();
			if (flag4)
			{
				Twitch.laserTracer.AddComponent<NetworkIdentity>();
			}
			foreach (LineRenderer lineRenderer in Twitch.laserTracer.GetComponentsInChildren<LineRenderer>())
			{
				bool flag5 = lineRenderer;
				if (flag5)
				{
					Material material = Object.Instantiate<Material>(lineRenderer.material);
					material.SetColor("_TintColor", Twitch.characterColor);
					lineRenderer.material = material;
					lineRenderer.startColor = Twitch.characterColor;
					lineRenderer.endColor = Twitch.characterColor;
				}
			}
			foreach (Light light in Twitch.laserTracer.GetComponentsInChildren<Light>())
			{
				bool flag6 = light;
				if (flag6)
				{
					light.color = Twitch.characterColor;
				}
			}
			foreach (MeshRenderer meshRenderer in Twitch.laserTracer.GetComponentsInChildren<MeshRenderer>())
			{
				bool flag7 = meshRenderer;
				if (flag7)
				{
					meshRenderer.enabled = false;
				}
			}
			bool flag8 = Twitch.characterPrefab;
			if (flag8)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.characterPrefab, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 837);
			}
			bool flag9 = this.characterDisplay;
			if (flag9)
			{
				PrefabAPI.RegisterNetworkPrefab(this.characterDisplay, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 838);
			}
			bool flag10 = Twitch.boltProjectile;
			if (flag10)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.boltProjectile, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 839);
			}
			bool flag11 = Twitch.expungeProjectile;
			if (flag11)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.expungeProjectile, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 840);
			}
			bool flag12 = Twitch.caskProjectile;
			if (flag12)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.caskProjectile, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 841);
			}
			bool flag13 = Twitch.venomPool;
			if (flag13)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.venomPool, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 842);
			}
			bool flag14 = Twitch.bazookaProjectile;
			if (flag14)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.bazookaProjectile, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 843);
			}
			bool flag15 = Twitch.grenadeProjectile;
			if (flag15)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.grenadeProjectile, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 844);
			}
			bool flag16 = Twitch.laserTracer;
			if (flag16)
			{
				PrefabAPI.RegisterNetworkPrefab(Twitch.laserTracer, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "RegisterCharacter", 845);
			}
			ProjectileCatalog.getAdditionalEntries += delegate(List<GameObject> list)
			{
				list.Add(Twitch.boltProjectile);
				list.Add(Twitch.bazookaProjectile);
				list.Add(Twitch.caskProjectile);
				list.Add(Twitch.venomPool);
				list.Add(Twitch.expungeProjectile);
				list.Add(Twitch.grenadeProjectile);
			};
			EffectAPI.AddEffect(Twitch.laserTracer);
			string text = "Twitch is a fragile rat who relies on good positioning and using his powerful debuff to shred and debilitate priority targets.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine;
			text = text + "< ! > Venom stacks are only soft capped by your attack speed, making it an invaluable stat." + Environment.NewLine + Environment.NewLine;
			text = text + "< ! > Crossbow hits reduce the cooldown of Ambush, rewarding aggressive play and well positioned piercing shots. " + Environment.NewLine + Environment.NewLine;
			text = text + "< ! > Proper usage of Ambush is key to succees, as it's both your only defensive and strongest offensive tool" + Environment.NewLine + Environment.NewLine;
			text = text + "< ! > Try and save Expunge for when venom is stacked high, as you can only use it once per enemy.</color>" + Environment.NewLine + Environment.NewLine;
			LanguageAPI.Add("TWITCH_NAME", "Twitch");
			LanguageAPI.Add("TWITCH_DESCRIPTION", text);
			LanguageAPI.Add("TWITCH_SUBTITLE", "The Plague Rat");
			LanguageAPI.Add("TWITCH_LORE", "\n''They threw this away? But it’s so shiny!''\n\nA plague rat by birth, a connoisseur of filth by passion, Twitch is a paranoid and mutated rat that walks upright and roots through the dregs of the planet for treasures only he truly values. Armed with a chem-powered crossbow, Twitch is not afraid to get his paws dirty as he builds a throne of refuse in his kingdom of filth, endlessly plotting the downfall of humanity.");
			LanguageAPI.Add("TWITCH_OUTRO_FLAVOR", "..and so he left, adorned with a great deal of plundered treasures.");
			SurvivorDef survivorDef = new SurvivorDef
			{
				name = "TWITCH_NAME",
				unlockableName = "",
				descriptionToken = "TWITCH_DESCRIPTION",
				primaryColor = Twitch.characterColor,
				bodyPrefab = Twitch.characterPrefab,
				displayPrefab = this.characterDisplay
			};
			SurvivorAPI.AddSurvivor(survivorDef);
			this.ItemDisplaySetup();
			this.SkillSetup();
			BodyCatalog.getAdditionalEntries += delegate(List<GameObject> list)
			{
				list.Add(Twitch.characterPrefab);
			};
			this.CreateMaster();
			this.SkinSetup();
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00006B24 File Offset: 0x00004D24
		private void SkinSetup()
		{
			GameObject gameObject = Twitch.characterPrefab.GetComponentInChildren<ModelLocator>().modelTransform.gameObject;
			CharacterModel component = gameObject.GetComponent<CharacterModel>();
			ModelSkinController modelSkinController = gameObject.AddComponent<ModelSkinController>();
			SkinnedMeshRenderer fieldValue = Reflection.GetFieldValue<SkinnedMeshRenderer>(component, "mainSkinnedMeshRenderer");
			LanguageAPI.Add("TWITCHBODY_DEFAULT_SKIN_NAME", "Default");
			LanguageAPI.Add("TWITCHBODY_SIMPLE_SKIN_NAME", "Simple");
			LanguageAPI.Add("TWITCHBODY_TAR_SKIN_NAME", "Tarrat");
			LanguageAPI.Add("TWITCHBODY_TUNDRA_SKIN_NAME", "Tundra");
			LoadoutAPI.SkinDefInfo skinDefInfo = default(LoadoutAPI.SkinDefInfo);
			skinDefInfo.BaseSkins = Array.Empty<SkinDef>();
			skinDefInfo.MinionSkinReplacements = new SkinDef.MinionSkinReplacement[0];
			skinDefInfo.ProjectileGhostReplacements = new SkinDef.ProjectileGhostReplacement[0];
			skinDefInfo.GameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
			skinDefInfo.Icon = LoadoutAPI.CreateSkinIcon(new Color(0.22f, 0.27f, 0.2f), new Color(0.74f, 0.65f, 0.52f), new Color(0.2f, 0.16f, 0.16f), new Color(0.1f, 0.14f, 0.13f));
			SkinDef.MeshReplacement[] array = new SkinDef.MeshReplacement[1];
			int num = 0;
			SkinDef.MeshReplacement meshReplacement = default(SkinDef.MeshReplacement);
			meshReplacement.renderer = fieldValue;
			meshReplacement.mesh = fieldValue.sharedMesh;
			array[num] = meshReplacement;
			skinDefInfo.MeshReplacements = array;
			skinDefInfo.Name = "TWITCHBODY_DEFAULT_SKIN_NAME";
			skinDefInfo.NameToken = "TWITCHBODY_DEFAULT_SKIN_NAME";
			skinDefInfo.RendererInfos = component.baseRendererInfos;
			skinDefInfo.RootObject = gameObject;
			skinDefInfo.UnlockableName = "";
			CharacterModel.RendererInfo[] rendererInfos = skinDefInfo.RendererInfos;
			CharacterModel.RendererInfo[] array2 = new CharacterModel.RendererInfo[rendererInfos.Length];
			rendererInfos.CopyTo(array2, 0);
			Material material = array2[0].defaultMaterial;
			bool flag = material;
			if (flag)
			{
				material = Object.Instantiate<Material>(Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponentInChildren<CharacterModel>().baseRendererInfos[0].defaultMaterial);
				material.SetColor("_Color", Assets.MainAssetBundle.LoadAsset<Material>("matTwitch").GetColor("_Color"));
				material.SetTexture("_MainTex", Assets.MainAssetBundle.LoadAsset<Material>("matTwitch").GetTexture("_MainTex"));
				material.SetColor("_EmColor", Color.black);
				material.SetFloat("_EmPower", 0f);
				material.SetTexture("_EmTex", Assets.MainAssetBundle.LoadAsset<Material>("matTwitch").GetTexture("_EmissionMap"));
				material.SetFloat("_NormalStrength", 0f);
				array2[0].defaultMaterial = material;
			}
			skinDefInfo.RendererInfos = array2;
			SkinDef skinDef = LoadoutAPI.CreateNewSkinDef(skinDefInfo);
			LoadoutAPI.SkinDefInfo skinDefInfo2 = default(LoadoutAPI.SkinDefInfo);
			skinDefInfo2.BaseSkins = Array.Empty<SkinDef>();
			skinDefInfo2.MinionSkinReplacements = new SkinDef.MinionSkinReplacement[0];
			skinDefInfo2.ProjectileGhostReplacements = new SkinDef.ProjectileGhostReplacement[0];
			skinDefInfo2.GameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
			skinDefInfo2.Icon = LoadoutAPI.CreateSkinIcon(new Color(0.23f, 0.32f, 0.21f), new Color(1f, 1f, 1f), new Color(0.17f, 0.14f, 0.12f), new Color(0.13f, 0.18f, 0.13f));
			SkinDef.MeshReplacement[] array3 = new SkinDef.MeshReplacement[1];
			int num2 = 0;
			meshReplacement = default(SkinDef.MeshReplacement);
			meshReplacement.renderer = fieldValue;
			meshReplacement.mesh = fieldValue.sharedMesh;
			array3[num2] = meshReplacement;
			skinDefInfo2.MeshReplacements = array3;
			skinDefInfo2.Name = "TWITCHBODY_SIMPLE_SKIN_NAME";
			skinDefInfo2.NameToken = "TWITCHBODY_SIMPLE_SKIN_NAME";
			skinDefInfo2.RendererInfos = component.baseRendererInfos;
			skinDefInfo2.RootObject = gameObject;
			skinDefInfo2.UnlockableName = "ROB_TWITCH_SIMPLEUNLOCKABLE_REWARD_ID";
			rendererInfos = skinDefInfo.RendererInfos;
			array2 = new CharacterModel.RendererInfo[rendererInfos.Length];
			rendererInfos.CopyTo(array2, 0);
			material = array2[0].defaultMaterial;
			bool flag2 = material;
			if (flag2)
			{
				material = Object.Instantiate<Material>(material);
				material.SetTexture("_MainTex", Assets.simpleSkinMat.GetTexture("_MainTex"));
				array2[0].defaultMaterial = material;
			}
			skinDefInfo2.RendererInfos = array2;
			SkinDef skinDef2 = LoadoutAPI.CreateNewSkinDef(skinDefInfo2);
			LoadoutAPI.SkinDefInfo skinDefInfo3 = default(LoadoutAPI.SkinDefInfo);
			skinDefInfo3.BaseSkins = Array.Empty<SkinDef>();
			skinDefInfo3.MinionSkinReplacements = new SkinDef.MinionSkinReplacement[0];
			skinDefInfo3.ProjectileGhostReplacements = new SkinDef.ProjectileGhostReplacement[0];
			skinDefInfo3.GameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
			skinDefInfo3.Icon = LoadoutAPI.CreateSkinIcon(new Color(0.28f, 0.29f, 0.27f), new Color(0.76f, 0.76f, 0.16f), new Color(0.03f, 0.03f, 0.07f), new Color(0.09f, 0.09f, 0.09f));
			SkinDef.MeshReplacement[] array4 = new SkinDef.MeshReplacement[1];
			int num3 = 0;
			meshReplacement = default(SkinDef.MeshReplacement);
			meshReplacement.renderer = fieldValue;
			meshReplacement.mesh = fieldValue.sharedMesh;
			array4[num3] = meshReplacement;
			skinDefInfo3.MeshReplacements = array4;
			skinDefInfo3.Name = "TWITCHBODY_TAR_SKIN_NAME";
			skinDefInfo3.NameToken = "TWITCHBODY_TAR_SKIN_NAME";
			skinDefInfo3.RendererInfos = component.baseRendererInfos;
			skinDefInfo3.RootObject = gameObject;
			skinDefInfo3.UnlockableName = "ROB_TWITCH_TARUNLOCKABLE_REWARD_ID";
			rendererInfos = skinDefInfo.RendererInfos;
			array2 = new CharacterModel.RendererInfo[rendererInfos.Length];
			rendererInfos.CopyTo(array2, 0);
			material = array2[0].defaultMaterial;
			bool flag3 = material;
			if (flag3)
			{
				material = Object.Instantiate<Material>(material);
				material.SetTexture("_MainTex", Assets.tarSkinMat.GetTexture("_MainTex"));
				material.SetTexture("_EmTex", Assets.tarSkinMat.GetTexture("_EmissionMap"));
				material.SetColor("_EmColor", Color.white);
				material.SetFloat("_EmPower", 5f);
				array2[0].defaultMaterial = material;
			}
			skinDefInfo3.RendererInfos = array2;
			SkinDef skinDef3 = LoadoutAPI.CreateNewSkinDef(skinDefInfo3);
			LoadoutAPI.SkinDefInfo skinDefInfo4 = default(LoadoutAPI.SkinDefInfo);
			skinDefInfo4.BaseSkins = Array.Empty<SkinDef>();
			skinDefInfo4.MinionSkinReplacements = new SkinDef.MinionSkinReplacement[0];
			skinDefInfo4.ProjectileGhostReplacements = new SkinDef.ProjectileGhostReplacement[0];
			skinDefInfo4.GameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
			skinDefInfo4.Icon = LoadoutAPI.CreateSkinIcon(new Color(0.88f, 0.88f, 0.88f), new Color(0.53f, 0.5f, 0.64f), new Color(0.22f, 0.18f, 0.28f), new Color(0.22f, 0.2f, 0.19f));
			SkinDef.MeshReplacement[] array5 = new SkinDef.MeshReplacement[1];
			int num4 = 0;
			meshReplacement = default(SkinDef.MeshReplacement);
			meshReplacement.renderer = fieldValue;
			meshReplacement.mesh = fieldValue.sharedMesh;
			array5[num4] = meshReplacement;
			skinDefInfo4.MeshReplacements = array5;
			skinDefInfo4.Name = "TWITCHBODY_TUNDRA_SKIN_NAME";
			skinDefInfo4.NameToken = "TWITCHBODY_TUNDRA_SKIN_NAME";
			skinDefInfo4.RendererInfos = component.baseRendererInfos;
			skinDefInfo4.RootObject = gameObject;
			skinDefInfo4.UnlockableName = "ROB_TWITCH_MASTERYUNLOCKABLE_REWARD_ID";
			rendererInfos = skinDefInfo.RendererInfos;
			array2 = new CharacterModel.RendererInfo[rendererInfos.Length];
			rendererInfos.CopyTo(array2, 0);
			material = array2[0].defaultMaterial;
			bool flag4 = material;
			if (flag4)
			{
				material = Object.Instantiate<Material>(material);
				material.SetTexture("_MainTex", Assets.tundraSkinMat.GetTexture("_MainTex"));
				array2[0].defaultMaterial = material;
			}
			skinDefInfo4.RendererInfos = array2;
			SkinDef skinDef4 = LoadoutAPI.CreateNewSkinDef(skinDefInfo4);
			modelSkinController.skins = new SkinDef[]
			{
				skinDef,
				skinDef4,
				skinDef2,
				skinDef3
			};
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000072C4 File Offset: 0x000054C4
		private void ItemDisplaySetup()
		{
			this.PopulateDisplays();
			ItemDisplayRuleSet itemDisplayRuleSet = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
			List<ItemDisplayRuleSet.NamedRuleGroup> list = new List<ItemDisplayRuleSet.NamedRuleGroup>();
			List<ItemDisplayRuleSet.NamedRuleGroup> list2 = new List<ItemDisplayRuleSet.NamedRuleGroup>();
			List<ItemDisplayRuleSet.NamedRuleGroup> list3 = list2;
			ItemDisplayRuleSet.NamedRuleGroup item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Jetpack";
			DisplayRuleGroup displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array = new ItemDisplayRule[1];
			int num = 0;
			ItemDisplayRule itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBugWings");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(0f, -12.2f, -23.7f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(12f, 12f, 12f);
			itemDisplayRule.limbMask = 0;
			array[num] = itemDisplayRule;
			displayRuleGroup.rules = array;
			item.displayRuleGroup = displayRuleGroup;
			list3.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list4 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "GoldGat";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array2 = new ItemDisplayRule[1];
			int num2 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGoldGat");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(50.5f, 5.9f, 0f);
			itemDisplayRule.localAngles = new Vector3(68f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array2[num2] = itemDisplayRule;
			displayRuleGroup.rules = array2;
			item.displayRuleGroup = displayRuleGroup;
			list4.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list5 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BFG";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array3 = new ItemDisplayRule[1];
			int num3 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBFG");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(20.5f, -3f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, -58f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array3[num3] = itemDisplayRule;
			displayRuleGroup.rules = array3;
			item.displayRuleGroup = displayRuleGroup;
			list5.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list6 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "CritGlasses";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array4 = new ItemDisplayRule[1];
			int num4 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGlasses");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 0f, 27.8f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array4[num4] = itemDisplayRule;
			displayRuleGroup.rules = array4;
			item.displayRuleGroup = displayRuleGroup;
			list6.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list7 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Syringe";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array5 = new ItemDisplayRule[1];
			int num5 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySyringeCluster");
			itemDisplayRule.childName = "Neck";
			itemDisplayRule.localPos = new Vector3(0f, 17.7f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array5[num5] = itemDisplayRule;
			displayRuleGroup.rules = array5;
			item.displayRuleGroup = displayRuleGroup;
			list7.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list8 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Behemoth";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array6 = new ItemDisplayRule[1];
			int num6 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBehemoth");
			itemDisplayRule.childName = "Weapon";
			itemDisplayRule.localPos = new Vector3(78f, 34.4f, 0f);
			itemDisplayRule.localAngles = new Vector3(-90f, 180f, 90f);
			itemDisplayRule.localScale = new Vector3(12f, 12f, 12f);
			itemDisplayRule.limbMask = 0;
			array6[num6] = itemDisplayRule;
			displayRuleGroup.rules = array6;
			item.displayRuleGroup = displayRuleGroup;
			list8.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list9 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Missile";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array7 = new ItemDisplayRule[1];
			int num7 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayMissileLauncher");
			itemDisplayRule.childName = "R_Clavicle";
			itemDisplayRule.localPos = new Vector3(-41.8f, -14.4f, 10.9f);
			itemDisplayRule.localAngles = new Vector3(180f, 0f, 60f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array7[num7] = itemDisplayRule;
			displayRuleGroup.rules = array7;
			item.displayRuleGroup = displayRuleGroup;
			list9.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list10 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Dagger";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array8 = new ItemDisplayRule[1];
			int num8 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayDagger");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(11f, 0f, 6.3f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(75f, 75f, 75f);
			itemDisplayRule.limbMask = 0;
			array8[num8] = itemDisplayRule;
			displayRuleGroup.rules = array8;
			item.displayRuleGroup = displayRuleGroup;
			list10.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list11 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Hoof";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array9 = new ItemDisplayRule[1];
			int num9 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayHoof");
			itemDisplayRule.childName = "L_Knee";
			itemDisplayRule.localPos = new Vector3(-21.3f, -7.9f, 0.9f);
			itemDisplayRule.localAngles = new Vector3(-24f, 96f, -2f);
			itemDisplayRule.localScale = new Vector3(12f, 12f, 6f);
			itemDisplayRule.limbMask = 0;
			array9[num9] = itemDisplayRule;
			displayRuleGroup.rules = array9;
			item.displayRuleGroup = displayRuleGroup;
			list11.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list12 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ChainLightning";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array10 = new ItemDisplayRule[1];
			int num10 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayUkulele");
			itemDisplayRule.childName = "Weapon";
			itemDisplayRule.localPos = new Vector3(34.1f, 0f, 0f);
			itemDisplayRule.localAngles = new Vector3(90f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array10[num10] = itemDisplayRule;
			displayRuleGroup.rules = array10;
			item.displayRuleGroup = displayRuleGroup;
			list12.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list13 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "GhostOnKill";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array11 = new ItemDisplayRule[1];
			int num11 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayMask");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 3.6f, 23.6f);
			itemDisplayRule.localAngles = new Vector3(-36f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array11[num11] = itemDisplayRule;
			displayRuleGroup.rules = array11;
			item.displayRuleGroup = displayRuleGroup;
			list13.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list14 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Mushroom";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array12 = new ItemDisplayRule[1];
			int num12 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayMushroom");
			itemDisplayRule.childName = "Tail5";
			itemDisplayRule.localPos = new Vector3(11f, 3.6f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array12[num12] = itemDisplayRule;
			displayRuleGroup.rules = array12;
			item.displayRuleGroup = displayRuleGroup;
			list14.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list15 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AttackSpeedOnCrit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array13 = new ItemDisplayRule[1];
			int num13 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWolfPelt");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 8.8f, 15.9f);
			itemDisplayRule.localAngles = new Vector3(12f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array13[num13] = itemDisplayRule;
			displayRuleGroup.rules = array13;
			item.displayRuleGroup = displayRuleGroup;
			list15.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list16 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BleedOnHit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array14 = new ItemDisplayRule[1];
			int num14 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayTriTip");
			itemDisplayRule.childName = "Neck";
			itemDisplayRule.localPos = new Vector3(13.5f, 29.9f, 0f);
			itemDisplayRule.localAngles = new Vector3(52f, -90f, -90f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array14[num14] = itemDisplayRule;
			displayRuleGroup.rules = array14;
			item.displayRuleGroup = displayRuleGroup;
			list16.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list17 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "WardOnLevel";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array15 = new ItemDisplayRule[1];
			int num15 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWarbanner");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(0f, -4.6f, 27.8f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, -90f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array15[num15] = itemDisplayRule;
			displayRuleGroup.rules = array15;
			item.displayRuleGroup = displayRuleGroup;
			list17.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list18 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "HealOnCrit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array16 = new ItemDisplayRule[1];
			int num16 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayScythe");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(-5.1f, 22.2f, -18.2f);
			itemDisplayRule.localAngles = new Vector3(-34f, -60f, 48f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array16[num16] = itemDisplayRule;
			displayRuleGroup.rules = array16;
			item.displayRuleGroup = displayRuleGroup;
			list18.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list19 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "HealWhileSafe";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array17 = new ItemDisplayRule[1];
			int num17 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySnail");
			itemDisplayRule.childName = "Tail10";
			itemDisplayRule.localPos = new Vector3(7f, 0f, -2f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, -90f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array17[num17] = itemDisplayRule;
			displayRuleGroup.rules = array17;
			item.displayRuleGroup = displayRuleGroup;
			list19.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list20 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Clover";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array18 = new ItemDisplayRule[1];
			int num18 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayClover");
			itemDisplayRule.childName = "L_Ear";
			itemDisplayRule.localPos = new Vector3(25.2f, -3.8f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, -90f);
			itemDisplayRule.localScale = new Vector3(44f, 44f, 44f);
			itemDisplayRule.limbMask = 0;
			array18[num18] = itemDisplayRule;
			displayRuleGroup.rules = array18;
			item.displayRuleGroup = displayRuleGroup;
			list20.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list21 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BarrierOnOverHeal";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array19 = new ItemDisplayRule[1];
			int num19 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAegis");
			itemDisplayRule.childName = "R_Elbow";
			itemDisplayRule.localPos = new Vector3(-37.5f, -7.2f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, -90f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array19[num19] = itemDisplayRule;
			displayRuleGroup.rules = array19;
			item.displayRuleGroup = displayRuleGroup;
			list21.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list22 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "GoldOnHit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array20 = new ItemDisplayRule[1];
			int num20 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBoneCrown");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 6.8f, 8f);
			itemDisplayRule.localAngles = new Vector3(22f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(75f, 75f, 75f);
			itemDisplayRule.limbMask = 0;
			array20[num20] = itemDisplayRule;
			displayRuleGroup.rules = array20;
			item.displayRuleGroup = displayRuleGroup;
			list22.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list23 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "WarCryOnMultiKill";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array21 = new ItemDisplayRule[1];
			int num21 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayPauldron");
			itemDisplayRule.childName = "R_Shoulder";
			itemDisplayRule.localPos = new Vector3(-9.3f, -7.5f, 0f);
			itemDisplayRule.localAngles = new Vector3(150f, -90f, 0f);
			itemDisplayRule.localScale = new Vector3(64f, 64f, 64f);
			itemDisplayRule.limbMask = 0;
			array21[num21] = itemDisplayRule;
			displayRuleGroup.rules = array21;
			item.displayRuleGroup = displayRuleGroup;
			list23.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list24 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "SprintArmor";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array22 = new ItemDisplayRule[1];
			int num22 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBuckler");
			itemDisplayRule.childName = "L_Elbow";
			itemDisplayRule.localPos = new Vector3(14.5f, 11.1f, -5.1f);
			itemDisplayRule.localAngles = new Vector3(-114f, 22f, 156f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array22[num22] = itemDisplayRule;
			displayRuleGroup.rules = array22;
			item.displayRuleGroup = displayRuleGroup;
			list24.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list25 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "IceRing";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array23 = new ItemDisplayRule[1];
			int num23 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayIceRing");
			itemDisplayRule.childName = "R_Elbow";
			itemDisplayRule.localPos = new Vector3(-15.5f, -0.2f, 0.7f);
			itemDisplayRule.localAngles = new Vector3(8f, 90f, -45f);
			itemDisplayRule.localScale = new Vector3(64f, 64f, 64f);
			itemDisplayRule.limbMask = 0;
			array23[num23] = itemDisplayRule;
			displayRuleGroup.rules = array23;
			item.displayRuleGroup = displayRuleGroup;
			list25.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list26 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "FireRing";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array24 = new ItemDisplayRule[1];
			int num24 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayFireRing");
			itemDisplayRule.childName = "R_Elbow";
			itemDisplayRule.localPos = new Vector3(-18.8f, 0.3f, 0.7f);
			itemDisplayRule.localAngles = new Vector3(8f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(64f, 64f, 64f);
			itemDisplayRule.limbMask = 0;
			array24[num24] = itemDisplayRule;
			displayRuleGroup.rules = array24;
			item.displayRuleGroup = displayRuleGroup;
			list26.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list27 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "UtilitySkillMagazine";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array25 = new ItemDisplayRule[2];
			int num25 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAfterburnerShoulderRing");
			itemDisplayRule.childName = "L_Shoulder";
			itemDisplayRule.localPos = new Vector3(1.2f, 2.8f, -15.8f);
			itemDisplayRule.localAngles = new Vector3(-118f, -1.5f, 12f);
			itemDisplayRule.localScale = new Vector3(98f, 98f, 98f);
			itemDisplayRule.limbMask = 0;
			array25[num25] = itemDisplayRule;
			int num26 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAfterburnerShoulderRing");
			itemDisplayRule.childName = "R_Shoulder";
			itemDisplayRule.localPos = new Vector3(6.6f, 8.2f, 14.7f);
			itemDisplayRule.localAngles = new Vector3(-218f, -175f, 172f);
			itemDisplayRule.localScale = new Vector3(98f, 98f, 98f);
			itemDisplayRule.limbMask = 0;
			array25[num26] = itemDisplayRule;
			displayRuleGroup.rules = array25;
			item.displayRuleGroup = displayRuleGroup;
			list27.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list28 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "JumpBoost";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array26 = new ItemDisplayRule[1];
			int num27 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWaxBird");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0.36f, -31f, -5.44f);
			itemDisplayRule.localAngles = new Vector3(24f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(76f, 76f, 76f);
			itemDisplayRule.limbMask = 0;
			array26[num27] = itemDisplayRule;
			displayRuleGroup.rules = array26;
			item.displayRuleGroup = displayRuleGroup;
			list28.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list29 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ArmorReductionOnHit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array27 = new ItemDisplayRule[1];
			int num28 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWarhammer");
			itemDisplayRule.childName = "R_Hand";
			itemDisplayRule.localPos = new Vector3(-8.4f, 2.8f, -49.4f);
			itemDisplayRule.localAngles = new Vector3(180f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array27[num28] = itemDisplayRule;
			displayRuleGroup.rules = array27;
			item.displayRuleGroup = displayRuleGroup;
			list29.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list30 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "NearbyDamageBonus";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array28 = new ItemDisplayRule[1];
			int num29 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayDiamond");
			itemDisplayRule.childName = "Weapon";
			itemDisplayRule.localPos = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array28[num29] = itemDisplayRule;
			displayRuleGroup.rules = array28;
			item.displayRuleGroup = displayRuleGroup;
			list30.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list31 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ArmorPlate";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array29 = new ItemDisplayRule[1];
			int num30 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayRepulsionArmorPlate");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(10.3f, -21f, 8f);
			itemDisplayRule.localAngles = new Vector3(-36f, 25f, 9f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array29[num30] = itemDisplayRule;
			displayRuleGroup.rules = array29;
			item.displayRuleGroup = displayRuleGroup;
			list31.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list32 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "CommandMissile";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array30 = new ItemDisplayRule[1];
			int num31 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayMissileRack");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(5f, 11f, -20f);
			itemDisplayRule.localAngles = new Vector3(81f, 94f, -78f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array30[num31] = itemDisplayRule;
			displayRuleGroup.rules = array30;
			item.displayRuleGroup = displayRuleGroup;
			list32.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list33 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Feather";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array31 = new ItemDisplayRule[1];
			int num32 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayFeather");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(8.5f, 0f, 0f);
			itemDisplayRule.localAngles = new Vector3(-90f, -90f, 0f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array31[num32] = itemDisplayRule;
			displayRuleGroup.rules = array31;
			item.displayRuleGroup = displayRuleGroup;
			list33.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list34 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Crowbar";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array32 = new ItemDisplayRule[1];
			int num33 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayCrowbar");
			itemDisplayRule.childName = "Neck";
			itemDisplayRule.localPos = new Vector3(0f, 16.9f, -16.7f);
			itemDisplayRule.localAngles = new Vector3(0f, 180f, 90f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array32[num33] = itemDisplayRule;
			displayRuleGroup.rules = array32;
			item.displayRuleGroup = displayRuleGroup;
			list34.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list35 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "FallBoots";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array33 = new ItemDisplayRule[2];
			int num34 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGravBoots");
			itemDisplayRule.childName = "R_Foot";
			itemDisplayRule.localPos = new Vector3(0f, -4.9f, -4.1f);
			itemDisplayRule.localAngles = new Vector3(14f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array33[num34] = itemDisplayRule;
			int num35 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGravBoots");
			itemDisplayRule.childName = "L_Foot";
			itemDisplayRule.localPos = new Vector3(1.8f, 5.1f, 3.4f);
			itemDisplayRule.localAngles = new Vector3(14f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array33[num35] = itemDisplayRule;
			displayRuleGroup.rules = array33;
			item.displayRuleGroup = displayRuleGroup;
			list35.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list36 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ExecuteLowHealthElite";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array34 = new ItemDisplayRule[1];
			int num36 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGuillotine");
			itemDisplayRule.childName = "Neck";
			itemDisplayRule.localPos = new Vector3(-37f, 1.16f, 7.35f);
			itemDisplayRule.localAngles = new Vector3(-25f, 67f, 56f);
			itemDisplayRule.localScale = new Vector3(12f, 12f, 12f);
			itemDisplayRule.limbMask = 0;
			array34[num36] = itemDisplayRule;
			displayRuleGroup.rules = array34;
			item.displayRuleGroup = displayRuleGroup;
			list36.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list37 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "EquipmentMagazine";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array35 = new ItemDisplayRule[1];
			int num37 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBattery");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(-15f, -2f, -23f);
			itemDisplayRule.localAngles = new Vector3(-50f, -30f, -118f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array35[num37] = itemDisplayRule;
			displayRuleGroup.rules = array35;
			item.displayRuleGroup = displayRuleGroup;
			list37.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list38 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "NovaOnHeal";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array36 = new ItemDisplayRule[2];
			int num38 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayDevilHorns");
			itemDisplayRule.childName = "L_Ear";
			itemDisplayRule.localPos = new Vector3(-12.2f, -11.3f, -0.7f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(64f, 64f, 64f);
			itemDisplayRule.limbMask = 0;
			array36[num38] = itemDisplayRule;
			int num39 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayDevilHorns");
			itemDisplayRule.childName = "R_Ear";
			itemDisplayRule.localPos = new Vector3(5.8f, 7.5f, 1.1f);
			itemDisplayRule.localAngles = new Vector3(180f, 388f, -90f);
			itemDisplayRule.localScale = new Vector3(64f, 64f, 64f);
			itemDisplayRule.limbMask = 0;
			array36[num39] = itemDisplayRule;
			displayRuleGroup.rules = array36;
			item.displayRuleGroup = displayRuleGroup;
			list38.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list39 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Infusion";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array37 = new ItemDisplayRule[1];
			int num40 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayInfusion");
			itemDisplayRule.childName = "Pelvis";
			itemDisplayRule.localPos = new Vector3(-15.43f, 2.42f, 4.39f);
			itemDisplayRule.localAngles = new Vector3(-34f, -54f, 0f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array37[num40] = itemDisplayRule;
			displayRuleGroup.rules = array37;
			item.displayRuleGroup = displayRuleGroup;
			list39.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list40 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Medkit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array38 = new ItemDisplayRule[1];
			int num41 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayMedkit");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(-21.71f, -9.5f, -8.86f);
			itemDisplayRule.localAngles = new Vector3(-90f, -180f, 75f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array38[num41] = itemDisplayRule;
			displayRuleGroup.rules = array38;
			item.displayRuleGroup = displayRuleGroup;
			list40.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list41 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Bandolier";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array39 = new ItemDisplayRule[1];
			int num42 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBandolier");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(-1.8f, 5.2f, -1.2f);
			itemDisplayRule.localAngles = new Vector3(-39f, 79f, -72f);
			itemDisplayRule.localScale = new Vector3(75f, 75f, 75f);
			itemDisplayRule.limbMask = 0;
			array39[num42] = itemDisplayRule;
			displayRuleGroup.rules = array39;
			item.displayRuleGroup = displayRuleGroup;
			list41.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list42 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BounceNearby";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array40 = new ItemDisplayRule[1];
			int num43 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayHook");
			itemDisplayRule.childName = "R_Clavicle";
			itemDisplayRule.localPos = new Vector3(-25.01f, -19.13f, 2.69f);
			itemDisplayRule.localAngles = new Vector3(38f, -28f, 68f);
			itemDisplayRule.localScale = new Vector3(0.5f, 0.5f, 0.5f);
			itemDisplayRule.limbMask = 0;
			array40[num43] = itemDisplayRule;
			displayRuleGroup.rules = array40;
			item.displayRuleGroup = displayRuleGroup;
			list42.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list43 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "IgniteOnKill";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array41 = new ItemDisplayRule[1];
			int num44 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGasoline");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(-10.61f, -10.5f, -23.26f);
			itemDisplayRule.localAngles = new Vector3(-90f, 0f, -60f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array41[num44] = itemDisplayRule;
			displayRuleGroup.rules = array41;
			item.displayRuleGroup = displayRuleGroup;
			list43.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list44 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "StunChanceOnHit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array42 = new ItemDisplayRule[1];
			int num45 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayStunGrenade");
			itemDisplayRule.childName = "R_Hip";
			itemDisplayRule.localPos = new Vector3(12.2f, 2.42f, 7.95f);
			itemDisplayRule.localAngles = new Vector3(0f, -90f, 64f);
			itemDisplayRule.localScale = new Vector3(64f, 64f, 64f);
			itemDisplayRule.limbMask = 0;
			array42[num45] = itemDisplayRule;
			displayRuleGroup.rules = array42;
			item.displayRuleGroup = displayRuleGroup;
			list44.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list45 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Firework";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array43 = new ItemDisplayRule[1];
			int num46 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayFirework");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(-9f, -2f, -24f);
			itemDisplayRule.localAngles = new Vector3(-84f, -96f, 104f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array43[num46] = itemDisplayRule;
			displayRuleGroup.rules = array43;
			item.displayRuleGroup = displayRuleGroup;
			list45.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list46 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "LunarDagger";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array44 = new ItemDisplayRule[1];
			int num47 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayLunarDagger");
			itemDisplayRule.childName = "R_Hand";
			itemDisplayRule.localPos = new Vector3(-19.1f, 2.8f, 22f);
			itemDisplayRule.localAngles = new Vector3(0f, 198f, 0f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array44[num47] = itemDisplayRule;
			displayRuleGroup.rules = array44;
			item.displayRuleGroup = displayRuleGroup;
			list46.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list47 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Knurl";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array45 = new ItemDisplayRule[1];
			int num48 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayKnurl");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(16.33f, -9.61f, 18.24f);
			itemDisplayRule.localAngles = new Vector3(24f, -32f, -16f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array45[num48] = itemDisplayRule;
			displayRuleGroup.rules = array45;
			item.displayRuleGroup = displayRuleGroup;
			list47.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list48 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BeetleGland";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array46 = new ItemDisplayRule[1];
			int num49 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBeetleGland");
			itemDisplayRule.childName = "L_Hip";
			itemDisplayRule.localPos = new Vector3(-15.9f, -12.5f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, -16f, -90f);
			itemDisplayRule.localScale = new Vector3(6f, 6f, 6f);
			itemDisplayRule.limbMask = 0;
			array46[num49] = itemDisplayRule;
			displayRuleGroup.rules = array46;
			item.displayRuleGroup = displayRuleGroup;
			list48.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list49 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "SprintBonus";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array47 = new ItemDisplayRule[1];
			int num50 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySoda");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(-21.65f, -3.88f, -20.38f);
			itemDisplayRule.localAngles = new Vector3(-50f, -20f, 52f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array47[num50] = itemDisplayRule;
			displayRuleGroup.rules = array47;
			item.displayRuleGroup = displayRuleGroup;
			list49.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list50 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "SecondarySkillMagazine";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array48 = new ItemDisplayRule[1];
			int num51 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayDoubleMag");
			itemDisplayRule.childName = "Weapon";
			itemDisplayRule.localPos = new Vector3(10f, 16f, 0f);
			itemDisplayRule.localAngles = new Vector3(90f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array48[num51] = itemDisplayRule;
			displayRuleGroup.rules = array48;
			item.displayRuleGroup = displayRuleGroup;
			list50.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list51 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "StickyBomb";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array49 = new ItemDisplayRule[1];
			int num52 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayStickyBomb");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(-2.2f, 26.1f, -13f);
			itemDisplayRule.localAngles = new Vector3(34f, 12f, -50f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array49[num52] = itemDisplayRule;
			displayRuleGroup.rules = array49;
			item.displayRuleGroup = displayRuleGroup;
			list51.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list52 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "TreasureCache";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array50 = new ItemDisplayRule[1];
			int num53 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayKey");
			itemDisplayRule.childName = "R_Hand";
			itemDisplayRule.localPos = new Vector3(9.05f, -4.67f, -7.79f);
			itemDisplayRule.localAngles = new Vector3(71f, -16f, 0f);
			itemDisplayRule.localScale = new Vector3(88f, 88f, 88f);
			itemDisplayRule.limbMask = 0;
			array50[num53] = itemDisplayRule;
			displayRuleGroup.rules = array50;
			item.displayRuleGroup = displayRuleGroup;
			list52.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list53 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BossDamageBonus";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array51 = new ItemDisplayRule[1];
			int num54 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAPRound");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(-16f, -5.3f, -20.5f);
			itemDisplayRule.localAngles = new Vector3(-79f, -3f, 49f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array51[num54] = itemDisplayRule;
			displayRuleGroup.rules = array51;
			item.displayRuleGroup = displayRuleGroup;
			list53.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list54 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "SlowOnHit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array52 = new ItemDisplayRule[1];
			int num55 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBauble");
			itemDisplayRule.childName = "Tail8";
			itemDisplayRule.localPos = new Vector3(-1.6f, -21.2f, 29.1f);
			itemDisplayRule.localAngles = new Vector3(0f, -114f, 18f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array52[num55] = itemDisplayRule;
			displayRuleGroup.rules = array52;
			item.displayRuleGroup = displayRuleGroup;
			list54.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list55 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ExtraLife";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array53 = new ItemDisplayRule[1];
			int num56 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayHippo");
			itemDisplayRule.childName = "L_Coat2";
			itemDisplayRule.localPos = new Vector3(0f, 3.1f, 0f);
			itemDisplayRule.localAngles = new Vector3(-90f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array53[num56] = itemDisplayRule;
			displayRuleGroup.rules = array53;
			item.displayRuleGroup = displayRuleGroup;
			list55.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list56 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "KillEliteFrenzy";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array54 = new ItemDisplayRule[1];
			int num57 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBrainstalk");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 4.9f, 20.3f);
			itemDisplayRule.localAngles = new Vector3(38f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array54[num57] = itemDisplayRule;
			displayRuleGroup.rules = array54;
			item.displayRuleGroup = displayRuleGroup;
			list56.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list57 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "RepeatHeal";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array55 = new ItemDisplayRule[1];
			int num58 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayCorpseFlower");
			itemDisplayRule.childName = "R_Ear";
			itemDisplayRule.localPos = new Vector3(-29f, 2.27f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 90f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array55[num58] = itemDisplayRule;
			displayRuleGroup.rules = array55;
			item.displayRuleGroup = displayRuleGroup;
			list57.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list58 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AutoCastEquipment";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array56 = new ItemDisplayRule[1];
			int num59 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayFossil");
			itemDisplayRule.childName = "R_Coat2";
			itemDisplayRule.localPos = new Vector3(-8.78f, -3.16f, -12.5f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, -86f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array56[num59] = itemDisplayRule;
			displayRuleGroup.rules = array56;
			item.displayRuleGroup = displayRuleGroup;
			list58.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list59 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "IncreaseHealing";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array57 = new ItemDisplayRule[2];
			int num60 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAntler");
			itemDisplayRule.childName = "L_Ear";
			itemDisplayRule.localPos = new Vector3(-5.9f, 0f, 0f);
			itemDisplayRule.localAngles = new Vector3(24f, 106f, 6f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array57[num60] = itemDisplayRule;
			int num61 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAntler");
			itemDisplayRule.childName = "R_Ear";
			itemDisplayRule.localPos = new Vector3(5.9f, 0f, 0f);
			itemDisplayRule.localAngles = new Vector3(-20f, 284f, 174f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array57[num61] = itemDisplayRule;
			displayRuleGroup.rules = array57;
			item.displayRuleGroup = displayRuleGroup;
			list59.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list60 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "TitanGoldDuringTP";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array58 = new ItemDisplayRule[1];
			int num62 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGoldHeart");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(3.36f, -8.7f, 24.65f);
			itemDisplayRule.localAngles = new Vector3(0f, 32f, -90f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array58[num62] = itemDisplayRule;
			displayRuleGroup.rules = array58;
			item.displayRuleGroup = displayRuleGroup;
			list60.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list61 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "SprintWisp";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array59 = new ItemDisplayRule[1];
			int num63 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBrokenMask");
			itemDisplayRule.childName = "L_Shoulder";
			itemDisplayRule.localPos = new Vector3(13.1f, 4.9f, 5.1f);
			itemDisplayRule.localAngles = new Vector3(-44f, 0f, 90f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array59[num63] = itemDisplayRule;
			displayRuleGroup.rules = array59;
			item.displayRuleGroup = displayRuleGroup;
			list61.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list62 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BarrierOnKill";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array60 = new ItemDisplayRule[1];
			int num64 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBrooch");
			itemDisplayRule.childName = "R_Hand";
			itemDisplayRule.localPos = new Vector3(1.86f, -9f, 2.83f);
			itemDisplayRule.localAngles = new Vector3(184f, 88f, 24f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array60[num64] = itemDisplayRule;
			displayRuleGroup.rules = array60;
			item.displayRuleGroup = displayRuleGroup;
			list62.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list63 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "TPHealingNova";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array61 = new ItemDisplayRule[1];
			int num65 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGlowFlower");
			itemDisplayRule.childName = "Tail9";
			itemDisplayRule.localPos = new Vector3(5f, 2.3f, -1f);
			itemDisplayRule.localAngles = new Vector3(-90f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array61[num65] = itemDisplayRule;
			displayRuleGroup.rules = array61;
			item.displayRuleGroup = displayRuleGroup;
			list63.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list64 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "LunarUtilityReplacement";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array62 = new ItemDisplayRule[1];
			int num66 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBirdFoot");
			itemDisplayRule.childName = "R_Hip";
			itemDisplayRule.localPos = new Vector3(8.9f, 14.7f, 1.5f);
			itemDisplayRule.localAngles = new Vector3(180f, 0f, 90f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array62[num66] = itemDisplayRule;
			displayRuleGroup.rules = array62;
			item.displayRuleGroup = displayRuleGroup;
			list64.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list65 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Thorns";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array63 = new ItemDisplayRule[1];
			int num67 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayRazorwireLeft");
			itemDisplayRule.childName = "L_Shoulder";
			itemDisplayRule.localPos = new Vector3(-4.9f, 0f, -3.5f);
			itemDisplayRule.localAngles = new Vector3(0f, 74f, 0f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array63[num67] = itemDisplayRule;
			displayRuleGroup.rules = array63;
			item.displayRuleGroup = displayRuleGroup;
			list65.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list66 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "LunarPrimaryReplacement";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array64 = new ItemDisplayRule[2];
			int num68 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBirdEye");
			itemDisplayRule.childName = "L_Eye";
			itemDisplayRule.localPos = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localAngles = new Vector3(90f, 0f, 58f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array64[num68] = itemDisplayRule;
			int num69 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBirdEye");
			itemDisplayRule.childName = "R_Eye";
			itemDisplayRule.localPos = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localAngles = new Vector3(-90f, 0f, -58f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array64[num69] = itemDisplayRule;
			displayRuleGroup.rules = array64;
			item.displayRuleGroup = displayRuleGroup;
			list66.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list67 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "NovaOnLowHealth";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array65 = new ItemDisplayRule[1];
			int num70 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayJellyGuts");
			itemDisplayRule.childName = "Tail6";
			itemDisplayRule.localPos = new Vector3(10.5f, 1.7f, -8.1f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(6f, 6f, 6f);
			itemDisplayRule.limbMask = 0;
			array65[num70] = itemDisplayRule;
			displayRuleGroup.rules = array65;
			item.displayRuleGroup = displayRuleGroup;
			list67.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list68 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "LunarTrinket";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array66 = new ItemDisplayRule[1];
			int num71 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBeads");
			itemDisplayRule.childName = "Tail5";
			itemDisplayRule.localPos = new Vector3(8.4f, 1.9f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(100f, 100f, 100f);
			itemDisplayRule.limbMask = 0;
			array66[num71] = itemDisplayRule;
			displayRuleGroup.rules = array66;
			item.displayRuleGroup = displayRuleGroup;
			list68.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list69 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Plant";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array67 = new ItemDisplayRule[1];
			int num72 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayInterstellarDeskPlant");
			itemDisplayRule.childName = "Tail10";
			itemDisplayRule.localPos = new Vector3(0f, 5.93f, -2.51f);
			itemDisplayRule.localAngles = new Vector3(-90f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array67[num72] = itemDisplayRule;
			displayRuleGroup.rules = array67;
			item.displayRuleGroup = displayRuleGroup;
			list69.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list70 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Bear";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array68 = new ItemDisplayRule[1];
			int num73 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBear");
			itemDisplayRule.childName = "R_Coat2";
			itemDisplayRule.localPos = new Vector3(0f, -3.6f, 0f);
			itemDisplayRule.localAngles = new Vector3(90f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array68[num73] = itemDisplayRule;
			displayRuleGroup.rules = array68;
			item.displayRuleGroup = displayRuleGroup;
			list70.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list71 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "DeathMark";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array69 = new ItemDisplayRule[1];
			int num74 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayDeathMark");
			itemDisplayRule.childName = "R_Hand";
			itemDisplayRule.localPos = new Vector3(-15.27f, 12.34f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 180f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array69[num74] = itemDisplayRule;
			displayRuleGroup.rules = array69;
			item.displayRuleGroup = displayRuleGroup;
			list71.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list72 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ExplodeOnDeath";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array70 = new ItemDisplayRule[1];
			int num75 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWilloWisp");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(-0.66f, 20.88f, -23.37f);
			itemDisplayRule.localAngles = new Vector3(24f, 16f, 34f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array70[num75] = itemDisplayRule;
			displayRuleGroup.rules = array70;
			item.displayRuleGroup = displayRuleGroup;
			list72.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list73 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Seed";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array71 = new ItemDisplayRule[1];
			int num76 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySeed");
			itemDisplayRule.childName = "R_Coat2";
			itemDisplayRule.localPos = new Vector3(3.55f, -0.79f, 11f);
			itemDisplayRule.localAngles = new Vector3(-14f, 23f, -62f);
			itemDisplayRule.localScale = new Vector3(3f, 3f, 3f);
			itemDisplayRule.limbMask = 0;
			array71[num76] = itemDisplayRule;
			displayRuleGroup.rules = array71;
			item.displayRuleGroup = displayRuleGroup;
			list73.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list74 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "SprintOutOfCombat";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array72 = new ItemDisplayRule[1];
			int num77 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWhip");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(-23.1f, -11f, -8.1f);
			itemDisplayRule.localAngles = new Vector3(-2f, 164f, 11f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array72[num77] = itemDisplayRule;
			displayRuleGroup.rules = array72;
			item.displayRuleGroup = displayRuleGroup;
			list74.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list75 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "CooldownOnCrit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array73 = new ItemDisplayRule[1];
			int num78 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySkull");
			itemDisplayRule.childName = "L_Hand";
			itemDisplayRule.localPos = new Vector3(-2.3f, 3.8f, 0f);
			itemDisplayRule.localAngles = new Vector3(204f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array73[num78] = itemDisplayRule;
			displayRuleGroup.rules = array73;
			item.displayRuleGroup = displayRuleGroup;
			list75.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list76 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Phasing";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array74 = new ItemDisplayRule[1];
			int num79 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayStealthkit");
			itemDisplayRule.childName = "R_Elbow";
			itemDisplayRule.localPos = new Vector3(-8.4f, -5.8f, 0f);
			itemDisplayRule.localAngles = new Vector3(0f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array74[num79] = itemDisplayRule;
			displayRuleGroup.rules = array74;
			item.displayRuleGroup = displayRuleGroup;
			list76.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list77 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "PersonalShield";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array75 = new ItemDisplayRule[1];
			int num80 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayShieldGenerator");
			itemDisplayRule.childName = "R_Hip";
			itemDisplayRule.localPos = new Vector3(6.75f, 8f, -3.48f);
			itemDisplayRule.localAngles = new Vector3(180f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array75[num80] = itemDisplayRule;
			displayRuleGroup.rules = array75;
			item.displayRuleGroup = displayRuleGroup;
			list77.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list78 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ShockNearby";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array76 = new ItemDisplayRule[1];
			int num81 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayTeslaCoil");
			itemDisplayRule.childName = "R_Elbow";
			itemDisplayRule.localPos = new Vector3(-8.3f, 0f, 6.49f);
			itemDisplayRule.localAngles = new Vector3(90f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array76[num81] = itemDisplayRule;
			displayRuleGroup.rules = array76;
			item.displayRuleGroup = displayRuleGroup;
			list78.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list79 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ShieldOnly";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array77 = new ItemDisplayRule[2];
			int num82 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayShieldBug");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(7.1f, 7.16f, 20.8f);
			itemDisplayRule.localAngles = new Vector3(0f, -90f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array77[num82] = itemDisplayRule;
			int num83 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayShieldBug");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(-6.55f, 7f, 20.8f);
			itemDisplayRule.localAngles = new Vector3(32f, -90f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array77[num83] = itemDisplayRule;
			displayRuleGroup.rules = array77;
			item.displayRuleGroup = displayRuleGroup;
			list79.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list80 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AlienHead";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array78 = new ItemDisplayRule[1];
			int num84 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAlienHead");
			itemDisplayRule.childName = "Nose";
			itemDisplayRule.localPos = new Vector3(9.65f, 8.96f, 0.17f);
			itemDisplayRule.localAngles = new Vector3(-50f, 90f, 0f);
			itemDisplayRule.localScale = new Vector3(64f, 64f, 64f);
			itemDisplayRule.limbMask = 0;
			array78[num84] = itemDisplayRule;
			displayRuleGroup.rules = array78;
			item.displayRuleGroup = displayRuleGroup;
			list80.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list81 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "HeadHunter";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array79 = new ItemDisplayRule[1];
			int num85 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySkullCrown");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 9.1f, 18.1f);
			itemDisplayRule.localAngles = new Vector3(14f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(32f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array79[num85] = itemDisplayRule;
			displayRuleGroup.rules = array79;
			item.displayRuleGroup = displayRuleGroup;
			list81.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list82 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "EnergizedOnEquipmentUse";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array80 = new ItemDisplayRule[1];
			int num86 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWarHorn");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(1.73f, -7.61f, 34.68f);
			itemDisplayRule.localAngles = new Vector3(206f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array80[num86] = itemDisplayRule;
			displayRuleGroup.rules = array80;
			item.displayRuleGroup = displayRuleGroup;
			list82.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list83 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "RegenOnKill";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array81 = new ItemDisplayRule[1];
			int num87 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySteakCurved");
			itemDisplayRule.childName = "Jaw";
			itemDisplayRule.localPos = new Vector3(11f, -5f, 0.2f);
			itemDisplayRule.localAngles = new Vector3(-115f, -86f, 85f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array81[num87] = itemDisplayRule;
			displayRuleGroup.rules = array81;
			item.displayRuleGroup = displayRuleGroup;
			list83.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list84 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Pearl";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array82 = new ItemDisplayRule[1];
			int num88 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayPearl");
			itemDisplayRule.childName = "R_Hand";
			itemDisplayRule.localPos = new Vector3(-46f, 0f, 9.2f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(20f, 20f, 20f);
			itemDisplayRule.limbMask = 0;
			array82[num88] = itemDisplayRule;
			displayRuleGroup.rules = array82;
			item.displayRuleGroup = displayRuleGroup;
			list84.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list85 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "ShinyPearl";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array83 = new ItemDisplayRule[1];
			int num89 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("ShinyPearl");
			itemDisplayRule.childName = "R_Hand";
			itemDisplayRule.localPos = new Vector3(-46f, 0f, 9.2f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(20f, 20f, 20f);
			itemDisplayRule.limbMask = 0;
			array83[num89] = itemDisplayRule;
			displayRuleGroup.rules = array83;
			item.displayRuleGroup = displayRuleGroup;
			list85.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list86 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BonusGoldPackOnKill";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array84 = new ItemDisplayRule[1];
			int num90 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayTome");
			itemDisplayRule.childName = "L_Hip";
			itemDisplayRule.localPos = new Vector3(-7.79f, -6.18f, -6.72f);
			itemDisplayRule.localAngles = new Vector3(-20f, -12f, 90f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array84[num90] = itemDisplayRule;
			displayRuleGroup.rules = array84;
			item.displayRuleGroup = displayRuleGroup;
			list86.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list87 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Squid";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array85 = new ItemDisplayRule[1];
			int num91 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySquidTurret");
			itemDisplayRule.childName = "Tail7";
			itemDisplayRule.localPos = new Vector3(11.6f, 6.8f, -0.2f);
			itemDisplayRule.localAngles = new Vector3(0f, 78f, 0f);
			itemDisplayRule.localScale = new Vector3(6f, 6f, 6f);
			itemDisplayRule.limbMask = 0;
			array85[num91] = itemDisplayRule;
			displayRuleGroup.rules = array85;
			item.displayRuleGroup = displayRuleGroup;
			list87.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list88 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Icicle";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array86 = new ItemDisplayRule[1];
			int num92 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayFrostRelic");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(-48f, -32f, -88f);
			itemDisplayRule.localAngles = new Vector3(90f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(1f, 1f, 1f);
			itemDisplayRule.limbMask = 0;
			array86[num92] = itemDisplayRule;
			displayRuleGroup.rules = array86;
			item.displayRuleGroup = displayRuleGroup;
			list88.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list89 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Talisman";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array87 = new ItemDisplayRule[1];
			int num93 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayTalisman");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(48f, -32f, -88f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(1f, 1f, 1f);
			itemDisplayRule.limbMask = 0;
			array87[num93] = itemDisplayRule;
			displayRuleGroup.rules = array87;
			item.displayRuleGroup = displayRuleGroup;
			list89.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list90 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "LaserTurbine";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array88 = new ItemDisplayRule[1];
			int num94 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayLaserTurbine");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(-48f, 32f, -88f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(1f, 1f, 1f);
			itemDisplayRule.limbMask = 0;
			array88[num94] = itemDisplayRule;
			displayRuleGroup.rules = array88;
			item.displayRuleGroup = displayRuleGroup;
			list90.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list91 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "FocusConvergence";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array89 = new ItemDisplayRule[1];
			int num95 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayFocusedConvergence");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(48f, 32f, -88f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			itemDisplayRule.limbMask = 0;
			array89[num95] = itemDisplayRule;
			displayRuleGroup.rules = array89;
			item.displayRuleGroup = displayRuleGroup;
			list91.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list92 = list;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Incubator";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array90 = new ItemDisplayRule[1];
			int num96 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayAncestralIncubator");
			itemDisplayRule.childName = "Neck";
			itemDisplayRule.localPos = new Vector3(0f, 16.2f, -2.4f);
			itemDisplayRule.localAngles = new Vector3(-12f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array90[num96] = itemDisplayRule;
			displayRuleGroup.rules = array90;
			item.displayRuleGroup = displayRuleGroup;
			list92.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list93 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Fruit";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array91 = new ItemDisplayRule[1];
			int num97 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayFruit");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(6f, -1.3f, 24.5f);
			itemDisplayRule.localAngles = new Vector3(-34f, -84f, 82f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array91[num97] = itemDisplayRule;
			displayRuleGroup.rules = array91;
			item.displayRuleGroup = displayRuleGroup;
			list93.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list94 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AffixRed";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array92 = new ItemDisplayRule[2];
			int num98 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEliteHorn");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(6.7f, 6.2f, 19.4f);
			itemDisplayRule.localAngles = new Vector3(56f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array92[num98] = itemDisplayRule;
			int num99 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEliteHorn");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(-6.1f, 5.5f, 18.3f);
			itemDisplayRule.localAngles = new Vector3(46f, 22f, 45f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array92[num99] = itemDisplayRule;
			displayRuleGroup.rules = array92;
			item.displayRuleGroup = displayRuleGroup;
			list94.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list95 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AffixBlue";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array93 = new ItemDisplayRule[2];
			int num100 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEliteRhinoHorn");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 1.7f, 31f);
			itemDisplayRule.localAngles = new Vector3(-45f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array93[num100] = itemDisplayRule;
			int num101 = 1;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEliteRhinoHorn");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 7.46f, 22.19f);
			itemDisplayRule.localAngles = new Vector3(-45f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array93[num101] = itemDisplayRule;
			displayRuleGroup.rules = array93;
			item.displayRuleGroup = displayRuleGroup;
			list95.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list96 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AffixWhite";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array94 = new ItemDisplayRule[1];
			int num102 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEliteIceCrown");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 12.7f, 20.1f);
			itemDisplayRule.localAngles = new Vector3(-76f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(2f, 2f, 2f);
			itemDisplayRule.limbMask = 0;
			array94[num102] = itemDisplayRule;
			displayRuleGroup.rules = array94;
			item.displayRuleGroup = displayRuleGroup;
			list96.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list97 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AffixPoison";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array95 = new ItemDisplayRule[1];
			int num103 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEliteUrchinCrown");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 9f, 19.1f);
			itemDisplayRule.localAngles = new Vector3(-64f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(5f, 5f, 5f);
			itemDisplayRule.limbMask = 0;
			array95[num103] = itemDisplayRule;
			displayRuleGroup.rules = array95;
			item.displayRuleGroup = displayRuleGroup;
			list97.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list98 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "AffixHaunted";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array96 = new ItemDisplayRule[1];
			int num104 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEliteStealthCrown");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 12.6f, 18.8f);
			itemDisplayRule.localAngles = new Vector3(-75f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array96[num104] = itemDisplayRule;
			displayRuleGroup.rules = array96;
			item.displayRuleGroup = displayRuleGroup;
			list98.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list99 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "CritOnUse";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array97 = new ItemDisplayRule[1];
			int num105 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayNeuralImplant");
			itemDisplayRule.childName = "Head";
			itemDisplayRule.localPos = new Vector3(0f, 0f, 48f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(32f, 32f, 32f);
			itemDisplayRule.limbMask = 0;
			array97[num105] = itemDisplayRule;
			displayRuleGroup.rules = array97;
			item.displayRuleGroup = displayRuleGroup;
			list99.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list100 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "DroneBackup";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array98 = new ItemDisplayRule[1];
			int num106 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayRadio");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(20f, -9.8f, -5.9f);
			itemDisplayRule.localAngles = new Vector3(0f, 86f, 0f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array98[num106] = itemDisplayRule;
			displayRuleGroup.rules = array98;
			item.displayRuleGroup = displayRuleGroup;
			list100.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list101 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Lightning";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array99 = new ItemDisplayRule[1];
			int num107 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayLightningArmRight");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(23.8f, -7.7f, 9.6f);
			itemDisplayRule.localAngles = new Vector3(17f, 22f, -36f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array99[num107] = itemDisplayRule;
			displayRuleGroup.rules = array99;
			item.displayRuleGroup = displayRuleGroup;
			list101.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list102 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "BurnNearby";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array100 = new ItemDisplayRule[1];
			int num108 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayPotion");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(25f, 7.9f, -3.4f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, -90f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array100[num108] = itemDisplayRule;
			displayRuleGroup.rules = array100;
			item.displayRuleGroup = displayRuleGroup;
			list102.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list103 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "CrippleWard";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array101 = new ItemDisplayRule[1];
			int num109 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEffigy");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(15.6f, 0.8f, -8.8f);
			itemDisplayRule.localAngles = new Vector3(-18f, 194f, 48f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array101[num109] = itemDisplayRule;
			displayRuleGroup.rules = array101;
			item.displayRuleGroup = displayRuleGroup;
			list103.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list104 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "QuestVolatileBattery";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array102 = new ItemDisplayRule[1];
			int num110 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayBatteryArray");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(0f, 27f, -16.1f);
			itemDisplayRule.localAngles = new Vector3(48f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array102[num110] = itemDisplayRule;
			displayRuleGroup.rules = array102;
			item.displayRuleGroup = displayRuleGroup;
			list104.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list105 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "GainArmor";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array103 = new ItemDisplayRule[1];
			int num111 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayElephantFigure");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(20f, 9f, -8f);
			itemDisplayRule.localAngles = new Vector3(18f, 24f, -48f);
			itemDisplayRule.localScale = new Vector3(48f, 48f, 48f);
			itemDisplayRule.limbMask = 0;
			array103[num111] = itemDisplayRule;
			displayRuleGroup.rules = array103;
			item.displayRuleGroup = displayRuleGroup;
			list105.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list106 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Recycle";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array104 = new ItemDisplayRule[1];
			int num112 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayRecycler");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(21.25f, -12.14f, -9.25f);
			itemDisplayRule.localAngles = new Vector3(7f, 5f, 0f);
			itemDisplayRule.localScale = new Vector3(4f, 4f, 4f);
			itemDisplayRule.limbMask = 0;
			array104[num112] = itemDisplayRule;
			displayRuleGroup.rules = array104;
			item.displayRuleGroup = displayRuleGroup;
			list106.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list107 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "FireBallDash";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array105 = new ItemDisplayRule[1];
			int num113 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayEgg");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(19.3f, 7f, -5f);
			itemDisplayRule.localAngles = new Vector3(42f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(24f, 24f, 24f);
			itemDisplayRule.limbMask = 0;
			array105[num113] = itemDisplayRule;
			displayRuleGroup.rules = array105;
			item.displayRuleGroup = displayRuleGroup;
			list107.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list108 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Cleanse";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array106 = new ItemDisplayRule[1];
			int num114 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayWaterPack");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(0f, -15.5f, -24.5f);
			itemDisplayRule.localAngles = new Vector3(0f, 180f, 0f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array106[num114] = itemDisplayRule;
			displayRuleGroup.rules = array106;
			item.displayRuleGroup = displayRuleGroup;
			list108.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list109 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Tonic";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array107 = new ItemDisplayRule[1];
			int num115 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayTonic");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(22.66f, 9.83f, -5.04f);
			itemDisplayRule.localAngles = new Vector3(28f, 121f, 35f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array107[num115] = itemDisplayRule;
			displayRuleGroup.rules = array107;
			item.displayRuleGroup = displayRuleGroup;
			list109.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list110 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Gateway";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array108 = new ItemDisplayRule[1];
			int num116 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayVase");
			itemDisplayRule.childName = "L_Clavicle";
			itemDisplayRule.localPos = new Vector3(20.5f, 11.7f, -12f);
			itemDisplayRule.localAngles = new Vector3(-21f, 66f, -48f);
			itemDisplayRule.localScale = new Vector3(16f, 16f, 16f);
			itemDisplayRule.limbMask = 0;
			array108[num116] = itemDisplayRule;
			displayRuleGroup.rules = array108;
			item.displayRuleGroup = displayRuleGroup;
			list110.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list111 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Meteor";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array109 = new ItemDisplayRule[1];
			int num117 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayMeteor");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(0f, 10.5f, -128f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(1f, 1f, 1f);
			itemDisplayRule.limbMask = 0;
			array109[num117] = itemDisplayRule;
			displayRuleGroup.rules = array109;
			item.displayRuleGroup = displayRuleGroup;
			list111.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list112 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Saw";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array110 = new ItemDisplayRule[1];
			int num118 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplaySawmerang");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(0f, 10.5f, -80f);
			itemDisplayRule.localAngles = new Vector3(90f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(0.2f, 0.2f, 0.2f);
			itemDisplayRule.limbMask = 0;
			array110[num118] = itemDisplayRule;
			displayRuleGroup.rules = array110;
			item.displayRuleGroup = displayRuleGroup;
			list112.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list113 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Blackhole";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array111 = new ItemDisplayRule[1];
			int num119 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayGravCube");
			itemDisplayRule.childName = "Spine2";
			itemDisplayRule.localPos = new Vector3(0f, 10.5f, -128f);
			itemDisplayRule.localAngles = new Vector3(0f, 0f, 0f);
			itemDisplayRule.localScale = new Vector3(1f, 1f, 1f);
			itemDisplayRule.limbMask = 0;
			array111[num119] = itemDisplayRule;
			displayRuleGroup.rules = array111;
			item.displayRuleGroup = displayRuleGroup;
			list113.Add(item);
			List<ItemDisplayRuleSet.NamedRuleGroup> list114 = list2;
			item = default(ItemDisplayRuleSet.NamedRuleGroup);
			item.name = "Scanner";
			displayRuleGroup = default(DisplayRuleGroup);
			ItemDisplayRule[] array112 = new ItemDisplayRule[1];
			int num120 = 0;
			itemDisplayRule = default(ItemDisplayRule);
			itemDisplayRule.ruleType = 0;
			itemDisplayRule.followerPrefab = this.LoadDisplay("DisplayScanner");
			itemDisplayRule.childName = "Spine1";
			itemDisplayRule.localPos = new Vector3(31.7f, -6.92f, -10.78f);
			itemDisplayRule.localAngles = new Vector3(-35f, -78f, -18f);
			itemDisplayRule.localScale = new Vector3(8f, 8f, 8f);
			itemDisplayRule.limbMask = 0;
			array112[num120] = itemDisplayRule;
			displayRuleGroup.rules = array112;
			item.displayRuleGroup = displayRuleGroup;
			list114.Add(item);
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			ItemDisplayRuleSet.NamedRuleGroup[] value = list.ToArray();
			ItemDisplayRuleSet.NamedRuleGroup[] value2 = list2.ToArray();
			typeof(ItemDisplayRuleSet).GetField("namedItemRuleGroups", bindingAttr).SetValue(itemDisplayRuleSet, value);
			typeof(ItemDisplayRuleSet).GetField("namedEquipmentRuleGroups", bindingAttr).SetValue(itemDisplayRuleSet, value2);
			Twitch.characterPrefab.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet = itemDisplayRuleSet;
		}

		// Token: 0x06000063 RID: 99 RVA: 0x0000D0A4 File Offset: 0x0000B2A4
		private GameObject LoadDisplay(string name)
		{
			bool flag = Twitch.itemDisplayPrefabs.ContainsKey(name.ToLower());
			if (flag)
			{
				bool flag2 = Twitch.itemDisplayPrefabs[name.ToLower()];
				if (flag2)
				{
					return Twitch.itemDisplayPrefabs[name.ToLower()];
				}
			}
			return null;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x0000D0FC File Offset: 0x0000B2FC
		private void PopulateDisplays()
		{
			ItemDisplayRuleSet itemDisplayRuleSet = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet;
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			ItemDisplayRuleSet.NamedRuleGroup[] array = typeof(ItemDisplayRuleSet).GetField("namedItemRuleGroups", bindingAttr).GetValue(itemDisplayRuleSet) as ItemDisplayRuleSet.NamedRuleGroup[];
			ItemDisplayRuleSet.NamedRuleGroup[] array2 = typeof(ItemDisplayRuleSet).GetField("namedEquipmentRuleGroups", bindingAttr).GetValue(itemDisplayRuleSet) as ItemDisplayRuleSet.NamedRuleGroup[];
			ItemDisplayRuleSet.NamedRuleGroup[] array3 = array;
			for (int i = 0; i < array3.Length; i++)
			{
				ItemDisplayRule[] rules = array3[i].displayRuleGroup.rules;
				for (int j = 0; j < rules.Length; j++)
				{
					GameObject followerPrefab = rules[j].followerPrefab;
					bool flag = !(followerPrefab == null);
					if (flag)
					{
						string name = followerPrefab.name;
						string key = (name != null) ? name.ToLower() : null;
						bool flag2 = !Twitch.itemDisplayPrefabs.ContainsKey(key);
						if (flag2)
						{
							Twitch.itemDisplayPrefabs[key] = followerPrefab;
						}
					}
				}
			}
			array3 = array2;
			for (int k = 0; k < array3.Length; k++)
			{
				ItemDisplayRule[] rules2 = array3[k].displayRuleGroup.rules;
				for (int l = 0; l < rules2.Length; l++)
				{
					GameObject followerPrefab2 = rules2[l].followerPrefab;
					bool flag3 = !(followerPrefab2 == null);
					if (flag3)
					{
						string name2 = followerPrefab2.name;
						string key2 = (name2 != null) ? name2.ToLower() : null;
						bool flag4 = !Twitch.itemDisplayPrefabs.ContainsKey(key2);
						if (flag4)
						{
							Twitch.itemDisplayPrefabs[key2] = followerPrefab2;
						}
					}
				}
			}
		}

		// Token: 0x06000065 RID: 101 RVA: 0x0000D2DC File Offset: 0x0000B4DC
		private void SkillSetup()
		{
			foreach (GenericSkill genericSkill in Twitch.characterPrefab.GetComponentsInChildren<GenericSkill>())
			{
				Object.DestroyImmediate(genericSkill);
			}
			this.PassiveSetup();
			this.PrimarySetup();
			this.SecondarySetup();
			this.UtilitySetup();
			this.SpecialSetup();
		}

		// Token: 0x06000066 RID: 102 RVA: 0x0000D334 File Offset: 0x0000B534
		private void RegisterStates()
		{
			LoadoutAPI.AddSkill(typeof(TwitchFireBolt));
			LoadoutAPI.AddSkill(typeof(TwitchFireSMG));
			LoadoutAPI.AddSkill(typeof(TwitchFireShotgun));
			LoadoutAPI.AddSkill(typeof(TwitchChargeBazooka));
			LoadoutAPI.AddSkill(typeof(TwitchFireBazooka));
			LoadoutAPI.AddSkill(typeof(TwitchExpunge));
			LoadoutAPI.AddSkill(typeof(TwitchAmbush));
			LoadoutAPI.AddSkill(typeof(TwitchThrowBomb));
			LoadoutAPI.AddSkill(typeof(TwitchThrowGrenade));
			LoadoutAPI.AddSkill(typeof(TwitchCheese));
			LoadoutAPI.AddSkill(typeof(TwitchScurry));
		}

		// Token: 0x06000067 RID: 103 RVA: 0x0000D3F4 File Offset: 0x0000B5F4
		private void PassiveSetup()
		{
			SkillLocator component = Twitch.characterPrefab.GetComponent<SkillLocator>();
			LanguageAPI.Add("TWITCH_PASSIVE_NAME", "Deadly Venom");
			LanguageAPI.Add("TWITCH_PASSIVE_DESCRIPTION", "Certain attacks apply a <style=cIsHealing>stacking venom</style>, lowering <style=cIsUtility>attack speed</style> and <style=cIsUtility>armor</style>.");
			component.passiveSkill.enabled = true;
			component.passiveSkill.skillNameToken = "TWITCH_PASSIVE_NAME";
			component.passiveSkill.skillDescriptionToken = "TWITCH_PASSIVE_DESCRIPTION";
			component.passiveSkill.icon = Assets.iconP;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x0000D46C File Offset: 0x0000B66C
		private void PrimarySetup()
		{
			SkillLocator component = Twitch.characterPrefab.GetComponent<SkillLocator>();
			LanguageAPI.Add("KEYWORD_VENOMOUS", "<style=cKeywordName>Venomous</style><style=cSub>Reduce <style=cIsUtility>attack speed</style> and <style=cIsUtility>armor</style> by a small amount per stack.</style>");
			LanguageAPI.Add("TWITCH_PRIMARY_CROSSBOW_NAME", "Crossbow");
			LanguageAPI.Add("TWITCH_PRIMARY_CROSSBOW_DESCRIPTION", "<style=cIsHealing>Venomous</style>. Fire a tipped arrow, dealing <style=cIsDamage>225% damage</style>. <style=cIsUtility>Reduce Ambush cooldown on hit.</style>");
			SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
			skillDef.activationState = new SerializableEntityStateType(typeof(TwitchFireBolt));
			skillDef.activationStateMachineName = "Weapon";
			skillDef.baseMaxStock = 1;
			skillDef.baseRechargeInterval = 0f;
			skillDef.beginSkillCooldownOnSkillEnd = false;
			skillDef.canceledFromSprinting = false;
			skillDef.fullRestockOnAssign = true;
			skillDef.interruptPriority = 0;
			skillDef.isBullets = false;
			skillDef.isCombatSkill = true;
			skillDef.mustKeyPress = false;
			skillDef.noSprint = true;
			skillDef.rechargeStock = 1;
			skillDef.requiredStock = 1;
			skillDef.shootDelay = 0f;
			skillDef.stockToConsume = 1;
			skillDef.icon = Assets.icon1;
			skillDef.skillDescriptionToken = "TWITCH_PRIMARY_CROSSBOW_DESCRIPTION";
			skillDef.skillName = "TWITCH_PRIMARY_CROSSBOW_NAME";
			skillDef.skillNameToken = "TWITCH_PRIMARY_CROSSBOW_NAME";
			skillDef.keywordTokens = new string[]
			{
				"KEYWORD_VENOMOUS"
			};
			LoadoutAPI.AddSkillDef(skillDef);
			component.primary = Twitch.characterPrefab.AddComponent<GenericSkill>();
			SkillFamily skillFamily = ScriptableObject.CreateInstance<SkillFamily>();
			skillFamily.variants = new SkillFamily.Variant[1];
			LoadoutAPI.AddSkillFamily(skillFamily);
			Reflection.SetFieldValue<SkillFamily>(component.primary, "_skillFamily", skillFamily);
			SkillFamily skillFamily2 = component.primary.skillFamily;
			SkillFamily.Variant[] variants = skillFamily2.variants;
			int num = 0;
			SkillFamily.Variant variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants[num] = variant;
			LanguageAPI.Add("TWITCH_PRIMARY_SMG_NAME", "Tommy Gun");
			LanguageAPI.Add("TWITCH_PRIMARY_SMG_DESCRIPTION", "<style=cIsHealing>Venomous</style>. Fire a hail of venom-soaked bullets, dealing <style=cIsDamage>3x85% damage</style>.");
			skillDef = ScriptableObject.CreateInstance<SkillDef>();
			skillDef.activationState = new SerializableEntityStateType(typeof(TwitchFireSMG));
			skillDef.activationStateMachineName = "Weapon";
			skillDef.baseMaxStock = 1;
			skillDef.baseRechargeInterval = 0f;
			skillDef.beginSkillCooldownOnSkillEnd = false;
			skillDef.canceledFromSprinting = false;
			skillDef.fullRestockOnAssign = true;
			skillDef.interruptPriority = 0;
			skillDef.isBullets = false;
			skillDef.isCombatSkill = true;
			skillDef.mustKeyPress = false;
			skillDef.noSprint = true;
			skillDef.rechargeStock = 1;
			skillDef.requiredStock = 1;
			skillDef.shootDelay = 0f;
			skillDef.stockToConsume = 1;
			skillDef.icon = Assets.icon1b;
			skillDef.skillDescriptionToken = "TWITCH_PRIMARY_SMG_DESCRIPTION";
			skillDef.skillName = "TWITCH_PRIMARY_SMG_NAME";
			skillDef.skillNameToken = "TWITCH_PRIMARY_SMG_NAME";
			skillDef.keywordTokens = new string[]
			{
				"KEYWORD_VENOMOUS"
			};
			LoadoutAPI.AddSkillDef(skillDef);
			Array.Resize<SkillFamily.Variant>(ref skillFamily2.variants, skillFamily2.variants.Length + 1);
			SkillFamily.Variant[] variants2 = skillFamily2.variants;
			int num2 = skillFamily2.variants.Length - 1;
			variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants2[num2] = variant;
			LanguageAPI.Add("TWITCH_PRIMARY_SHOTGUN_NAME", "Street Sweeper");
			LanguageAPI.Add("TWITCH_PRIMARY_SHOTGUN_DESCRIPTION", "<style=cIsHealing>Venomous</style>. Fire a close range burst of venom-soaked shells, dealing <style=cIsDamage>4x90% damage</style>.");
			skillDef = ScriptableObject.CreateInstance<SkillDef>();
			skillDef.activationState = new SerializableEntityStateType(typeof(TwitchFireShotgun));
			skillDef.activationStateMachineName = "Weapon";
			skillDef.baseMaxStock = 1;
			skillDef.baseRechargeInterval = 0f;
			skillDef.beginSkillCooldownOnSkillEnd = false;
			skillDef.canceledFromSprinting = false;
			skillDef.fullRestockOnAssign = true;
			skillDef.interruptPriority = 0;
			skillDef.isBullets = false;
			skillDef.isCombatSkill = true;
			skillDef.mustKeyPress = false;
			skillDef.noSprint = true;
			skillDef.rechargeStock = 1;
			skillDef.requiredStock = 1;
			skillDef.shootDelay = 0f;
			skillDef.stockToConsume = 1;
			skillDef.icon = Assets.icon1c;
			skillDef.skillDescriptionToken = "TWITCH_PRIMARY_SHOTGUN_DESCRIPTION";
			skillDef.skillName = "TWITCH_PRIMARY_SHOTGUN_NAME";
			skillDef.skillNameToken = "TWITCH_PRIMARY_SHOTGUN_NAME";
			skillDef.keywordTokens = new string[]
			{
				"KEYWORD_VENOMOUS"
			};
			LoadoutAPI.AddSkillDef(skillDef);
			Array.Resize<SkillFamily.Variant>(ref skillFamily2.variants, skillFamily2.variants.Length + 1);
			SkillFamily.Variant[] variants3 = skillFamily2.variants;
			int num3 = skillFamily2.variants.Length - 1;
			variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants3[num3] = variant;
			bool value = Twitch.boom.Value;
			if (value)
			{
				LanguageAPI.Add("TWITCH_PRIMARY_BAZOOKA_NAME", "Bazooka");
				LanguageAPI.Add("TWITCH_PRIMARY_BAZOOKA_DESCRIPTION", "Charge up and fire a <style=cIsUtility>rocket</style> that deals <style=cIsDamage>50-750% damage</style> based on charge. <style=cIsDamage>Direct hits deal triple damage</style>.");
				skillDef = ScriptableObject.CreateInstance<SkillDef>();
				skillDef.activationState = new SerializableEntityStateType(typeof(TwitchChargeBazooka));
				skillDef.activationStateMachineName = "Weapon";
				skillDef.baseMaxStock = 1;
				skillDef.baseRechargeInterval = 0f;
				skillDef.beginSkillCooldownOnSkillEnd = false;
				skillDef.canceledFromSprinting = false;
				skillDef.fullRestockOnAssign = true;
				skillDef.interruptPriority = 0;
				skillDef.isBullets = false;
				skillDef.isCombatSkill = true;
				skillDef.mustKeyPress = false;
				skillDef.noSprint = true;
				skillDef.rechargeStock = 1;
				skillDef.requiredStock = 1;
				skillDef.shootDelay = 0f;
				skillDef.stockToConsume = 1;
				skillDef.icon = Assets.icon1d;
				skillDef.skillDescriptionToken = "TWITCH_PRIMARY_BAZOOKA_DESCRIPTION";
				skillDef.skillName = "TWITCH_PRIMARY_BAZOOKA_NAME";
				skillDef.skillNameToken = "TWITCH_PRIMARY_BAZOOKA_NAME";
				LoadoutAPI.AddSkillDef(skillDef);
				Array.Resize<SkillFamily.Variant>(ref skillFamily2.variants, skillFamily2.variants.Length + 1);
				SkillFamily.Variant[] variants4 = skillFamily2.variants;
				int num4 = skillFamily2.variants.Length - 1;
				variant = default(SkillFamily.Variant);
				variant.skillDef = skillDef;
				variant.unlockableName = "";
				variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
				variants4[num4] = variant;
			}
		}

		// Token: 0x06000069 RID: 105 RVA: 0x0000DA14 File Offset: 0x0000BC14
		private void SecondarySetup()
		{
			SkillLocator component = Twitch.characterPrefab.GetComponent<SkillLocator>();
			LanguageAPI.Add("TWITCH_SECONDARY_CASK_NAME", "Venom Cask");
			LanguageAPI.Add("TWITCH_SECONDARY_CASK_DESCRIPTION", "<style=cIsHealing>Venomous</style>. Hurl a cask of <style=cIsUtility>venom</style> that explodes for <style=cIsDamage>300% damage</style> and leaves a pool of <style=cIsUtility>venom</style>.");
			SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
			skillDef.activationState = new SerializableEntityStateType(typeof(TwitchThrowBomb));
			skillDef.activationStateMachineName = "Weapon";
			skillDef.baseMaxStock = 1;
			skillDef.baseRechargeInterval = 8f;
			skillDef.beginSkillCooldownOnSkillEnd = false;
			skillDef.canceledFromSprinting = false;
			skillDef.fullRestockOnAssign = true;
			skillDef.interruptPriority = 1;
			skillDef.isBullets = false;
			skillDef.isCombatSkill = true;
			skillDef.mustKeyPress = false;
			skillDef.noSprint = true;
			skillDef.rechargeStock = 1;
			skillDef.requiredStock = 1;
			skillDef.shootDelay = 0f;
			skillDef.stockToConsume = 1;
			skillDef.icon = Assets.icon2;
			skillDef.skillDescriptionToken = "TWITCH_SECONDARY_CASK_DESCRIPTION";
			skillDef.skillName = "TWITCH_SECONDARY_CASK_NAME";
			skillDef.skillNameToken = "TWITCH_SECONDARY_CASK_NAME";
			skillDef.keywordTokens = new string[]
			{
				"KEYWORD_VENOMOUS"
			};
			LoadoutAPI.AddSkillDef(skillDef);
			component.secondary = Twitch.characterPrefab.AddComponent<GenericSkill>();
			SkillFamily skillFamily = ScriptableObject.CreateInstance<SkillFamily>();
			skillFamily.variants = new SkillFamily.Variant[1];
			LoadoutAPI.AddSkillFamily(skillFamily);
			Reflection.SetFieldValue<SkillFamily>(component.secondary, "_skillFamily", skillFamily);
			SkillFamily skillFamily2 = component.secondary.skillFamily;
			SkillFamily.Variant[] variants = skillFamily2.variants;
			int num = 0;
			SkillFamily.Variant variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants[num] = variant;
			LanguageAPI.Add("TWITCH_SECONDARY_FRAG_NAME", "Hand Grenade");
			LanguageAPI.Add("TWITCH_SECONDARY_FRAG_DESCRIPTION", "Throw a grenade that deals <style=cIsDamage>750% damage</style>. <style=cIsUtility>Hold</style> to let the grenade cook before throwing, <style=cIsHealth>but don't hold it for too long</style>!");
			skillDef = ScriptableObject.CreateInstance<SkillDef>();
			skillDef.activationState = new SerializableEntityStateType(typeof(TwitchThrowGrenade));
			skillDef.activationStateMachineName = "Weapon";
			skillDef.baseMaxStock = 2;
			skillDef.baseRechargeInterval = 12f;
			skillDef.beginSkillCooldownOnSkillEnd = false;
			skillDef.canceledFromSprinting = false;
			skillDef.fullRestockOnAssign = true;
			skillDef.interruptPriority = 1;
			skillDef.isBullets = false;
			skillDef.isCombatSkill = true;
			skillDef.mustKeyPress = false;
			skillDef.noSprint = true;
			skillDef.rechargeStock = 1;
			skillDef.requiredStock = 1;
			skillDef.shootDelay = 0f;
			skillDef.stockToConsume = 1;
			skillDef.icon = Assets.icon2b;
			skillDef.skillDescriptionToken = "TWITCH_SECONDARY_FRAG_DESCRIPTION";
			skillDef.skillName = "TWITCH_SECONDARY_FRAG_NAME";
			skillDef.skillNameToken = "TWITCH_SECONDARY_FRAG_NAME";
			LoadoutAPI.AddSkillDef(skillDef);
			Array.Resize<SkillFamily.Variant>(ref skillFamily2.variants, skillFamily2.variants.Length + 1);
			SkillFamily.Variant[] variants2 = skillFamily2.variants;
			int num2 = skillFamily2.variants.Length - 1;
			variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants2[num2] = variant;
		}

		// Token: 0x0600006A RID: 106 RVA: 0x0000DCEC File Offset: 0x0000BEEC
		private void UtilitySetup()
		{
			SkillLocator component = Twitch.characterPrefab.GetComponent<SkillLocator>();
			LanguageAPI.Add("TWITCH_UTILITY_AMBUSH_NAME", "Ambush");
			LanguageAPI.Add("TWITCH_UTILITY_AMBUSH_DESCRIPTION", "Turn <style=cIsUtility>invisible</style> for 6 seconds. Upon exiting stealth, gain <style=cIsDamage>bonus attack speed</style> and <style=cIsUtility>piercing shots</style> for 5 seconds.");
			SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
			skillDef.activationState = new SerializableEntityStateType(typeof(TwitchAmbush));
			skillDef.activationStateMachineName = "Weapon";
			skillDef.baseMaxStock = 1;
			skillDef.baseRechargeInterval = 24f;
			skillDef.beginSkillCooldownOnSkillEnd = true;
			skillDef.canceledFromSprinting = false;
			skillDef.fullRestockOnAssign = false;
			skillDef.interruptPriority = 1;
			skillDef.isBullets = false;
			skillDef.isCombatSkill = false;
			skillDef.mustKeyPress = false;
			skillDef.noSprint = false;
			skillDef.rechargeStock = 1;
			skillDef.requiredStock = 1;
			skillDef.shootDelay = 0f;
			skillDef.stockToConsume = 1;
			skillDef.icon = Assets.icon3;
			skillDef.skillDescriptionToken = "TWITCH_UTILITY_AMBUSH_DESCRIPTION";
			skillDef.skillName = "TWITCH_UTILITY_AMBUSH_NAME";
			skillDef.skillNameToken = "TWITCH_UTILITY_AMBUSH_NAME";
			LoadoutAPI.AddSkillDef(skillDef);
			component.utility = Twitch.characterPrefab.AddComponent<GenericSkill>();
			SkillFamily skillFamily = ScriptableObject.CreateInstance<SkillFamily>();
			skillFamily.variants = new SkillFamily.Variant[1];
			LoadoutAPI.AddSkillFamily(skillFamily);
			Reflection.SetFieldValue<SkillFamily>(component.utility, "_skillFamily", skillFamily);
			SkillFamily skillFamily2 = component.utility.skillFamily;
			SkillFamily.Variant[] variants = skillFamily2.variants;
			int num = 0;
			SkillFamily.Variant variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants[num] = variant;
			Twitch.ambushDef = skillDef;
			LanguageAPI.Add("TWITCH_UTILITY_AMBUSHACTIVE_NAME", "Spray and Pray");
			LanguageAPI.Add("TWITCH_UTILITY_AMBUSHACTIVE_DESCRIPTION", "Gain <style=cIsDamage>bonus attack speed</style> and <style=cIsUtility>piercing shots</style> for 5 seconds upon exiting stealth.");
			SkillDef skillDef2 = ScriptableObject.CreateInstance<SkillDef>();
			skillDef2.activationState = new SerializableEntityStateType(typeof(TwitchAmbush));
			skillDef2.activationStateMachineName = "Weapon";
			skillDef2.baseMaxStock = 0;
			skillDef2.baseRechargeInterval = 0f;
			skillDef2.beginSkillCooldownOnSkillEnd = false;
			skillDef2.canceledFromSprinting = false;
			skillDef2.fullRestockOnAssign = false;
			skillDef2.interruptPriority = 0;
			skillDef2.isBullets = false;
			skillDef2.isCombatSkill = false;
			skillDef2.mustKeyPress = false;
			skillDef2.noSprint = false;
			skillDef2.rechargeStock = 0;
			skillDef2.requiredStock = 100;
			skillDef2.shootDelay = 0f;
			skillDef2.stockToConsume = 0;
			skillDef2.icon = Assets.icon3b;
			skillDef2.skillDescriptionToken = "TWITCH_UTILITY_AMBUSHACTIVE_DESCRIPTION";
			skillDef2.skillName = "TWITCH_UTILITY_AMBUSHACTIVE_NAME";
			skillDef2.skillNameToken = "TWITCH_UTILITY_AMBUSHACTIVE_NAME";
			LoadoutAPI.AddSkillDef(skillDef2);
			Twitch.ambushActiveDef = skillDef2;
			LanguageAPI.Add("TWITCH_UTILITY_SCURRY_NAME", "Scurry");
			LanguageAPI.Add("TWITCH_UTILITY_SCURRY_DESCRIPTION", "Turn <style=cIsUtility>invisible</style> and <style=cIsUtility>dash</style> a short distance. Gain <style=cIsDamage>bonus attack speed</style> and <style=cIsUtility>piercing shots</style> for 2 seconds.");
			SkillDef skillDef3 = ScriptableObject.CreateInstance<SkillDef>();
			skillDef3.activationState = new SerializableEntityStateType(typeof(TwitchScurry));
			skillDef3.activationStateMachineName = "Body";
			skillDef3.baseMaxStock = 1;
			skillDef3.baseRechargeInterval = 6f;
			skillDef3.beginSkillCooldownOnSkillEnd = false;
			skillDef3.canceledFromSprinting = true;
			skillDef3.fullRestockOnAssign = true;
			skillDef3.interruptPriority = 1;
			skillDef3.isBullets = false;
			skillDef3.isCombatSkill = false;
			skillDef3.mustKeyPress = false;
			skillDef3.noSprint = true;
			skillDef3.rechargeStock = 1;
			skillDef3.requiredStock = 1;
			skillDef3.shootDelay = 0f;
			skillDef3.stockToConsume = 1;
			skillDef3.icon = Assets.icon3c;
			skillDef3.skillDescriptionToken = "TWITCH_UTILITY_SCURRY_DESCRIPTION";
			skillDef3.skillName = "TWITCH_UTILITY_SCURRY_NAME";
			skillDef3.skillNameToken = "TWITCH_UTILITY_SCURRY_NAME";
			LoadoutAPI.AddSkillDef(skillDef3);
			Array.Resize<SkillFamily.Variant>(ref skillFamily2.variants, skillFamily2.variants.Length + 1);
			SkillFamily.Variant[] variants2 = skillFamily2.variants;
			int num2 = skillFamily2.variants.Length - 1;
			variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef3;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants2[num2] = variant;
			LanguageAPI.Add("TWITCH_UTILITY_CHEESE_NAME", "Cheese");
			LanguageAPI.Add("TWITCH_UTILITY_CHEESE_DESCRIPTION", "Relax and have some <style=cIsUtility>cheese</style>. <style=cIsHealing>Restores 100% of your max health</style>.");
			SkillDef skillDef4 = ScriptableObject.CreateInstance<SkillDef>();
			skillDef4.activationState = new SerializableEntityStateType(typeof(TwitchCheese));
			skillDef4.activationStateMachineName = "Body";
			skillDef4.baseMaxStock = 1;
			skillDef4.baseRechargeInterval = 32f;
			skillDef4.beginSkillCooldownOnSkillEnd = false;
			skillDef4.canceledFromSprinting = true;
			skillDef4.fullRestockOnAssign = true;
			skillDef4.interruptPriority = 1;
			skillDef4.isBullets = false;
			skillDef4.isCombatSkill = false;
			skillDef4.mustKeyPress = false;
			skillDef4.noSprint = true;
			skillDef4.rechargeStock = 1;
			skillDef4.requiredStock = 1;
			skillDef4.shootDelay = 0f;
			skillDef4.stockToConsume = 1;
			skillDef4.icon = Assets.icon3d;
			skillDef4.skillDescriptionToken = "TWITCH_UTILITY_CHEESE_DESCRIPTION";
			skillDef4.skillName = "TWITCH_UTILITY_CHEESE_NAME";
			skillDef4.skillNameToken = "TWITCH_UTILITY_CHEESE_NAME";
			LoadoutAPI.AddSkillDef(skillDef4);
			Array.Resize<SkillFamily.Variant>(ref skillFamily2.variants, skillFamily2.variants.Length + 1);
			SkillFamily.Variant[] variants3 = skillFamily2.variants;
			int num3 = skillFamily2.variants.Length - 1;
			variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef4;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants3[num3] = variant;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x0000E228 File Offset: 0x0000C428
		private void SpecialSetup()
		{
			SkillLocator component = Twitch.characterPrefab.GetComponent<SkillLocator>();
			LanguageAPI.Add("KEYWORD_INFECTION", "<style=cKeywordName>Infectious</style><style=cSub>Reduce <style=cIsUtility>attack speed</style> and <style=cIsUtility>armor</style> by a large amount per stack <style=cIsHealth>permanently</style>.");
			LanguageAPI.Add("TWITCH_SPECIAL_EXPUNGE_NAME", "Expunge");
			LanguageAPI.Add("TWITCH_SPECIAL_EXPUNGE_DESCRIPTION", "<style=cIsDamage>Infectious</style>. Throw an <style=cIsUtility>infected knife</style> that deals <style=cIsDamage>400(+70 per venom stack)% damage</style>. Target becomes <style=cIsHealth>immune to venom</style>.");
			SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
			skillDef.activationState = new SerializableEntityStateType(typeof(TwitchExpunge));
			skillDef.activationStateMachineName = "Weapon";
			skillDef.baseMaxStock = 1;
			skillDef.baseRechargeInterval = 8f;
			skillDef.beginSkillCooldownOnSkillEnd = false;
			skillDef.canceledFromSprinting = false;
			skillDef.fullRestockOnAssign = true;
			skillDef.interruptPriority = 2;
			skillDef.isBullets = false;
			skillDef.isCombatSkill = true;
			skillDef.mustKeyPress = false;
			skillDef.noSprint = true;
			skillDef.rechargeStock = 1;
			skillDef.requiredStock = 1;
			skillDef.shootDelay = 0f;
			skillDef.stockToConsume = 1;
			skillDef.icon = Assets.icon4;
			skillDef.skillDescriptionToken = "TWITCH_SPECIAL_EXPUNGE_DESCRIPTION";
			skillDef.skillName = "TWITCH_SPECIAL_EXPUNGE_NAME";
			skillDef.skillNameToken = "TWITCH_SPECIAL_EXPUNGE_NAME";
			skillDef.keywordTokens = new string[]
			{
				"KEYWORD_INFECTION"
			};
			LoadoutAPI.AddSkillDef(skillDef);
			component.special = Twitch.characterPrefab.AddComponent<GenericSkill>();
			SkillFamily skillFamily = ScriptableObject.CreateInstance<SkillFamily>();
			skillFamily.variants = new SkillFamily.Variant[1];
			LoadoutAPI.AddSkillFamily(skillFamily);
			Reflection.SetFieldValue<SkillFamily>(component.special, "_skillFamily", skillFamily);
			SkillFamily skillFamily2 = component.special.skillFamily;
			SkillFamily.Variant[] variants = skillFamily2.variants;
			int num = 0;
			SkillFamily.Variant variant = default(SkillFamily.Variant);
			variant.skillDef = skillDef;
			variant.unlockableName = "";
			variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
			variants[num] = variant;
		}

		// Token: 0x0600006C RID: 108 RVA: 0x0000E3D0 File Offset: 0x0000C5D0
		private void CreateMaster()
		{
			this.doppelganger = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Charactermasters/CommandoMonsterMaster"), "TwitchMonsterMaster", true, "C:\\Users\\rseid\\Documents\\ror2mods\\Twitch\\Twitch\\Twitch.cs", "CreateMaster", 4097);
			MasterCatalog.getAdditionalEntries += delegate(List<GameObject> list)
			{
				list.Add(this.doppelganger);
			};
			CharacterMaster component = this.doppelganger.GetComponent<CharacterMaster>();
			component.bodyPrefab = Twitch.characterPrefab;
		}

		// Token: 0x04000075 RID: 117
		public const string MODUID = "com.rob.Twitch";

		// Token: 0x04000076 RID: 118
		public static GameObject characterPrefab;

		// Token: 0x04000077 RID: 119
		public GameObject characterDisplay;

		// Token: 0x04000078 RID: 120
		public GameObject doppelganger;

		// Token: 0x04000079 RID: 121
		public static SkillDef ambushDef;

		// Token: 0x0400007A RID: 122
		public static SkillDef ambushActiveDef;

		// Token: 0x0400007B RID: 123
		public static SkillDef ambushRecastDef;

		// Token: 0x0400007C RID: 124
		public static GameObject boltProjectile;

		// Token: 0x0400007D RID: 125
		public static GameObject bulletProjectile;

		// Token: 0x0400007E RID: 126
		public static GameObject expungeProjectile;

		// Token: 0x0400007F RID: 127
		public static GameObject caskProjectile;

		// Token: 0x04000080 RID: 128
		public static GameObject venomPool;

		// Token: 0x04000081 RID: 129
		public static GameObject bazookaProjectile;

		// Token: 0x04000082 RID: 130
		public static GameObject grenadeProjectile;

		// Token: 0x04000083 RID: 131
		public static GameObject laserTracer;

		// Token: 0x04000084 RID: 132
		public static BuffIndex venomDebuff;

		// Token: 0x04000085 RID: 133
		public static BuffIndex ambushBuff;

		// Token: 0x04000086 RID: 134
		public static BuffIndex expungeDebuff;

		// Token: 0x04000087 RID: 135
		private static readonly Color characterColor = new Color(0.16f, 0.34f, 0.04f);

		// Token: 0x04000088 RID: 136
		private static readonly Color poisonColor = new Color(0.36f, 0.54f, 0.24f);

		// Token: 0x04000089 RID: 137
		private static Dictionary<string, GameObject> itemDisplayPrefabs = new Dictionary<string, GameObject>();

		// Token: 0x0400008A RID: 138
		public static ConfigEntry<bool> how;

		// Token: 0x0400008B RID: 139
		private static ConfigEntry<bool> boom;
	}
}
