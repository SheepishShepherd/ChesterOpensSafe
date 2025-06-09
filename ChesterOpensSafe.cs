using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace ChesterOpensSafe
{
	public class ChesterOpensSafe : Mod
	{
		internal static Configuration config;
		private static int RequestChesterBank => (int)config.ChesterBankValue;
		// TODO: Learn IL Editing to make this simpler?

		public override void Load() {
			// No need to interject when the Piggy Bank is the vanilla container
			if (config.ChesterBankValue != Configuration.Bank.PiggyBank) {
				On_Player.HandleBeingInChestRange += Player_HandleBeingInChestRange; // Logic for when chester is actively open
				On_Main.TryInteractingWithMoneyTrough += Main_TryInteractingWithMoneyTrough; // Interacting with chester to open logic
				IL_Projectile.TryGetContainerIndex += Projectile_TryGetContainerIndex; // Quick-stack logic
				On_Player.QuickStackAllChests += Player_QuickStackAllChests; // Needed to make above IL edit work??
				//On_Player.GetNearbyContainerProjectilesList += Player_GetNearbyContainerProjectilesList;
				//On_ChestUI.QuickStack += ChestUI_QuickStack;
			}
		}

		private static void Player_HandleBeingInChestRange(On_Player.orig_HandleBeingInChestRange orig, Player player) {
			if (player.piggyBankProjTracker.ProjectileType != ProjectileID.ChesterPet) {
				orig(player); // if not a chester pet projectile, just default to the original method
			}
			else {
				if (player.chest != RequestChesterBank) {
					player.piggyBankProjTracker.Clear(); // clear the tracked projectile if no longer accessing safe contents
					//Main.NewText($"Projectile tracker cleared. (Player_HandleBeingInChestRange)");
				}
				//Main.NewText($"in chester: {player.piggyBankProjTracker.ProjectileLocalIndex}");
				if (player.piggyBankProjTracker.ProjectileLocalIndex >= 0) {
					Projectile chesterProj = Main.projectile[player.piggyBankProjTracker.ProjectileLocalIndex];
					if (!chesterProj.active) {
						//Main.NewText($"Chester no longer active. (Player_HandleBeingInChestRange)");
						Main.PlayInteractiveProjectileOpenCloseSound(chesterProj.type, open: false);
						player.chest = -1;
						Recipe.FindRecipes();
					}
					else {
						int X = (int)(((double)player.position.X + (double)player.width * 0.5) / 16.0);
						int Y = (int)(((double)player.position.Y + (double)player.height * 0.5) / 16.0);
						Vector2 vector = chesterProj.Hitbox.ClosestPointInRect(player.Center);
						player.chestX = (int)vector.X / 16;
						player.chestY = (int)vector.Y / 16;
						if (X < player.chestX - Player.tileRangeX || X > player.chestX + Player.tileRangeX + 1 || Y < player.chestY - Player.tileRangeY || Y > player.chestY + Player.tileRangeY + 1) {
							if (player.chest != -1)
								Main.PlayInteractiveProjectileOpenCloseSound(chesterProj.type, open: false);

							//Main.NewText($"Chester out of range? (Player_HandleBeingInChestRange)");
							player.chest = -1;
							Recipe.FindRecipes();
						}
					}
				}
			}
		}

		private static int Main_TryInteractingWithMoneyTrough(On_Main.orig_TryInteractingWithMoneyTrough orig, Projectile proj) {
			if (Main.gamePaused || Main.gameMenu)
				return 0;

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
			if (!((flag2 || Main.SmartInteractProj == proj.whoAmI) & !localPlayer.lastMouseInterface))
				return usingMouse ? 0 : 1;

			Main.HasInteractibleObjectThatIsNotATile = true;
			if (flag2) {
				localPlayer.noThrow = 2;
				localPlayer.cursorItemIconEnabled = true;
				localPlayer.cursorItemIconID = ItemID.MoneyTrough;
				if (proj.type == ProjectileID.ChesterPet)
					localPlayer.cursorItemIconID = ItemID.ChesterPetItem;
			}

			if (PlayerInput.UsingGamepad)
				localPlayer.GamepadEnableGrappleCooldown();

			if (Main.mouseRight && Main.mouseRightRelease && Player.BlockInteractionWithProjectiles == 0) {
				Main.mouseRightRelease = false;
				localPlayer.tileInteractAttempted = true;
				localPlayer.tileInteractionHappened = true;
				localPlayer.releaseUseTile = false;
				if (proj.type == ProjectileID.ChesterPet) {
					if (localPlayer.chest == RequestChesterBank) {
						//Main.NewText($"Chester interacted: closed. (Main_TryInteractingWithMoneyTrough)");
						localPlayer.chest = -1;
						Main.PlayInteractiveProjectileOpenCloseSound(proj.type, open: false);
						Recipe.FindRecipes();
					}
					else {
						//Main.NewText($"Chester interacted: open. (Main_TryInteractingWithMoneyTrough)");
						localPlayer.chest = RequestChesterBank; // set the chester inventory to safe contents
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
				else {
					if (localPlayer.chest == -2) {
						localPlayer.chest = -1;
						Main.PlayInteractiveProjectileOpenCloseSound(proj.type, open: false);
						Recipe.FindRecipes();
					}
					else {
						localPlayer.chest = -2;
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
			}

			if (!Main.SmartCursorIsUsed && !PlayerInput.UsingGamepad)
				return 0;

			return usingMouse ? 0 : 2;
		}

		private static void Player_QuickStackAllChests(On_Player.orig_QuickStackAllChests orig, Player player) {
			orig(player); // For some reason, this is needed to make the IL edit below to work at all...
		}

		private static void Projectile_TryGetContainerIndex(ILContext il) {
			try {
				ILCursor c = new ILCursor(il);
				c.GotoNext(i => i.MatchStindI4()); // send cursor to the 'containerIndex = -1' default declaration
				c.Index++; // move to next line, which is just before the Piggybank & Chester check
				var label = il.DefineLabel(); // use the MoneyTrough/ChesterPet label position as a reference for later

				c.Emit(OpCodes.Ldarg_0); // Projectile parameter
				c.Emit(OpCodes.Ldfld, typeof(Projectile).GetField(nameof(Projectile.type))); // get the type field
				c.Emit(OpCodes.Ldc_I4, ProjectileID.ChesterPet); // check if it is the Chester projectile
				c.Emit(OpCodes.Bne_Un_S, label); // if check above is false, move back to label reference

				c.Emit(OpCodes.Ldarg_1); // containerIndex parameter
				c.Emit(OpCodes.Ldc_I4_S, (sbyte)RequestChesterBank); // variable desired
				c.Emit(OpCodes.Stind_I4); // apply the selected value to containerIndex
				c.Emit(OpCodes.Ldc_I4_1); // variable true
				c.Emit(OpCodes.Ret); // apply true to return

				c.MarkLabel(label); // after all this, the label is applied
			}
			catch (Exception e) {
				MonoModHooks.DumpIL(ModContent.GetInstance<ChesterOpensSafe>(), il); // Logs/ILDumps/ChesterOpensSafe/Projectile_TryGetContainerIndex.txt
			}
		}

		/// KEEP BELOW CODE FOR DEBUGGING

		/*
		private static bool Projectile_TryGetContainerIndex(On_Projectile.orig_TryGetContainerIndex orig, Projectile proj, out int containerIndex) {
			if (proj.type == ProjectileID.ChesterPet) {
				containerIndex = -3;
				Main.NewText($"Chester found. Result: {containerIndex} (Projectile_TryGetContainerIndex)");
				return true;
			}
			if (proj.type == ProjectileID.FlyingPiggyBank) {
				containerIndex = -2;
				Main.NewText($"Flying Piggy Bank found. Result: {containerIndex} (Projectile_TryGetContainerIndex)");
				return true;
			}
			if (proj.type == ProjectileID.VoidLens) {
				containerIndex = -5;
				Main.NewText($"Void Bag found. Result: {containerIndex} (Projectile_TryGetContainerIndex)");
				return true;
			}
			Main.NewText(proj.Name + " found. Result -1 (Projectile_TryGetContainerIndex)");
			containerIndex = -1;
			return false;
		}


		private static List<int> Player_GetNearbyContainerProjectilesList(On_Player.orig_GetNearbyContainerProjectilesList orig, Player player) {
			Main.NewText("Generating list of interactible projectiles (Player_GetNearbyContainerProjectilesList)");
			List<int> list = orig(player);
			foreach (int item in list) {
				Projectile proj = Main.projectile[item];
				Main.NewText($"[{proj.type}] {proj.Name} found in Main.projetile[{item}]. (Player_GetNearbyContainerProjectilesList)");
			}
			return list;
		}
		private static void ChestUI_QuickStack(On_ChestUI.orig_QuickStack orig, ContainerTransferContext context, bool voidStack) {
			Main.NewText($"Before: {Main.LocalPlayer.chest} (ChestUI_QuickStack)");
			orig(context, voidStack);
			Main.NewText($"After: {Main.LocalPlayer.chest} (ChestUI_QuickStack)");
		}
		*/
	}
}
