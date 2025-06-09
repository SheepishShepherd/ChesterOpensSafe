using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace ChesterOpensSafe
{
	public class Configuration : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;
		public override void OnLoaded() => ChesterOpensSafe.config = this;

		internal LocalizedText Piggy = Lang.GetItemName(ItemID.PiggyBank);
		internal string Safe = "Safe";
		internal string Forge = "Defender's Forge";

		public enum Bank {
			PiggyBank = -2,
			Safe = -3,
			Forge = -4,
			VoidVault = -5
		}

		[ReloadRequired]
		[DrawTicks]
		[DefaultValue(Bank.PiggyBank)]
		public Bank ChesterBankValue { get; set; }
	}
}
