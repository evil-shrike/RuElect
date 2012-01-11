using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Elect.DomainObjects;

namespace Elect.Loader
{
	public class Repository : IRegionResolver
	{
		private int ProtocolCommonValuesCount = 18;
		private int ProtocolVotesCount = 7;

		private readonly string m_connectionString;
		private readonly ILogger m_logger;
		private bool m_bInitialized;
		private readonly Dictionary<Guid, Region> m_regions = new Dictionary<Guid, Region>();
		private readonly Dictionary<string, Guid> m_regionsNameToId = new Dictionary<string, Guid>();
		private List<Comission> m_comissions;
		private readonly Dictionary<Guid, ResultProvider> m_providers = new Dictionary<Guid, ResultProvider>();
		private readonly Dictionary<string, Guid> m_providersNameToId = new Dictionary<string, Guid>();

		public Repository(string connectionString, ILogger logger)
		{
			m_connectionString = connectionString;
			m_logger = logger;
		}

		public IDownloader Downloader { get; set; }

		public void Initialize()
		{
			loadRegions();
			loadComissions();
			loadProviders();
			m_bInitialized = true;
		}

		private void loadRegions()
		{
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = "select ObjectID, Name from Region";
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						string name = reader.GetString(1);
						Guid id = reader.GetGuid(0);
						m_regionsNameToId[name] = id;
						m_regions[id] = new Region {Id = id, Name = name};
					}
				}
			}
		}

		private void loadComissions()
		{
			m_comissions = new List<Comission>();
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = "select ObjectID, [Region], [Number] from Comission";
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var comission = new Comission()
						                	{
						                		Id = reader.GetGuid(0),
						                		Region = m_regions[reader.GetGuid(1)],
						                		Number = reader.GetInt32(2)
						                	};
						m_comissions.Add(comission);
					}
				}
			}
		}

		private void loadProviders()
		{
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = "select ObjectID, Name from ResultProvider";
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var provider = new ResultProvider
						               	{
						               		Id = reader.GetGuid(0),
						               		Name = reader.GetString(1)
						               	};
						m_providers[provider.Id] = provider;
						m_providersNameToId[provider.Name] = provider.Id;
					}
				}
			}
		}

		private void ensureInitialized()
		{
			if (!m_bInitialized)
				Initialize();
		}

		public ResultProvider GetOrCreateProvider(string providerName, out bool isNew)
		{
			isNew = false;
			ResultProvider provider;
			Guid providerId;
			if (m_providersNameToId.TryGetValue(providerName, out providerId))
			{
				provider = m_providers[providerId];
			}
			else
			{
				providerId = insertProvider(providerName, true);
				provider = new ResultProvider {Id = providerId, Name = providerName};
				m_logger.Log("Created new provider: name=" + providerName + ", id=" + providerId);
				isNew = true;
			}
			return provider;
		}

		private Guid insertProvider(string name, bool isFile)
		{
			Guid id;
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = "insert into ResultProvider (ObjectID, Name, IsFile) values (@p1, @p2, @p3)";
				var sqlParam = new SqlParameter("p1", SqlDbType.UniqueIdentifier);
				id = Guid.NewGuid();
				sqlParam.Value = id;
				cmd.Parameters.Add(sqlParam);
				sqlParam = new SqlParameter("p2", SqlDbType.VarChar);
				sqlParam.Value = name;
				cmd.Parameters.Add(sqlParam);
				sqlParam = new SqlParameter("p3", SqlDbType.Bit);
				sqlParam.Value = isFile;
				cmd.Parameters.Add(sqlParam);

				cmd.ExecuteNonQuery();
			}
			return id;
		}

		public PollProtocol LoadProtocol(PollProtocol protocol)
		{
			ensureInitialized();

			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText =
					@"select p.ObjectID, p.Provider,
p.Value1, p.Value2, p.Value3, p.Value4, p.Value5, p.Value6, p.Value7, p.Value8, p.Value9, p.Value10, p.Value11, p.Value12, p.Value13, p.Value14, p.Value15, p.Value16, p.Value17, p.Value18
from Protocol p join Comission c on p.Comission = c.ObjectID
where p.Provider = @p1 and c.Number = @p2 and c.Region = @p3";

				// p1 - ProviderId
				var paramName = new SqlParameter("p1", SqlDbType.UniqueIdentifier);
				paramName.Value = protocol.Provider.Id;
				cmd.Parameters.Add(paramName);
				// p2 - Comission.Number
				paramName = new SqlParameter("p2", SqlDbType.Int);
				paramName.Value = protocol.Comission;
				cmd.Parameters.Add(paramName);
				// p2 - Comission.Region
				paramName = new SqlParameter("p3", SqlDbType.UniqueIdentifier);
				paramName.Value = protocol.Region.Id;
				cmd.Parameters.Add(paramName);

				//m_comissions.Where(c => c.Region.Id =)
				var protocols = loadProtocols(cmd, protocol.Region, protocol.Comission);
				return protocols.FirstOrDefault();
			}
		}

		private IList<PollProtocol> loadProtocols(DbCommand cmd, Region region, int comissionNum)
		{
			var protocols = new List<PollProtocol>();
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					const int offset = 2; // start index at which protocol result values begin (we need skip Object, Provider)

					var protocol = new PollProtocol
					               	{
					               		Id = reader.GetGuid(0),
					               		Provider = m_providers[reader.GetGuid(1)],
					               		Region = region,
					               		Comission = comissionNum,
					               		Results = new int[ProtocolCommonValuesCount + ProtocolVotesCount]
					               	};
					for (int i = 0; i < ProtocolCommonValuesCount; i++)
					{
						protocol.Results[i] = reader.GetInt32(offset + i);
					}
					protocols.Add(protocol);
				}
			}
			if (protocols.Count > 0)
			{
				// load candidates' results
				cmd.Parameters.Clear();
				cmd.CommandText = "select Value from ProtocolResult where Protocol = @p order by [Index]";
				var sqlParam = cmd.CreateParameter();
				sqlParam.ParameterName = "p";
				sqlParam.DbType = DbType.Guid;
				cmd.Parameters.Add(sqlParam);

				foreach (PollProtocol protocol in protocols)
				{
					sqlParam.Value = protocol.Id;
					using (var reader = cmd.ExecuteReader())
					{
						int i = ProtocolCommonValuesCount;
						while (reader.Read())
						{
							protocol.Results[i] = reader.GetInt32(0);
							i++;
						}
					}
				}
			}
			// TODO: load protocols' images
			return protocols;
		}

		public bool SaveProtocol(PollProtocol protocol, ProtocolSaveOption options)
		{
			ensureInitialized();

			Comission comission = m_comissions.FirstOrDefault(
				comissionTmp => comissionTmp.Region.Id == protocol.Region.Id && comissionTmp.Number == protocol.Comission);

			SqlCommand cmd;
			SqlParameter sqlParam;
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();

				// create region if needed
				if (protocol.Region.IsNew)
				{
					switch (options.UnknownRegionAction)
					{
						case UnknownRegionActions.Create:
							createRegion(protocol.Region);
							m_logger.Log(String.Format("\tCreated new region: '{0}' - id='{1}'", protocol.Region.Name, protocol.Region.Id));
							Debug.Assert(!protocol.Region.IsNew);
							break;
						case UnknownRegionActions.Ignore:
						case UnknownRegionActions.Stop:
							m_logger.Log(String.Format("\tNot existed region '{0}' was ignored", protocol.Region.Name));
							return false;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				// create comission if needed
				Guid comissionId;
				if (comission == null)
				{
					// we need to create the protocol's comission as it doesn't exist
					switch (options.UnknownComissionAction)
					{
						case UnknownComissionActions.Create:
							comissionId = insertComission(con, protocol);
							m_logger.Log(String.Format("\tCreated new comission: {0} (region: '{1}') - id='{2}'", protocol.Comission,
							                           protocol.Region.Name, comissionId));
							break;
						case UnknownComissionActions.Ignore:
						case UnknownComissionActions.Stop:
							m_logger.Log(String.Format("\tNot existed comission '{0}' (region:'{1}') was ignored", protocol.Comission,
							                           protocol.Region.Name));
							return false;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				else
				{
					comissionId = comission.Id;
				}

				// TODO: UPDATE Protocol/ProtocolResult/ProtocolImage

				// base results
				cmd = con.CreateCommand();
				cmd.CommandText =
					"insert into Protocol (ObjectID, [Provider], [Comission], [Value1], [Value2], [Value3], [Value4], [Value5], [Value6], [Value7], [Value8], [Value9], [Value10], [Value11], [Value12], [Value13], [Value14], [Value15], [Value16], [Value17], [Value18]) " +
					"values (@pId, @pPr, @pCom, @pV1, @pV2, @pV3, @pV4, @pV5, @pV6, @pV7, @pV8, @pV9, @pV10, @pV11, @pV12, @pV13, @pV14, @pV15, @pV16, @pV17, @pV18)";
				// ObjectID
				sqlParam = new SqlParameter("pId", SqlDbType.UniqueIdentifier);
				var protocolId = Guid.NewGuid();
				sqlParam.Value = protocolId;
				cmd.Parameters.Add(sqlParam);
				// Provider
				sqlParam = new SqlParameter("pPr", SqlDbType.UniqueIdentifier);
				sqlParam.Value = protocol.Provider.Id;
				cmd.Parameters.Add(sqlParam);
				// Comission
				sqlParam = new SqlParameter("pCom", SqlDbType.UniqueIdentifier);
				sqlParam.Value = comissionId;
				cmd.Parameters.Add(sqlParam);
				for (int i = 0; i < ProtocolCommonValuesCount; i++)
				{
					sqlParam = new SqlParameter("pV" + (i + 1), SqlDbType.Int);
					sqlParam.Value = protocol.Results[i];
					cmd.Parameters.Add(sqlParam);
				}
				cmd.ExecuteNonQuery();


				// results by candidates
				cmd = con.CreateCommand();
				cmd.CommandText = "insert into ProtocolResult (ObjectID, [Protocol], [Candidate], [Value], [Index]) " +
				                  "values (@pId, @pProto, @pCandidate, @pValue, @pIndex)";
				// ObjectID
				var sqlParamId = new SqlParameter("pId", SqlDbType.UniqueIdentifier);
				cmd.Parameters.Add(sqlParamId);
				// Protocol
				sqlParam = new SqlParameter("pProto", SqlDbType.UniqueIdentifier);
				sqlParam.Value = protocolId;
				cmd.Parameters.Add(sqlParam);

				// Candidate
				var sqlParamCandidate = new SqlParameter("pCandidate", SqlDbType.UniqueIdentifier);
				cmd.Parameters.Add(sqlParamCandidate);
				// Value
				var sqlParamValue = new SqlParameter("pValue", SqlDbType.Int);
				cmd.Parameters.Add(sqlParamValue);
				// Index
				var sqlParamIndex = new SqlParameter("pIndex", SqlDbType.Int);
				cmd.Parameters.Add(sqlParamIndex);

				IList<Candidate> candidates = protocol.Provider.GetCandidates();
				int idx = 0;
				foreach (var candidate in candidates)
				{
					sqlParamId.Value = Guid.NewGuid();
					sqlParamCandidate.Value = candidate.Id;
					sqlParamValue.Value = protocol.Results[ProtocolCommonValuesCount + idx];
					sqlParamIndex.Value = idx;

					cmd.ExecuteNonQuery();
					idx++;
				}

				// images of protocol
				if (protocol.Images != null)
				{
					// NOTE: if we decide to download in parallel then we'll have to refuse using of single DownloadNotifier instance!
					if (Downloader != null)
						foreach (var image in protocol.Images)
						{
							var downloader = Downloader;
							if (image.Image == null && downloader != null)
							{
								Task<byte[]> task = downloader.Download(image.Uri, options.DownloadNotifier, options.CancellationToken);
								try
								{
									task.Wait();
								}
								catch
								{
									options.CancellationToken.ThrowIfCancellationRequested();
									throw;
								}
								image.Image = task.Result;
							}
						}
					idx = 0;

					foreach (var image in protocol.Images)
					{
						if (String.IsNullOrEmpty(image.Uri) && image.Image == null)
						{
							// what's matter in creating empty object?
							//m_logger.Log("\t");
						}
						else
						{
							cmd = con.CreateCommand();
							cmd.CommandText = "insert into ProtocolImage (ObjectID, [Protocol], [Uri], [Image], [Index]) " +
							                  "values (@pId, @pProto, @pUri, @pImage, @pIndex)";
							// ObjectID
							sqlParam = new SqlParameter("pId", SqlDbType.UniqueIdentifier);
							sqlParam.Value = Guid.NewGuid();
							cmd.Parameters.Add(sqlParam);
							// Protocol
							sqlParam = new SqlParameter("pProto", SqlDbType.UniqueIdentifier);
							sqlParam.Value = protocolId;
							cmd.Parameters.Add(sqlParam);
							// Uri
							sqlParam = new SqlParameter("pUri", SqlDbType.VarChar);
							sqlParam.Value = image.Uri;
							if (String.IsNullOrEmpty(image.Uri))
								sqlParam.Value = DBNull.Value;
							cmd.Parameters.Add(sqlParam);
							// Image
							sqlParam = new SqlParameter("pImage", SqlDbType.VarBinary);
							sqlParam.Value = image.Image;
							if (image.Image == null)
								sqlParam.Value = DBNull.Value;
							sqlParam.IsNullable = true;
							cmd.Parameters.Add(sqlParam);
							// Index
							sqlParam = new SqlParameter("pIndex", SqlDbType.Int);
							sqlParam.Value = idx;
							cmd.Parameters.Add(sqlParam);

							cmd.ExecuteNonQuery();
							idx++;
						}
					}
				}
			}
			return true;
		}

		private Guid insertComission(SqlConnection con, PollProtocol protocol)
		{
			var cmd = con.CreateCommand();
			cmd.CommandText = "insert into Comission (ObjectID, Region, [Number]) values (@p1, @p2, @p3)";
			// ObjectID
			var sqlParam = new SqlParameter("p1", SqlDbType.UniqueIdentifier);
			Guid comissionId = Guid.NewGuid();
			sqlParam.Value = comissionId;
			cmd.Parameters.Add(sqlParam);
			// Region
			sqlParam = new SqlParameter("p2", SqlDbType.UniqueIdentifier);
			sqlParam.Value = protocol.Region.Id;
			cmd.Parameters.Add(sqlParam);
			// Number
			sqlParam = new SqlParameter("p3", SqlDbType.Int);
			sqlParam.Value = protocol.Comission;
			cmd.Parameters.Add(sqlParam);

			cmd.ExecuteNonQuery();
			return comissionId;
		}

		private void createRegion(Region region)
		{
			ensureInitialized();
			Debug.Assert(region.IsNew);

			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = "insert into Region (ObjectID, Name) values (@p1, @p2)";
				// ObjectID
				var sqlParamId = new SqlParameter("p1", SqlDbType.UniqueIdentifier);
				cmd.Parameters.Add(sqlParamId);
				sqlParamId.Value = region.Id;
				// Name
				var sqlParamName = new SqlParameter("p2", SqlDbType.VarChar);
				cmd.Parameters.Add(sqlParamName);
				sqlParamName.Value = region.Name;

				cmd.ExecuteNonQuery();

				m_regionsNameToId[region.Name] = region.Id;
				m_regions[region.Id] = region;
				region.IsNew = false;
			}
		}

		public int UpdateRegions(string[] regions)
		{
			ensureInitialized();
			int createCount = 0;
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText = "insert into Region (ObjectID, Name) values (@p1, @p2)";
				var sqlParamId = new SqlParameter("p1", SqlDbType.UniqueIdentifier);
				cmd.Parameters.Add(sqlParamId);
				var sqlParamName = new SqlParameter("p2", SqlDbType.VarChar);
				cmd.Parameters.Add(sqlParamName);

				foreach (var region in regions)
				{
					if (!m_regionsNameToId.ContainsKey(region))
					{
						var id = Guid.NewGuid();
						sqlParamId.Value = id;
						sqlParamName.Value = region;
						cmd.ExecuteNonQuery();
						m_regionsNameToId[region] = id;
						m_regions[id] = new Region() {Id = id, Name = region};
						createCount++;
					}
				}
			}
			return createCount;
		}

		Region IRegionResolver.GetOrCreate(string name)
		{
			ensureInitialized();
			Guid id;
			if (m_regionsNameToId.TryGetValue(name, out id))
				return m_regions[id];

			var region = new Region() {Id = Guid.NewGuid(), Name = name, IsNew = true};
			return region;
		}

		/*

				bool IRegionResolver.TryGet(string name, out Region region)
				{
					ensureInitialized();
					Guid id;
					region = null;
					if (m_regionsNameToId.TryGetValue(name, out id))
					{
						region = m_regions[id];
						return true;
					}
					return false;
				}
		*/

		bool IRegionResolver.Contains(string name)
		{
			ensureInitialized();
			Guid id;
			return m_regionsNameToId.TryGetValue(name, out id);
		}

		public void EnsurePollExists(Poll poll)
		{
			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				using (var tran = con.BeginTransaction())
				{
					var cmd = con.CreateCommand();
					cmd.Transaction = tran;
					cmd.CommandText = @"select ObjectID, Name from Poll where lower(Name) = @p";

					var sqlParameter = new SqlParameter("p", SqlDbType.VarChar);
					sqlParameter.Value = poll.Name.ToLower();
					cmd.Parameters.Add(sqlParameter);

					bool bNew = true;
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							poll.Id = reader.GetGuid(0);
							poll.Name = reader.GetString(1);
							bNew = false;
						}
					}
					if (bNew)
					{
						cmd = con.CreateCommand();
						cmd.Transaction = tran;
						cmd.CommandText = @"insert into Poll (ObjectID, Name) values (@p1, @p2)";
						sqlParameter = new SqlParameter("p1", SqlDbType.UniqueIdentifier);
						if (poll.Id == Guid.Empty)
							poll.Id = Guid.NewGuid();
						sqlParameter.Value = poll.Id;
						cmd.Parameters.Add(sqlParameter);
						sqlParameter = new SqlParameter("p2", SqlDbType.VarChar);
						sqlParameter.Value = poll.Name;
						cmd.Parameters.Add(sqlParameter);
						cmd.ExecuteNonQuery();
					}

					int candidateIdx = 0;
					foreach (var candidate in poll.Candidates)
					{
						bool bCreate = bNew;
						if (!bNew)
						{
							cmd = con.CreateCommand();
							cmd.Transaction = tran;
							cmd.CommandText = @"select ObjectID from Candidate where lower(Name) = @p1 and Poll = @p2";

							sqlParameter = new SqlParameter("p1", SqlDbType.VarChar);
							sqlParameter.Value = candidate.Name.ToLower();
							cmd.Parameters.Add(sqlParameter);

							sqlParameter = new SqlParameter("p2", SqlDbType.UniqueIdentifier);
							sqlParameter.Value = poll.Id;
							cmd.Parameters.Add(sqlParameter);

							var candidateIdRaw = cmd.ExecuteScalar();
							if (candidateIdRaw != null)
								candidate.Id = (Guid) candidateIdRaw;
							bCreate = (candidateIdRaw == null);
						}

						if (bCreate)
						{
							cmd = con.CreateCommand();
							cmd.Transaction = tran;
							cmd.CommandText = @"insert into Candidate (ObjectID, Name, Poll, [Index]) values (@p1, @p2, @p3, @p4)";
							// ObjectID
							sqlParameter = new SqlParameter("p1", SqlDbType.UniqueIdentifier);
							if (candidate.Id == Guid.Empty)
								candidate.Id = Guid.NewGuid();
							sqlParameter.Value = candidate.Id;
							cmd.Parameters.Add(sqlParameter);
							// Name
							sqlParameter = new SqlParameter("p2", SqlDbType.VarChar);
							sqlParameter.Value = candidate.Name;
							cmd.Parameters.Add(sqlParameter);
							// Poll
							sqlParameter = new SqlParameter("p3", SqlDbType.UniqueIdentifier);
							sqlParameter.Value = poll.Id;
							cmd.Parameters.Add(sqlParameter);
							// Index
							sqlParameter = new SqlParameter("p4", SqlDbType.Int);
							sqlParameter.Value = candidateIdx;
							cmd.Parameters.Add(sqlParameter);

							cmd.ExecuteNonQuery();
						}
						candidateIdx++;
					}
					tran.Commit();
				}
			}
		}

		public IEnumerable<Region> GetRegions()
		{
			ensureInitialized();
			return m_regions.Values;
		}

		public IEnumerable<Comission> GetComissions(Guid regionId)
		{
			ensureInitialized();
			return m_comissions.Where(c => c.Region.Id == regionId);
		}

		public IEnumerable<PollProtocol> GetComissionProtocols(Comission comission)
		{
			ensureInitialized();

			using (var con = new SqlConnection(m_connectionString))
			{
				con.Open();
				var cmd = con.CreateCommand();
				cmd.CommandText =
					@"select p.ObjectID, p.Provider,
p.Value1, p.Value2, p.Value3, p.Value4, p.Value5, p.Value6, p.Value7, p.Value8, p.Value9, p.Value10, p.Value11, p.Value12, p.Value13, p.Value14, p.Value15, p.Value16, p.Value17, p.Value18
from Protocol p join Comission c on p.Comission = c.ObjectID
where c.Number = @p1 and c.Region = @p2";

				// p1 - Comission.Number
				var sqlParam = new SqlParameter("p1", SqlDbType.Int);
				sqlParam.Value = comission.Number;
				cmd.Parameters.Add(sqlParam);
				// p2 - Comission.Region
				sqlParam = new SqlParameter("p2", SqlDbType.UniqueIdentifier);
				sqlParam.Value = comission.Region.Id;
				cmd.Parameters.Add(sqlParam);

				var protocols = loadProtocols(cmd, comission.Region, comission.Number);
				return protocols;
			}
		}
	}
}