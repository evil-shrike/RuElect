using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Elect.DomainObjects;
using Elect.Loader.Common;

namespace Elect.Loader
{
	public abstract class CsvLoaderBase
	{
		protected CsvReader Reader { get; private set; }

		protected CsvLoaderBase(string fileName, char delimeter = ';')
		{
			Reader = new CsvReader(new StreamReader(fileName, Encoding.GetEncoding(1251)), false, delimeter);
		}

		public void Dispose()
		{
			Reader.Dispose();
		}

		protected int ColumnIndexRegion;
		protected int ColumnIndexComission;
		protected int ResultColumnsCount;
		protected int FirstResultColumnIndex;
		protected int FirstImageUriColumnIndex;

		public IEnumerable<PollProtocol> LoadResults(IRegionResolver regionResolver)
		{
			while (Reader.ReadNextRecord())
			{
				string regionName = Reader[ColumnIndexRegion];
				Int32 commision;

				Region region = regionResolver.GetOrCreate(regionName);

				if (!Int32.TryParse(Reader[ColumnIndexComission], out commision))
					throw new InvalidDataException("Can't parse as Int32: " + Reader[ColumnIndexComission] + ". Row #" + Reader.CurrentRecordIndex);
				var results = new int[ResultColumnsCount];
				
				for (int i = 0; i < ResultColumnsCount; i++)
				{
					results[i] = Int32.Parse(Reader[i + FirstResultColumnIndex]);
				}
				// Images
				List<PollProtocolImage> images = null;
				if (FirstImageUriColumnIndex > -1)
				{
					images = new List<PollProtocolImage>();
					Uri uri;
					for (int i = FirstImageUriColumnIndex; i < Reader.FieldCount; i++)
					{
						string value = Reader[i];
						if (string.IsNullOrWhiteSpace(value))
							break;
						if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
							throw new InvalidDataException("Can't parse as Uri: " + value + ". Row #" + Reader.CurrentRecordIndex);

						images.Add(new PollProtocolImage { Uri = uri.ToString() });
					}
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
	}

	public class KartaitogovCsvLoader : CsvLoaderBase
	{
		/*
kom1;kom2;Ссылка;Регион;№ УИК;Число избирателей, внесенных в список избирателей на момент окончания голосования;Число избирательных бюллетеней, полученных участковой избирательной комиссией;Число избирательных бюллетеней, выданных избирателям, проголосовавшим досрочно;Число избирательных бюллетеней, выданных участковой избирательной комиссией избирателям в помещении для голосования в день голосования;Число бюллетеней, выданных избирателям, проголосовавшим вне помещения для голосования в день голосования;Число погашенных бюллетеней;Число бюллетеней, содержащихся в переносных ящиках для голосования;Число избирательных бюллетеней, содержащихся в стационарных ящиках для голосования;Число недействительных избирательных бюллетеней;Число действительных избирательных бюллетеней;Число открепительных удостоверений, полученных участковой избирательной комиссией;Число открепительных удостоверений, выданных участковой избирательной комиссией избирателям на избирательном участке до дня голосования;Число избирателей, проголосовавших по открепительным удостоверениям на избирательном участке;Число погашенных неиспользованных открепительных удостоверений;Число открепительных удостоверений, выданных избирателям территориальной избирательной комиссией;Число утраченных открепительных удостоверений;Число утраченных избирательных бюллетеней;Число избирательных бюллетеней, не учтенных при получении;Справедливая Россия;ЛДПР;Патриоты России;КПРФ;ЯБЛОКО;Единая Россия;Правое дело
		 */
		public KartaitogovCsvLoader(string fileName, char delimeter = ';')
			: base(fileName, delimeter)
		{
			ColumnIndexRegion = 3;
			ColumnIndexComission = 4;
			ResultColumnsCount = 25;
			FirstResultColumnIndex = 5;
			FirstImageUriColumnIndex = -1;
		}
	}

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