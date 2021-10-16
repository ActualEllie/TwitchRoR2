using System;
using UnityEngine;

namespace Twitch
{
	// Token: 0x02000010 RID: 16
	public class SeparateFromParent : MonoBehaviour
	{
		// Token: 0x06000074 RID: 116 RVA: 0x0000E69E File Offset: 0x0000C89E
		private void Awake()
		{
			base.transform.SetParent(null);
		}
	}
}
