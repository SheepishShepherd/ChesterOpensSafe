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
		// A bool to flag if Chester is open.
		// //This will not be reset each update as we want it to carry over into the next tick.
		bool handlingChester = false;

		public override void PreUpdate() {
			// Temporarily set the player's chest to Piggy Bank before the vanilla Chest Handling code is run.
			// Chest Handling for Chester's inventory will close if it is not set to -2.
			if (handlingChester) {
				Player.chest = -2;
			}
		}

		public override void PostUpdate() {
			// After vanilla Chest Handling code is run, determine if it actually was Chester.
			// If so, run the modded ChesterHandling code.
			handlingChester = Player.piggyBankProjTracker.ProjectileType == ProjectileID.ChesterPet;
			if (handlingChester) {
				HandleChesterSafe(Main.projectile[Player.piggyBankProjTracker.ProjectileLocalIndex]);
			}
		}

		private void UnhandleChester(string debugMessage) {
			// To prevent anything breaking, clear the piggyBankTracker and reset the bool.
			Player.piggyBankProjTracker.Clear();
			handlingChester = false;
			Recipe.FindRecipes();
			//Main.NewText(debugMessage); // NewText for debugging
		}

		private void HandleChesterSafe(Projectile chester) {
			// Run through normal vanilla chest handling for Chester.
			if (!chester.active || chester.type != ProjectileID.ChesterPet) {
				UnhandleChester("Projectile is inactive or not the correct type");
				return;
			}
			else {
				int x = (int)(((double)Player.position.X + (double)Player.width * 0.5) / 16.0);
				int y = (int)(((double)Player.position.Y + (double)Player.height * 0.5) / 16.0);
				bool xOutOfRange = x < Player.chestX - Player.tileRangeX || x > Player.chestX + Player.tileRangeX + 1;
				bool yOutOfRange = y < Player.chestY - Player.tileRangeY || y > Player.chestY + Player.tileRangeY + 1;
				if (xOutOfRange || yOutOfRange) {
					UnhandleChester("Projectile is too far from the player");
					return;
				}
			}
			// Will only get to this point if projectile is active, is the correct type, and is within range.
			// Set the Player's chest inventory to the Safe's inventory.
			Player.chest = -3;
			Recipe.FindRecipes();
		}
	}
}
