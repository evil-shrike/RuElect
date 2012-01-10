using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Elect.DomainObjects;
using HtmlAgilityPack;

namespace Elect.Loader
{
	public class KartaitogovWebLoader: IProtocolLoader
	{
		private readonly string m_pageContent;
		private readonly IDictionary<string, string> m_imageFilesMap;
		private readonly ILogger m_logger;

		public KartaitogovWebLoader(string pageContent, IDictionary<string, string> imageFilesMap, ILogger logger)
		{
			m_pageContent = pageContent;
			m_imageFilesMap = imageFilesMap;
			m_logger = logger;
		}

		public IEnumerable<PollProtocol> LoadProtocols(IRegionResolver regionResolver)
		{
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(m_pageContent);

			var reUikNumber = new Regex(@"\d+", RegexOptions.Compiled | RegexOptions.Multiline);

			foreach (HtmlNode headUik in htmlDoc.DocumentNode.SelectNodes("//h3[@class='uik']"))
			{
				var regionNode = headUik.SelectSingleNode("preceding-sibling::h2[@class='oblast']");
				var uikText = headUik.InnerText;
				if (regionNode == null)
				{
					m_logger.LogError("Can't find region node in the html");
				}
				else
				{
					var match = reUikNumber.Match(uikText);
					if (!match.Success)
					{
						m_logger.LogError("Can't parse comission number: " + uikText);
						continue;
					}
					int comissionNum;
					if (!Int32.TryParse(match.Value, out comissionNum))
					{
						m_logger.LogError("Can't parse parse comission number as integer: " + match.Value);
						continue;
					}

					string regionName = regionNode.InnerText;
					Region region = regionResolver.GetOrCreate(regionName);

					// Results
					var tableResults = headUik.SelectSingleNode("following-sibling::table[@class='observers_data']");
					if (tableResults == null)
					{
						m_logger.LogError(String.Format("{0}/{1}: Can't find table with protocols results (table[@class='observers_data'])", regionName, comissionNum));
						continue;
					}
					var trAltResults = tableResults.SelectSingleNode("descendant::tr[@class='obs']");
					if (trAltResults == null)
					{
						m_logger.LogError(String.Format("{0}/{1}: Can't find table row with alternative protocol results (tr[@class='obs'])", regionName, comissionNum));
						continue;
					}

					var tdResults = trAltResults.SelectNodes("td");
					var results = new int[25];
					if (tdResults.Count != 26)
					{
						m_logger.LogError(String.Format("{0}/{1}: Number of columns in row with protocol result doesn't equal expected value (26), but equals to {2}", regionName, comissionNum, tdResults.Count));
						continue;
					}

					bool needBreak = false;
					for (int i = 1; i < 26; i++)
					{
						string votesRaw = tdResults[i].InnerText;
						int votes;
						if (!Int32.TryParse(votesRaw, out votes))
						{
							m_logger.LogError(String.Format("{0}/{1}: Can't parse votes count as integer: {2}", regionName, comissionNum, tdResults.Count));
							needBreak = true;
							break;
						}
						results[i - 1] = votes;
					}
					if (needBreak)
						continue;

					// Images:
					var imagesNode = headUik.SelectSingleNode("following-sibling::div[@class='photo_names']");
					if (imagesNode == null)
					{
						m_logger.LogError(String.Format("{0}/{1}: Can't find div with protocol images (div[@class='photo_names'])", regionName, comissionNum));
						continue;
					}
					var images = new List<PollProtocolImage>();
					var imageAnchorNodes = imagesNode.SelectNodes("descendant::a");
					if (imageAnchorNodes == null || imageAnchorNodes.Count == 0)
						m_logger.LogWarning(String.Format("{0}/{1}: Can't find hyperlinks to protocol images (a)", regionName, comissionNum));
					else
					{
						foreach (HtmlNode anchorNode in imageAnchorNodes)
						{
							string uri = anchorNode.GetAttributeValue("href", "");
							// remove uri parameters (everything after "?")
							var idx = uri.IndexOf("?");
							if (idx > -1)
								uri = uri.Substring(0, idx);
							string filePath;
							byte[] imageBytes = null;
							if (m_imageFilesMap.TryGetValue(uri, out filePath))
								imageBytes = File.ReadAllBytes(filePath);
							var protocolImage = new PollProtocolImage
							                    	{
							                    		Uri = uri,
							                    		Image = imageBytes
							                    	};
							images.Add(protocolImage);
						}
					}

					var protocol = new PollProtocol
					               	{
					               		Region = region,
					               		Comission = comissionNum,
					               		Images = images,
					               		Results = results
					               	};

					yield return protocol;
				}
			}
		}

		public void Dispose()
		{ }
	}
}