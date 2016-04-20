﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CygnusGroundStation
{
	class SetupDialogSettings	: SettingsBase
	{
		// Dialog position settings
		public WindowPosSettings DialogPos;

		public SetupDialogSettings() : base("Main","SetupDialog")
		{
			DialogPos = new WindowPosSettings();
			SetDefaultValues();
		}

		override public void SetDefaultValues()
		{
			DialogPos.SetDefault(400, 300);
		}
	}
}
