using System;
using RoR2.Projectile;
using UnityEngine;

namespace Twitch
{
	// Token: 0x02000012 RID: 18
	public class TwitchGrenadeMain : MonoBehaviour
	{
		// Token: 0x06000078 RID: 120 RVA: 0x0000E6D4 File Offset: 0x0000C8D4
		private void Awake()
		{
			this.projectileController = base.GetComponent<ProjectileController>();
			this.projectileImpact = base.GetComponent<ProjectileImpactExplosion>();
		}

		// Token: 0x06000079 RID: 121 RVA: 0x0000E6F0 File Offset: 0x0000C8F0
		private void Start()
		{
			bool flag = this.projectileController;
			if (flag)
			{
				bool flag2 = this.projectileController.ghost;
				if (flag2)
				{
					this.modelController = this.projectileController.ghost.gameObject.GetComponent<TwitchGrenadeController>();
				}
				bool flag3 = this.modelController;
				if (flag3)
				{
					bool flag4 = this.projectileImpact;
					if (flag4)
					{
						bool flag5 = this.projectileImpact.lifetime <= 0.25f;
						if (flag5)
						{
							this.EnableOverlay();
						}
						else
						{
							base.Invoke("EnableOverlay", this.projectileImpact.lifetime - 0.25f);
						}
					}
				}
			}
		}

		// Token: 0x0600007A RID: 122 RVA: 0x0000E7AC File Offset: 0x0000C9AC
		private void EnableOverlay()
		{
			bool flag = this.modelController;
			if (flag)
			{
				this.modelController.EnableOverlay();
			}
		}

		// Token: 0x0400008E RID: 142
		private TwitchGrenadeController modelController;

		// Token: 0x0400008F RID: 143
		private ProjectileController projectileController;

		// Token: 0x04000090 RID: 144
		private ProjectileImpactExplosion projectileImpact;
	}
}
