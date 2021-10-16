using System;
using RoR2;
using UnityEngine;

namespace Twitch
{
	// Token: 0x0200000F RID: 15
	public class ApplyHorns : MonoBehaviour
	{
		// Token: 0x06000070 RID: 112 RVA: 0x0000E488 File Offset: 0x0000C688
		private void Start()
		{
			this.model = base.GetComponentInChildren<CharacterModel>();
			this.childLocator = base.GetComponentInChildren<ChildLocator>();
			Debug.Log("adding horns");
			this.AddHorns();
		}

		// Token: 0x06000071 RID: 113 RVA: 0x0000E4B8 File Offset: 0x0000C6B8
		private void AddHorns()
		{
			bool flag = this.model;
			if (flag)
			{
				Debug.Log("1");
				DisplayRuleGroup equipmentDisplayRuleGroup = this.model.itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(4);
				bool flag2 = equipmentDisplayRuleGroup.rules.Length > 1;
				if (flag2)
				{
					Transform transform = this.childLocator.FindChild(equipmentDisplayRuleGroup.rules[0].childName);
					bool flag3 = transform;
					if (flag3)
					{
						this.Apply(this.model, equipmentDisplayRuleGroup.rules[0].followerPrefab, transform, equipmentDisplayRuleGroup.rules[0].localPos, Quaternion.Euler(equipmentDisplayRuleGroup.rules[0].localAngles), equipmentDisplayRuleGroup.rules[0].localScale);
					}
					Transform transform2 = this.childLocator.FindChild(equipmentDisplayRuleGroup.rules[1].childName);
					bool flag4 = transform2;
					if (flag4)
					{
						this.Apply(this.model, equipmentDisplayRuleGroup.rules[1].followerPrefab, transform, equipmentDisplayRuleGroup.rules[1].localPos, Quaternion.Euler(equipmentDisplayRuleGroup.rules[1].localAngles), equipmentDisplayRuleGroup.rules[1].localScale);
					}
				}
			}
			else
			{
				Debug.Log("no charactermodel");
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x0000E620 File Offset: 0x0000C820
		private void Apply(CharacterModel characterModel, GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			GameObject gameObject = Object.Instantiate<GameObject>(prefab.gameObject, parent);
			gameObject.transform.localPosition = localPosition;
			gameObject.transform.localRotation = localRotation;
			gameObject.transform.localScale = localScale;
			LimbMatcher component = gameObject.GetComponent<LimbMatcher>();
			bool flag = component && this.childLocator;
			if (flag)
			{
				component.SetChildLocator(this.childLocator);
			}
		}

		// Token: 0x0400008C RID: 140
		private CharacterModel model;

		// Token: 0x0400008D RID: 141
		private ChildLocator childLocator;
	}
}
