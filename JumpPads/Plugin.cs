using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace JumpPads
{
	[ApiVersion(1, 25)]
	public class Plugin : TerrariaPlugin
	{
		public override string Author
		{
			get { return "White"; }
		}

		public override string Name
		{
			get { return "JumpPads"; }
		}

		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}

		private static Database db;
		private static Timer updateTimer;
		private static List<JumpPad> _jumpPads = new List<JumpPad>();
		private static bool[] _disable = new bool[Main.player.Length];

		public Plugin(Main game) : base(game)
		{
		}

		public override void Initialize()
		{
			db = Database.InitDb("JumpPads");

			db.LoadJumpPads(ref _jumpPads);

			Commands.ChatCommands.Add(new Command("jumppads.add", AddJumpPad, "jumppad", "jp")
			{
				AllowServer = false,
				HelpDesc = new[]
				{
					"Usage: jumppad [width|height|jump|launch|permission] [value].",
					"Disable jumppads for yourself by using jumppad disable.",
					"Re-enable them by using jumppad enable"
				}
			});

			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
			ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
			}
			base.Dispose(disposing);
		}

		private void OnPostInitialize(EventArgs args)
		{
			updateTimer = new Timer(1000);
			updateTimer.Elapsed += UpdateTimerOnElapsed;
			updateTimer.Start();
		}

		private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs args)
		{
			if (_jumpPads.Count == 0)
			{
				return;
			}

			foreach (var player in TShock.Players.Where(player => player != null))
			{
				var jumpPad = _jumpPads.FirstOrDefault(j => j.CanJump(player));
				if (jumpPad != null)
				{
					if (player.Index >= 0)
					{
						if (_disable[player.Index])
						{
							continue;
						}
					}

					jumpPad.Jump(player);
					jumpPad.Launch(player);
					TSPlayer.All.SendData(PacketTypes.PlayerUpdate, "", player.Index);
				}
			}
		}

		private void OnServerLeave(LeaveEventArgs args)
		{
			if (args.Who >= 0)
			{
				_disable[args.Who] = false;
			}
		}

		private void AddJumpPad(CommandArgs args)
		{
			if (args.Parameters.Count == 1)
			{
				if (args.Parameters[0].ToLowerInvariant() == "reload" && args.Player.Group.HasPermission("jumppads.reload"))
				{
					_jumpPads.Clear();
					db.LoadJumpPads(ref _jumpPads);

					args.Player.SendSuccessMessage("JumpPads have been reloaded.");
					return;
				}

				if (args.Parameters[0].ToLowerInvariant() == "disable")
				{
					_disable[args.Player.Index] = true;
					args.Player.SendSuccessMessage("JumpPads will no longer effect you.");
					return;
				}

				if (args.Parameters[0].ToLowerInvariant() == "enable")
				{
					_disable[args.Player.Index] = false;
					args.Player.SendSuccessMessage("JumpPads will now effect you.");
					return;
				}

				if ((args.Parameters[0].ToLowerInvariant() == "d" || args.Parameters[0].ToLowerInvariant() == "delete")
					&& args.Player.Group.HasPermission("jumppads.delete"))
				{
					if (_jumpPads.Count == 0)
					{
						args.Player.SendErrorMessage("No jumppads have been defined.");
						return;
					}

					var index = -1;
					for (var i = 0; i < _jumpPads.Count; i++)
					{
						if (_jumpPads[i].InArea(args.Player))
						{
							index = i;
							break;
						}
					}

					if (index == -1)
					{
						args.Player.SendErrorMessage("Failed to find a jumppad underneath you.");
						return;
					}

					var jp = _jumpPads[index];
					_jumpPads.RemoveAt(index);
					db.DeleteJumpPad(jp.Id);
					jp.UnwriteWire();

					args.Player.SendSuccessMessage("Deleted the jumppad underneath you.");
					return;
				}
			}

			if (args.Parameters.Count < 2)
			{
				args.Player.SendErrorMessage("Invalid syntax. {0}jumppad [width|height|jump|launch|permission] [value]",
					TShock.Config.CommandSpecifier);
				return;
			}

			var newJumpPad = false;
			var makeWire = true;
			var jumpPad = _jumpPads.FirstOrDefault(j => j.CanJump(args.Player));

			if (jumpPad == null)
			{
				jumpPad = new JumpPad(args.Player.TileX, args.Player.TileY, 0f, 0f, 3, 1, string.Empty);
				newJumpPad = true;
			}

			if (args.Parameters.Last().ToLowerInvariant() == "-nowire")
			{
				makeWire = false;
			}

			switch (args.Parameters[0].ToLowerInvariant())
			{
				case "width":
				case "w":
				{
					int width;
					if (!int.TryParse(args.Parameters[1], out width))
					{
						args.Player.SendErrorMessage("Invalid width.");
						return;
					}

					if (makeWire)
					{
						jumpPad.ReWriteWire(width, -1);
					}

					args.Player.SendSuccessMessage("JumpPad on your position is now {0} blocks wide.", width);
					break;
				}

				case "height":
				case "h":
				{
					int height;
					if (!int.TryParse(args.Parameters[1], out height))
					{
						args.Player.SendErrorMessage("Invalid height.");
						return;
					}

					if (makeWire)
					{
						jumpPad.ReWriteWire(-1, height);
					}

					args.Player.SendSuccessMessage("JumpPad on your position is now {0} blocks high.", height);
					break;
				}

				case "jump":
				case "j":
				{
					float jump;
					if (!float.TryParse(args.Parameters[1], out jump))
					{
						args.Player.SendErrorMessage("Invalid jump height.");
						return;
					}

					jumpPad.jump = -jump;
					args.Player.SendSuccessMessage("JumpPad on your position now has a jump power of {0}.", jump);
					break;
				}

				case "launch":
				case "l":
				{
					float launch;
					if (!float.TryParse(args.Parameters[1], out launch))
					{
						args.Player.SendErrorMessage("Invalid launch power.");
						return;
					}

					jumpPad.launch = launch;
					args.Player.SendSuccessMessage("JumpPad on your position now has a launch power of {0}.", launch);
					break;
				}

				case "permission":
				case "perm":
				case "p":
				{
					var permission = args.Parameters[1].ToLowerInvariant();

					jumpPad.permission = permission;
					args.Player.SendSuccessMessage("JumpPad on your position now requires the permission '{0}'.", permission);
					break;
				}

				default:
				{
					args.Player.SendErrorMessage("Invalid syntax. {0}jumppad [width|height|jump|permission] [value]",
						TShock.Config.CommandSpecifier);
					break;
				}
			}

			if (newJumpPad)
			{
				jumpPad.Id = db.AddJumpPad(jumpPad);
				_jumpPads.Add(jumpPad);

				if (makeWire)
				{
					jumpPad.WriteWire();
				}
			}
			else
			{
				db.UpdateJumpPad(jumpPad);
			}
		}
	}
}