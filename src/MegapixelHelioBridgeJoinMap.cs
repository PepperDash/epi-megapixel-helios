using PepperDash.Essentials.Core;

namespace MegapixelHelios
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	public class MegapixelHeliosBridgeJoinMap : JoinMapBaseAdvanced
	{
		#region Digital

		[JoinName("PowerOn")]
		public JoinDataComplete PowerOn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Power On (blackout) set & get",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PowerOff")]
		public JoinDataComplete PowerOff = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Power Off (blackout) set & get",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});


        [JoinName("SetRedundancyToMain")]
        public JoinDataComplete SetRedundancyToMain = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set redundancy to main. FB true if currently on main.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SetRedundancyToBackup")]
        public JoinDataComplete SetRedundancyToBackup = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set redundancy to backup. FB true if currently on backup.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("TestPatternOn")]
        public JoinDataComplete TestPatternOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 31,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Test Pattern On set & get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("TestPatternOff")]
        public JoinDataComplete TestPatternOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 32,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Test Pattern Off set & get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });



        [JoinName("BrightnessHigh")]
        public JoinDataComplete BrightnessHigh = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 33,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Brightness High",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("BrightnessMedium")]
        public JoinDataComplete BrightnessMedium = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 34,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Brightness Medium",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("BrightnessLow")]
        public JoinDataComplete BrightnessLow = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 35,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Brightness Low",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });


		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 50,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Is Online",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		#endregion


		#region Analog

		[JoinName("ResponseCode")]
		public JoinDataComplete ResponseCode = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Response code feedback",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		[JoinName("RecallPresetById")]
		public JoinDataComplete RecallPresetById = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 21,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Preset set & get by ID",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Analog
			});

        [JoinName("Brightness")]
        public JoinDataComplete Brightness = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 33,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Brightness",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

		#endregion


		#region Serial

		[JoinName("Name")]
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Device Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});


		[JoinName("ResponseContent")]
		public JoinDataComplete ResponseContent = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Response content feedback",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("RecallPresetByName")]
		public JoinDataComplete RecallPresetByName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 21,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Preset set & get by name",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public MegapixelHeliosBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(MegapixelHeliosBridgeJoinMap))
		{
		}
	}
}