using System;
using System.Data.SqlClient;
using System.IO;
using System.Reactive.Linq;
using System.Text;

namespace Elect.Loader.Tests
{
	public static class DatabaseHelper
	{
		public static void ExecuteScripts(SqlConnection con, string[] fileNames)
		{
			foreach (string file in fileNames)
			{
				string[] lines = File.ReadAllLines(file);
				StringBuilder cmdBuilder = null;
				lines.ToObservable().Subscribe(
					line =>
						{
							if (line.StartsWith("GO"))
							{
								if (cmdBuilder != null)
								{
									var cmd = con.CreateCommand();
									cmd.CommandText = cmdBuilder.ToString();
									cmd.ExecuteNonQuery();
								}
								cmdBuilder = null;
							}
							else
							{
								if (cmdBuilder == null)
									cmdBuilder = new StringBuilder();
								cmdBuilder.AppendLine(line);
							}
						},
					ex => { Console.WriteLine("ERROR on executing: " + cmdBuilder); },
					() =>
						{
							if (cmdBuilder != null)
							{
								var cmd = con.CreateCommand();
								cmd.CommandText = cmdBuilder.ToString();
								cmd.ExecuteNonQuery();
							}
						}
					);

				Console.WriteLine("Script executed: " + file);
			}
		}
	}
}