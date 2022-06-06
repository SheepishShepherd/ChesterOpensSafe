using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ChesterOpensSafe
{
	public class ChesterOpensSafe : Mod
	{

	}

	public class ChesterPlayer : ModPlayer
	{
		bool handlingChester = false;
		int LocalChester = -1;

		public override void PostUpdateMiscEffects() {
			if (Player.piggyBankProjTracker.ProjectileType == ProjectileID.ChesterPet || handlingChester) {
				HandleChesterSafe();
			}
		}

		private void UnhandleChester(string debugMessage) {
			Player.piggyBankProjTracker.Clear();
			handlingChester = false;
			LocalChester = -1;

			// NewText for debugging
			//Main.NewText(debugMessage);
		}

		private void HandleChesterSafe() {
			if (Player.chest == -2) {
				LocalChester = Player.piggyBankProjTracker.ProjectileLocalIndex;

				handlingChester = true;
				Player.chest = -3;
				Recipe.FindRecipes();
			}
			else if (handlingChester) {
				if (LocalChester >= 0) {
					Projectile projectile = Main.projectile[LocalChester];
					if (!projectile.active || projectile.type != ProjectileID.ChesterPet) {
						UnhandleChester("Projectile is inactive or not the correct type");
						return;
					}
					else {
						Player.piggyBankProjTracker.Set(projectile);
						LocalChester = Player.piggyBankProjTracker.ProjectileLocalIndex;
						handlingChester = true;
						Player.chest = -3;

						int x = (int)(((double)Player.position.X + (double)Player.width * 0.5) / 16.0);
						int y = (int)(((double)Player.position.Y + (double)Player.height * 0.5) / 16.0);
						bool xOutOfRange = x < Player.chestX - Player.tileRangeX || x > Player.chestX + Player.tileRangeX + 1;
						bool yOutOfRange = y < Player.chestY - Player.tileRangeY || y > Player.chestY + Player.tileRangeY + 1;
						if (xOutOfRange || yOutOfRange) {
							UnhandleChester("Projectile is too far from the player");
							return;
						}
					}
					Player.piggyBankProjTracker.Set(projectile);
					Recipe.FindRecipes();
				}
				else {
					UnhandleChester("Projectile index is -1");
				}
			}
			else {
				UnhandleChester("Player bank is not -2 nor -3");
			}
		}
	}
}
