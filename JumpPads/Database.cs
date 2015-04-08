using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace JumpPads
{
	public class Database
	{
		private IDbConnection _db;

		public Database(IDbConnection db)
		{
			_db = db;
			var sqlCreator = new SqlTableCreator(_db,
				_db.GetSqlType() == SqlType.Sqlite
					? (IQueryBuilder) new SqliteQueryCreator()
					: new MysqlQueryCreator());
			var table = new SqlTable("JumpPads",
				new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
				new SqlColumn("PosX", MySqlDbType.Float),
				new SqlColumn("PosY", MySqlDbType.Float),
				new SqlColumn("Jump", MySqlDbType.Float),
				new SqlColumn("Width", MySqlDbType.Int32),
				new SqlColumn("Height", MySqlDbType.Int32),
				new SqlColumn("Permission", MySqlDbType.Text));
			sqlCreator.EnsureTableStructure(table);
		}

		public static Database InitDb(string name)
		{
			IDbConnection db;
			if (TShock.Config.StorageType.ToLower() == "sqlite")
				db =
					new SqliteConnection(string.Format("uri=file://{0},Version=3",
						Path.Combine(TShock.SavePath, name + ".sqlite")));
			else if (TShock.Config.StorageType.ToLower() == "mysql")
			{
				try
				{
					var host = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
							host[0],
							host.Length == 1 ? "3306" : host[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword
							)
					};
				}
				catch (MySqlException x)
				{
					TShock.Log.Error(x.ToString());
					throw new Exception("MySQL not setup correctly.");
				}
			}
			else
				throw new Exception("Invalid storage type.");
			var database = new Database(db);
			return database;
		}

		public QueryResult QueryReader(string query, params object[] args)
		{
			return _db.QueryReader(query, args);
		}

		public int Query(string query, params object[] args)
		{
			return _db.Query(query, args);
		}
		
		public int AddJumpPad(JumpPad copy)
		{
			Query("INSERT INTO JumpPads (PosX, PosY, Jump, Width, Height, Permission) VALUES (@0, @1, @2, @3, @4, @5)",
				copy.posx, copy.posy, copy.jump, copy.width, copy.height, copy.permission);

			using (var reader = QueryReader("SELECT max(ID) FROM JumpPads"))
			{
				if (reader.Read())
				{
					var id = reader.Get<int>("max(ID)") + 1;
					return id;
				}
			}

			return -1;
		}

		public void DeleteJumpPad(int id)
		{
			Query("DELETE FROM JumpPads WHERE ID = @0", id);
		}

		public void UpdateJumpPad(JumpPad update)
		{
			var query =
				string.Format(
					"UPDATE JumpPads SET PosX = {0}, PosY = {1}, Jump = {2}, Width = {3}, Height = {4}, Permission = @0 WHERE ID = @1",
					update.posx, update.posy, update.jump, update.width, update.height);
			
			Query(query, update.permission, update.Id);
		}

		public void LoadJumpPads(ref List<JumpPad> list)
		{
			using (var reader = QueryReader("SELECT * FROM JumpPads"))
			{
				while (reader.Read())
				{
					var id = reader.Get<int>("ID");
					var x = reader.Get<float>("PosX");
					var y = reader.Get<float>("PosY");
					var jump = reader.Get<float>("Jump");
					var width = reader.Get<int>("Width");
					var height = reader.Get<int>("Height");
					var permission = reader.Get<string>("Permission");

					var jumpPad = new JumpPad(x, y, jump, width, height, permission) {Id = id};
					list.Add(jumpPad);
				}
			}
		}
	}
}