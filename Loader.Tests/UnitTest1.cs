using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elect.DomainObjects;
using Elect.Loader;
using Elect.Loader.Common;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Elect.Loader.Tests
{
	[TestClass]
	public class RuelectLoaderTests
	{
		private static string m_connectionStringDba = "Data Source=.;Integrated Security=True";
		private const string DbName = "elect_tests";
		private static string m_connectionString = "Data Source=.;Initial Catalog = elect_tests;Integrated Security=True";

		[AssemblyInitialize]
		public static void Initialize(TestContext testContext)
		{
			createDatabase();

			initializeDatabase();
		}

		[AssemblyCleanup]
		public static void AssemblyCleanup()
		{
			//dropDatabase();
		}

		private static void createDatabase()
		{
			dropDatabase();
			using (var con = new SqlConnection(m_connectionStringDba))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = "CREATE DATABASE [" + DbName + "]";
				cmd.ExecuteNonQuery();
			}
		}

		private static void dropDatabase()
		{
			using (var con = new SqlConnection(m_connectionStringDba))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = String.Format(@"
IF  EXISTS (SELECT name FROM sys.databases WHERE name = N'{0}')
BEGIN
	ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
	DROP DATABASE [{0}]
END", DbName);
				cmd.ExecuteNonQuery();
			}
		}

		private static void initializeDatabase()
		{
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var fileNames = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts"));
				DatabaseHelper.ExecuteScripts(con, fileNames);
			}
		}
		
		class MockDownloader: IDownloader
		{
			private readonly byte[] _returnBytes;

			public MockDownloader(byte[] returnBytes)
			{
				_returnBytes = returnBytes;
			}

			public Task<byte[]> Download(string uri, IDownloadNotifier notifier, CancellationToken cancellationToken)
			{
				var tcs = new TaskCompletionSource<byte[]>();
				tcs.SetResult(_returnBytes);
				return tcs.Task;
			}
		}

		[TestInitialize]
		public void TestInitialize()
		{
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				var commands = new[]
				               	{
				               		"delete ProtocolImage",
				               		"delete ProtocolResult",
				               		"delete Protocol",
				               		"delete Comission",
				               		"delete Candidate",
				               		"delete Poll",
				               		"delete Region",
				               		"delete ResultProvider"
				               	};
				foreach (var cmdText in commands)
				{
					cmd.CommandText = cmdText;
					cmd.ExecuteNonQuery();
				}
			}
		}

		[TestCleanup]
		public void TestCleanup()
		{

		}

		[TestMethod]
		public void MetaTest_CsvWithDifferentFieldCount()
		{
			/*
			1;2
			1;2;3;4
			1;2;3
			 */
			var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Meta", "001.csv");
			var reader = new CsvReader(new StreamReader(fileName, Encoding.GetEncoding(1251)), false, ';');
			reader.ReadNextRecord();
			Assert.AreEqual("1;2", String.Join(";", new[] { reader[0], reader[1] }));
			reader.ReadNextRecord();
			Assert.AreEqual("1;2;3;4", String.Join(";", new[] { reader[0], reader[1], reader[2], reader[3] }));
			reader.ReadNextRecord();
			Assert.AreEqual("1;2;3", String.Join(";", new[] { reader[0], reader[1], reader[2]}));
		}
		
		class Logger: ILogger
		{
			public void Log(LogItem item)
			{
				Console.WriteLine(item.Severity + ": " + item.Message);
			}

			public ILogItemProgress LogProgress()
			{
				return null;
			}
		}

		RuelectViewModel createViewModel()
		{
			var logger = new Logger();
			var repo = new Repository(m_connectionString, logger);
			repo.Downloader = new MockDownloader(new byte[10]);
			var viewModel = new RuelectViewModel(logger);
			viewModel.Repository = repo;
			return viewModel;
		}

		[TestMethod]
		public void LoadNewProtocols()
		{
			const string dataFile = "001.csv";
			var viewModel = createViewModel();
			var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RuElect", dataFile);
			viewModel.FileName = fileName;

			// Test:
			var task = viewModel.loadProtocols();
			Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10)));
			Assert.IsNull(viewModel.LastError, "Error occured: " + viewModel.LastError);

			// Check:
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				PollProtocol protocol;
				protocol = assertProtocol(con, dataFile, "Республика Тыва", 1);
				assertProtocolResults(con, protocol.Id, new[] {83, 97, 8, 92, 16, 948, 3});
				assertProtocolImages(con, protocol.Id, new[] { "http://ruelect.com/photos/4f803183-c6f7-9094-9925-daa6a2bae8c2.jpg" });

				protocol = assertProtocol(con, dataFile, "Город Москва", 6);
				assertProtocolResults(con, protocol.Id, new[] { 113, 86, 9, 202, 134, 128, 7 });
				assertProtocolImages(con, protocol.Id, new[] { "http://ruelect.com/photos/f02cabe2-be53-8d24-0d00-1c6db821ea79.jpg", "http://ruelect.com/photos/5296ccf5-6c97-3b54-5502-608f60443e1b.jpg" });

				protocol = assertProtocol(con, dataFile, "Республика Тыва", 6);
				assertProtocolResults(con, protocol.Id, new[] { 31, 53, 11, 48, 11, 1080, 4 });
				assertProtocolImages(con, protocol.Id, new[] { "http://ruelect.com/photos/4d5bd4bc-c38f-4df4-415d-30105c3d5f7e.jpg", "http://ruelect.com/photos/e1c7f89e-ead4-9904-31e6-8e31d4fa2a5b.jpg", "http://ruelect.com/photos/9eabfb1f-2a1e-6614-b9f6-e584e3e47e23.jpg" });
			}

			// Test: we expect no errors
			task = viewModel.loadProtocols();
			Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10)));
			Assert.IsNull(viewModel.LastError, "Error occured: " + viewModel.LastError);
		}

		[TestMethod]
		public void LoadAndUpdateProtocols()
		{
			const string dataFile1 = "002_1.csv";
			const string dataFile2 = "002_2.csv";
			const string dataFile = "002.csv";
			var viewModel = createViewModel();
			Task task;

			// Step 1
			// NOTE: we have to use the same file name as it's used as ResultProvider's name (and two names should be the same)
			File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RuElect", dataFile1), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataFile), true);
			viewModel.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataFile);
			// Test:
			task = viewModel.loadProtocols();
			Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10)));
			Assert.IsNull(viewModel.LastError, "Error occured: " + viewModel.LastError);

			// Step 2
			File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RuElect", dataFile2), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataFile), true);
			viewModel.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataFile);
			// Test:
			task = viewModel.loadProtocols();
			Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10)));
			Assert.IsNull(viewModel.LastError, "Error occured: " + viewModel.LastError);

			// Check:
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				PollProtocol protocol;
				protocol = assertProtocol(con, dataFile, "Республика Тыва", 1);
				assertProtocolResults(con, protocol.Id, new[] { 83,97,8,92,16,948,3 });


				protocol = assertProtocol(con, dataFile, "Город Москва", 1);
				assertProtocolResults(con, protocol.Id, new[] { 113,86,9,202,134,128,7 });
			}
		}

		private PollProtocol assertProtocol(SqlConnection con, string dataFile, string regionName, int comissionNum)
		{
			Guid providerId = assertProvider(con, dataFile);
			Guid regionId = assertRegion(con, regionName);
			Guid comissionId1 = assertComission(con, regionId, comissionNum);
			var protocol = assertProtocol(con, providerId, comissionId1);
			return protocol;
		}

		private Guid assertProvider(SqlConnection con, string name)
		{
			var cmd = con.CreateCommand();
			cmd.CommandText = "select ObjectID from ResultProvider where Name = '" + name + "'";
			using (var reader = cmd.ExecuteReader())
			{
				Assert.IsTrue(reader.Read());
				return reader.GetGuid(0);
			}
		}
		private Guid assertRegion(SqlConnection con, string name)
		{
			var cmd = con.CreateCommand();
			cmd.CommandText = "select ObjectID from Region where Name = '" + name + "'";
			using (var reader = cmd.ExecuteReader())
			{
				Assert.IsTrue(reader.Read());
				return reader.GetGuid(0);
			}
		}
		private Guid assertComission(SqlConnection con, Guid regionId, int comissionNum)
		{
			var cmd = con.CreateCommand();
			cmd.CommandText = "select ObjectID from Comission where Region = '" + regionId+ "' and Number = " + comissionNum;
			using (var reader = cmd.ExecuteReader())
			{
				Assert.IsTrue(reader.Read());
				return reader.GetGuid(0);
			}
		}
		private PollProtocol assertProtocol(SqlConnection con, Guid providerId, Guid comissionId)
		{
			var cmd = con.CreateCommand();
			cmd.CommandText = "select * from Protocol where Provider='" + providerId + "' and Comission='" + comissionId + "'";
			using (var reader = cmd.ExecuteReader())
			{
				Assert.IsTrue(reader.Read());
				Assert.AreEqual(3+18, reader.FieldCount);	// ObjectId, Provider, Comissin + Value1..Value18
				var protocol = new PollProtocol()
				               	{
				               		Id = reader.GetGuid(0),
				               		Results = new int[18]
				               	};
				for (int i = 0; i < 18; i++)
				{
					protocol.Results[i] = reader.GetInt32(i + 3);
				}
				return protocol;
			}
		}
		private void assertProtocolResults(SqlConnection con, Guid protocolId, int[] expected)
		{
			var cmd = con.CreateCommand();
			cmd.CommandText = "select Value from ProtocolResult where Protocol='" + protocolId + "' order by [Index]";
			using (var reader = cmd.ExecuteReader())
			{
				int idx = 0;
				while (reader.Read())
				{
					var value = reader.GetInt32(0);
					Assert.AreEqual(expected[idx], value);
					idx++;
				}
				Assert.AreEqual(expected.Length, idx);
			}
		}

		private void assertProtocolImages(SqlConnection con, Guid protocolId, string[] expected)
		{
			var cmd = con.CreateCommand();
			cmd.CommandText = "select Uri, Len(Image) from ProtocolImage where Protocol='" + protocolId + "' order by [Index]";
			using (var reader = cmd.ExecuteReader())
			{
				int idx = 0;
				while (reader.Read())
				{
					var uri = reader.GetString(0);
					Assert.AreEqual(expected[idx], uri);
					var size = reader.GetInt64(1);
					Assert.IsTrue(size > 0);
					idx++;
				}
				Assert.AreEqual(expected.Length, idx, "Protocol images count was expected to be equal to " + expected.Length + " but equals to " + idx);
			}
		}

		[TestMethod]
		public void ParseKartaitogovWebPage()
		{
			var viewModel = new KartaitogovViewModel(new Logger());
			//string resourceName = "Loader.Tests.diff.htm";
			//Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Kartaitogov", "diff.htm");
			//byte[] webpageContent = Encoding.UTF8.GetBytes(File.ReadAllText(filePath));

/*
			viewModel.Downloader = new MockDownloader(webpageContent);

			var task = viewModel.downloadImages();
			Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10)));
			Assert.IsNull(viewModel.LastError, "Error occured: " + viewModel.LastError);

*/

			HtmlDocument htmlDoc = new HtmlDocument();
			Encoding encoding = htmlDoc.DetectEncoding(filePath) ?? Encoding.UTF8;
			htmlDoc.Load(filePath, encoding);

			var reUikNumber = new System.Text.RegularExpressions.Regex(@"\d+");
			using (var con = new SqlConnection("Data Source=.;Initial Catalog = elect;Integrated Security=True"))
			{
				con.Open();
				var cmdRegion = con.CreateCommand();
				cmdRegion.CommandText = "select ObjectID from Region where name = @pName";
				var sqlParamRegName = new SqlParameter("pName", SqlDbType.VarChar);
				cmdRegion.Parameters.Add(sqlParamRegName);

				var cmdComission = con.CreateCommand();
				cmdComission.CommandText = "select ObjectID from Comission where Region = @pRegion and [Number] = @pNumber";
				var sqlParamComNum = new SqlParameter("pNumber", SqlDbType.Int);
				cmdComission.Parameters.Add(sqlParamComNum);
				var sqlParamRegId = new SqlParameter("pRegion", SqlDbType.UniqueIdentifier);
				cmdComission.Parameters.Add(sqlParamRegId);

				string regionName = null;
				Guid regionId = Guid.Empty;
				foreach (HtmlNode headUik in htmlDoc.DocumentNode.SelectNodes("//h3[@class='uik']"))
				{
					var regionNode = headUik.SelectSingleNode("preceding-sibling::h2[@class='oblast']");
					var uikText = headUik.InnerText;
					if (regionNode != null)
					{
						var match = reUikNumber.Match(uikText);
						if (!match.Success)
						{
							Console.WriteLine("ERROR: Can't parse UIK number: " + uikText);
						}
						else
						{
							if (regionName != regionNode.InnerText)
							{
								regionName = regionNode.InnerText;
								sqlParamRegName.Value = regionName;
								var regionIdRaw = cmdRegion.ExecuteScalar();
								if (regionIdRaw != null)
									regionId = (Guid)regionIdRaw;
								else
								{
									regionId = Guid.Empty;
									Console.WriteLine("WARN: Can't find in DB a region with name: " + regionName);
								}
							}

							sqlParamRegId.Value = regionId;
							int comissionNum = Int32.Parse(match.Value);
							sqlParamComNum.Value = comissionNum;
							var comissionIdRaw = cmdComission.ExecuteScalar();
							Guid comissionId;
							if (comissionIdRaw != null)
								comissionId = (Guid)comissionIdRaw;
							else
								comissionId = Guid.Empty;
							//Console.WriteLine(regionNode.InnerText + " : " + uikText.Substring(uikText.IndexOf('\n', 0, 2)));
							Console.WriteLine(regionName + "(" + regionId + ")" + " / " + comissionNum + "(" + comissionId + ")");
						}
					}
					else
					{
						Console.WriteLine("ERROR: Can't find region node!");
					}
				}
			}
		}
	}
}
