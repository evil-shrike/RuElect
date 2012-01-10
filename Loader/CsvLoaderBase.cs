using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Elect.DomainObjects;
using Elect.Loader.Common;

namespace Elect.Loader
{
	/// <summary>
	/// Base implementation of CSV Protocol Loader .
	/// </summary>
	public abstract class CsvLoaderBase : IProtocolLoader
	{
		protected CsvReader Reader { get; private set; }

		protected CsvLoaderBase(string fileName, char delimeter = ';')
		{
			Reader = new CsvReader(new StreamReader(fileName, Encoding.GetEncoding(1251)), false, delimeter);
		}

		protected int ColumnIndexRegion;
		protected int ColumnIndexComission;
		protected int ResultColumnsCount;
		protected int FirstResultColumnIndex;
		protected int FirstImageUriColumnIndex;

		public IEnumerable<PollProtocol> LoadProtocols(IRegionResolver regionResolver)
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

		public void Dispose()
		{
			Reader.Dispose();
		}
	}
}