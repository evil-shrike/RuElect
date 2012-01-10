using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elect.Loader
{
	public class RuelectCsvLoader : CsvLoaderBase
	{
		public RuelectCsvLoader(string fileName, char delimeter = ';')
			: base(fileName, delimeter)
		{
			ColumnIndexRegion = 0;
			ColumnIndexComission = 1;
			ResultColumnsCount = 25;
			FirstResultColumnIndex = 2;
			FirstImageUriColumnIndex = 27;
		}

		public void CheckRegions(IRegionResolver regionResolver)
		{
			var unknownRegions = new List<string>();
			while (Reader.ReadNextRecord())
			{
				string regionName = Reader[0];
				if (!regionResolver.Contains(regionName))
				{
					if (!unknownRegions.Contains(regionName))
						unknownRegions.Add(regionName);
				}
			}
			if (unknownRegions.Count > 0)
				throw new InvalidDataException("Unknown regions: " + String.Join(",", unknownRegions));
		}

		public string[] LoadRegions()
		{
			var regions = new HashSet<string>();
			while (Reader.ReadNextRecord())
			{
				string regionName = Reader[0];
				if (!regions.Contains(regionName))
				{
					regions.Add(regionName);
				}
			}
			return regions.ToArray();
		}

/*
		public IEnumerable<PollProtocol> LoadResults(IRegionResolver regionResolver)
		{
			const int colRegion = 0;
			const int colComission = 1;
			const int resultColCount = 25;

			while (Reader.ReadNextRecord())
			{
				string regionName = Reader[colRegion];
				Int32 commision;
				var images = new List<PollProtocolImage>();

				Region region = regionResolver.GetOrCreate(regionName);

				if (!Int32.TryParse(Reader[colComission], out commision))
					throw new InvalidDataException("Can't parse as Int32: " + Reader[colComission] + ". Row #" + Reader.CurrentRecordIndex);
				var results = new int[resultColCount];
				const int startIdx = 2;
				int i;
				for (i = startIdx; i < resultColCount + startIdx; i++)
				{
					results[i - startIdx] = Int32.Parse(Reader[i]);
				}
				Uri uri;
				for (; i < Reader.FieldCount; i++)
				{
					string value = Reader[i];
					if (string.IsNullOrWhiteSpace(value))
						break;
					if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
						throw new InvalidDataException("Can't parse as Uri: " + value + ". Row #" + Reader.CurrentRecordIndex);

					images.Add(new PollProtocolImage() { Uri = uri.ToString() });
				}
				var result = new PollProtocol
				             	{
				             		Region = region,
				             		Comission = commision,
				             		Results = results,
				             		Images = images
				             	};
				yield return result;
			}
		}
*/

	}
}