using TShockAPI;

namespace JumpPads
{
	public class JumpPad
	{
		public float posx, posy, jump;
		public int width, height;
		public string permission;
		public int Id;

		public JumpPad(float posx, float posy, float jump, int width, int height, string permission)
		{
			this.posx = posx;
			this.posy = posy;
			this.jump = jump;
			this.width = width;
			this.height = height;
			this.permission = permission;
		}

		public bool InArea(TSPlayer player)
		{
			//Player is in jump pad area
			if (player.TileX <= posx + width && player.TileX >= posx &&
				player.TileY >= posy - height && player.TileY <= posy)
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
			player.TPlayer.velocity.Y = jump;
		}
	}
}