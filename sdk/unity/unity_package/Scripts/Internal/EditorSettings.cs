/*
 * Copyright (C) 2020 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using TiltFive.Logging;

namespace TiltFive
{
	[System.Serializable]
	public class RemotePlayerSettings
	{
		[System.Serializable]
		public class Host {
			public string ip = "127.0.0.1";
			public const int port = 7202;
		};

		[System.Serializable]
		public class Glasses {
			[Range(1,10)]
			public int refreshRate = 3;
		};

		[System.Serializable]
		public class Wand {
			public bool enabled = true;
			[Range(1,10)]
			public int refreshRate = 3;
		};

		[System.Serializable]
		public class ScreenStreaming {
			public bool enabled = true;
			[Range(1,10)]
			public int refreshRate = 3;
			[Range(1, 100)]
			public int imageQuality = 75;
		};

		public bool enabled = false;
		public RemotePlayerSettings.Host host = new RemotePlayerSettings.Host ();
		public RemotePlayerSettings.Glasses glasses = new RemotePlayerSettings.Glasses ();
		public RemotePlayerSettings.Wand wand = new RemotePlayerSettings.Wand ();
		public RemotePlayerSettings.ScreenStreaming screenStreaming = new RemotePlayerSettings.ScreenStreaming();
	}

	[System.Serializable]
	public class EditorSettings
	{		
		public RemotePlayerSettings remotePlayerSettings = new RemotePlayerSettings ();

        //holds the editor panel display config.
        public int activePanel = 0;
	}
}

#endif