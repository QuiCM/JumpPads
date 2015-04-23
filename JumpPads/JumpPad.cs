using System.Linq;
using Terraria;
using TShockAPI;

namespace JumpPads
{
	public class JumpPad
	{
		public float posx, posy, jump, launch;
		public int width, height;
		public string permission;
		public int Id;

		public JumpPad(float posx, float posy, float jump, float launch, int width, int height, string permission)
		{
			this.posx = posx;
			this.posy = posy;
			this.jump = jump;
			this.launch = launch;
			this.width = width;
			this.height = height;
			this.permission = permission;
		}

		public bool InArea(TSPlayer player)
		{
			//Player is in jump pad area
			if (player.TileX < posx + width && player.TileX >= posx &&
				player.TileY > posy - height && player.TileY <= posy)
			{
				return true;
			}
			return false;
		}

		public bool CanJump(TSPlayer player)
		{
			if (player == null || (!player.Group.HasPermission(permission) && !string.IsNullOrEmpty(permission)))
			{
				return false;
			}

			return InArea(player);
		}

		public void Jump(TSPlayer player)
		{
			if (jump > 0 || jump < 0)
			{
				player.TPlayer.velocity.Y = jump;
			}
		}

		public void Launch(TSPlayer player)
		{
			if (launch > 0 || launch < 0)
			{
				player.TPlayer.velocity.X = launch;
			}
		}

		/// <summary>
		/// Rewrites the wire made by the jumppad to mark its position
		/// </summary>
		/// <param name="newWidth"></param>
		/// <param name="newHeight"></param>
		public void ReWriteWire(int newWidth, int newHeight)
		{
			UnwriteWire(false);
			if (newWidth != -1)
			{
				width = newWidth;
			}
			if (newHeight != -1)
			{
				height = newHeight;
			}
			WriteWire();
		}

		/// <summary>
		/// Writes wire to mark the jumppads position
		/// </summary>
		public void WriteWire()
		{
			for (var i = posx; i <= posx + width; i++)
			{
				for (var j = posy + 3; j >= posy + 2; j--)
				{
					var tile = Main.tile[(int)i, (int)j];
					tile.wire(true);
				}
			}

			WriteJ();
			WriteBounds();

			ResetSection();
		}

		/// <summary>
		/// Removes wire made by the jumppad to mark its position
		/// </summary>
		public void UnwriteWire(bool reset = true)
		{
			for (var i = posx; i <= posx + width; i++)
			{
				for (var j = posy + 3; j >= posy + 2; j--)
				{
					var tile = Main.tile[(int)i, (int)j];
					tile.wire(false);
				}
			}

			UnWriteJ();
			UnWriteBounds();

			if (reset)
			{
				ResetSection();
			}
		}

		private void WriteBounds()
		{
			if (height < 5)
			{
				return;
			}

			var leftBound = (int)(posx);
			var rightBound = (int)(posx + width);

			for (var j = (int)posy - 3; j >= posy - height; j--)
			{
				if (j%2 == 0)
				{
					Main.tile[leftBound, j].wire3(true);
					Main.tile[rightBound, j].wire3(true);
				}
			}
		}

		private void UnWriteBounds()
		{
			var leftBound = (int)(posx);
			var rightBound = (int)(posx + width);

			for (var j = (int)posy - 3; j >= posy - height; j--)
			{
				if (j % 2 == 0)
				{
					Main.tile[leftBound, j].wire3(false);
					Main.tile[rightBound, j].wire3(false);
				}
			}
		}

		private void WriteJ()
		{
			var center = (int)(posx + (width / 2f));

			for (var i = center - 2; i <= center + 2; i++)
			{
				for (var j = (int)posy + 1; j >= (int)posy - 2; j--)
				{
					if (i <= center && j == (int)posy + 1)
					{
						Main.tile[i, j].wire2(true);
						continue;
					}

					if (i <= center && j == (int) posy)
					{
						if (i == center - 2)
						{
							Main.tile[i, j].wire2(true);
							continue;
						}
						if (i == center)
						{
							Main.tile[i, j].wire2(true);
							continue;
						}
					}

					if (i == center && j == (int) posy - 1)
					{
						Main.tile[i, j].wire2(true);
						continue;
					}

					if (j == (int) posy - 2)
					{
						Main.tile[i, j].wire2(true);
					}
				}
			}
		}

		private void UnWriteJ()
		{
			var center = (int)(posx + (width / 2f));

			for (var i = center - 2; i <= center + 2; i++)
			{
				for (var j = (int)posy + 1; j >= (int)posy - 2; j--)
				{
					if (i <= center && j == (int)posy + 1)
					{
						Main.tile[i, j].wire2(false);
						continue;
					}

					if (i <= center && j == (int)posy)
					{
						if (i == center - 2)
						{
							Main.tile[i, j].wire2(false);
							continue;
						}
						if (i == center)
						{
							Main.tile[i, j].wire2(false);
							continue;
						}
					}

					if (i == center && j == (int)posy - 1)
					{
						Main.tile[i, j].wire2(false);
						continue;
					}

					if (j == (int)posy - 2)
					{
						Main.tile[i, j].wire2(false);
					}
				}
			}
		}

		private void ResetSection()
		{
			var startx = Netplay.GetSectionX((int)posx);
			var endx = Netplay.GetSectionX((int)(posx + width));
			var starty = Netplay.GetSectionY((int)posy);
			var endy = Netplay.GetSectionY((int)(posy - 1));

			foreach (var sock in Netplay.serverSock.Where(s => s.active))
			{
				for (var i = startx; i <= endx; i++)
				{
					for (var j = endy; j <= starty; j++)
					{
						sock.tileSection[i, j] = false;
					}
				}
			}
		}
	}
}