using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace ChesterOpensSafe
{
	public class ChesterOpensSafe : Mod
	{
		// TODO: Learn IL Editing to make this simpler?

		public override void Load() {
			On_Player.HandleBeingInChestRange += Player_HandleBeingInChestRange;
			On_Main.TryInteractingWithMoneyTrough += Main_TryInteractingWithMoneyTrough;
		}

		private static void Player_HandleBeingInChestRange(On_Player.orig_HandleBeingInChestRange orig, Player player) {
			if (player.piggyBankProjTracker.ProjectileType == ProjectileID.ChesterPet) {
				if (player.chest != -3) {
					player.piggyBankProjTracker.Clear();
				}
				else {
					player.chest = -3;
					Recipe.FindRecipes();
				}
			}
			else {
				orig(player);
			}
		}

		private static int Main_TryInteractingWithMoneyTrough(On_Main.orig_TryInteractingWithMoneyTrough orig, Projectile proj) {
			if (Main.gamePaused || Main.gameMenu || proj.type != ProjectileID.ChesterPet) {
				return orig(proj);
			}

			bool usingMouse = !Main.SmartCursorIsUsed && !PlayerInput.UsingGamepad;
			Player localPlayer = Main.LocalPlayer;
			Point point = proj.Center.ToTileCoordinates();
			Vector2 compareSpot = localPlayer.Center;
			if (!localPlayer.IsProjectileInteractibleAndInInteractionRange(proj, ref compareSpot)) {
				localPlayer.piggyBankProjTracker.Clear();
				return 0;
			}
			Matrix matrix = Matrix.Invert(Main.GameViewMatrix.ZoomMatrix);
			Vector2 position = Main.ReverseGravitySupport(Main.MouseScreen);
			Vector2.Transform(Main.screenPosition, matrix);
			Vector2 v = Vector2.Transform(position, matrix) + Main.screenPosition;
			bool flag2 = proj.Hitbox.Contains(v.ToPoint());
			if (!((flag2 || Main.SmartInteractProj == proj.whoAmI) & !localPlayer.lastMouseInterface)) {
				if (!usingMouse) {
					return 1;
				}
				return 0;
			}
			Main.HasInteractibleObjectThatIsNotATile = true;
			if (flag2) {
				localPlayer.noThrow = 2;
				localPlayer.cursorItemIconEnabled = true;
				localPlayer.cursorItemIconID = 3213;
				if (proj.type == 960) {
					localPlayer.cursorItemIconID = 5098;
				}
			}
			if (PlayerInput.UsingGamepad) {
				localPlayer.GamepadEnableGrappleCooldown();
			}
			if (Main.mouseRight && Main.mouseRightRelease && Player.BlockInteractionWithProjectiles == 0) {
				Main.mouseRightRelease = false;
				localPlayer.tileInteractAttempted = true;
				localPlayer.tileInteractionHappened = true;
				localPlayer.releaseUseTile = false;
				if (localPlayer.chest == -3) {
					localPlayer.chest = -1;
					Main.PlayInteractiveProjectileOpenCloseSound(proj.type, open: false);
					Recipe.FindRecipes();
				}
				else {
					localPlayer.chest = -3;
					for (int i = 0; i < 40; i++) {
						ItemSlot.SetGlow(i, -1f, chest: true);
					}
					localPlayer.piggyBankProjTracker.Set(proj);
					localPlayer.chestX = point.X;
					localPlayer.chestY = point.Y;
					localPlayer.SetTalkNPC(-1);
					Main.SetNPCShopIndex(0);
					Main.playerInventory = true;
					Main.PlayInteractiveProjectileOpenCloseSound(proj.type, open: true);
					Recipe.FindRecipes();
				}
			}
			if (!Main.SmartCursorIsUsed && !PlayerInput.UsingGamepad) {
				return 0;
			}
			if (!usingMouse) {
				return 2;
			}
			return 0;
		}
	}
}
