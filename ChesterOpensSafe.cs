using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ChesterOpensSafe
{
	public class ChesterOpensSafe : Mod
	{
		// TODO: [Issue] Chest remains at -2 for a single frame
	}

	public class ChesterPlayer : ModPlayer
	{
		// A bool to flag if Chester is open.
		// This will not be reset each update as we want it to carry over into the next tick.
		bool handlingChester = false;

		// Temporarily set the player's chest to Piggy Bank before the vanilla Chest Handling code is run.
		// Chest Handling for Chester's inventory will close if it is not set to -2.
		public override void PreUpdate() {
			if (handlingChester) {
				Player.chest = -2;

				// Prevents Chester being considered 'open' when accessing a Piggy Bank or Safe
				if (Main.mouseRight && Main.mouseRightRelease && Player.BlockInteractionWithProjectiles == 0) {
					if (Player.IsTileTypeInInteractionRange(TileID.Safes) && Main.tile[Player.tileTargetX, Player.tileTargetY].TileType == TileID.Safes) {
						UnhandleChester("A Safe was opened via right-click");
						return;
					}
					// Smart Interact can be a non-tile, so check if its within the bounds
					if (Main.SmartInteractX >= 0 && Main.SmartInteractX <= Main.maxTilesX && Main.SmartInteractY >= 0 && Main.SmartInteractY <= Main.maxTilesY) {
						if (Main.tile[Main.SmartInteractX, Main.SmartInteractY].TileType == TileID.Safes) {
							UnhandleChester("A Safe was opened via smart-cursor");
							return;
						}
					}

					if (Player.IsTileTypeInInteractionRange(TileID.PiggyBank) && Main.tile[Player.tileTargetX, Player.tileTargetY].TileType == TileID.PiggyBank) {
						UnhandleChester("A Piggy Bank was opened via right-click");
						return;
					}
					if (Main.SmartInteractX >= 0 && Main.SmartInteractX <= Main.maxTilesX && Main.SmartInteractY >= 0 && Main.SmartInteractY <= Main.maxTilesY) {
						if (Main.tile[Main.SmartInteractX, Main.SmartInteractY].TileType == TileID.PiggyBank) {
							UnhandleChester("A Piggy Bank was opened via smart-cursor");
							return;
						}
					}
				}
			}
		}

		// Until the first frame issue is resolved, this will allow users with autopause to access the safe when using Chester
		public override void UpdateAutopause() {
			if (Player.piggyBankProjTracker.ProjectileType == ProjectileID.ChesterPet) {
				Player.chest = -3;
				Recipe.FindRecipes();
			}
		}

		// After vanilla Chest Handling code is run, determine if it Chester was being used.
		// If so, run the modded Chester handling code.
		public override void PostUpdate() {
			// Prevents Chester from closing when accessing it while already being in a piggy bank chest
			if (!handlingChester && Player.chest == -2 && Main.mouseRight && Main.mouseRightRelease && Player.BlockInteractionWithProjectiles == 0) {
				int index = Player.GetListOfProjectilesToInteractWithHack().FindIndex(x => Main.projectile[x].type == ProjectileID.ChesterPet);
				if (index != -1 && Main.projectile[index].Hitbox.Contains(Main.MouseWorld.ToPoint())) {
					Player.chest = -3;
					Recipe.FindRecipes();
					return;
				}
				else if (Main.SmartInteractProj != -1 && Main.projectile[Main.SmartInteractProj].type == ProjectileID.ChesterPet) {
					Player.chest = -3;
					Recipe.FindRecipes();
					return;
				}
			}

			// Run the Chester handling code if Chester is being used
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
			Main.NewText(debugMessage); // NewText for debugging
		}

		private void HandleChesterSafe(Projectile chester) {
			// Run through each case where Chester should be closed...

			if (!chester.active || chester.type != ProjectileID.ChesterPet) {
				UnhandleChester("Projectile is inactive or not the correct type");
				return;
			}

			if (!Main.playerInventory) {
				UnhandleChester("Inventory was closed");
				return;
			}

			Vector2 compareSpot = Player.Center;
			if (!Player.IsProjectileInteractibleAndInInteractionRange(chester, ref compareSpot)) {
				UnhandleChester("Projectile is too far from the player or uninteractible");
				return;
			}

			if (Main.mouseRight && Main.mouseRightRelease && Player.BlockInteractionWithProjectiles == 0) {
				if (chester.Hitbox.Contains(Main.MouseWorld.ToPoint())) {
					UnhandleChester("Closed Chester via right-click");
					return;
				}
				if (Main.SmartInteractProj == Player.piggyBankProjTracker.ProjectileLocalIndex) {
					UnhandleChester("Closed Chester via smart-cursor");
					return;
				}
			}

			// Set the Player's chest to the safe inventory.
			Player.chest = -3;
			Recipe.FindRecipes();
		}
	}
}
