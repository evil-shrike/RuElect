using System;
using System.IO;

namespace Elect.Loader.Common
{
	public class CsvReader
	{
		private readonly StreamReader _reader;
		private readonly bool _hasHeaders;
		private readonly char _delimeter;
		private string[] _currentRow;
		private bool _initialized;
		private int _currentRowIndex = -1;
		private bool _eos;

		public CsvReader(StreamReader reader, bool hasHeaders, char delimeter)
		{
			_initialized = false;
			_reader = reader;
			_hasHeaders = hasHeaders;
			_delimeter = delimeter;
		}

		public Int32 CurrentRecordIndex
		{
			get { return _currentRowIndex; }
		}

		public Int32 FieldCount
		{
			get
			{
				if (_currentRow != null)
					return _currentRow.Length;
				return -1;
			}
		}

		public bool ReadNextRecord()
		{
			_currentRow = null;
			if (_eos)
				return false;
			_currentRowIndex++;
			var row = _reader.ReadLine();
			_eos = _reader.EndOfStream;
			if (row != null)
			{
				_currentRow = row.Split(_delimeter);
			}
			_initialized = true;
			return true;
		}

		public string this[int index]
		{
			get
			{
				if (_currentRow == null)
					throw new InvalidOperationException("No current row data");
				if (_currentRow.Length < index + 1)
					throw new IndexOutOfRangeException(string.Format("Index {0} of row #{1} is out of range", index, _currentRowIndex));
				return _currentRow[index];
			}
		}

		public void Dispose()
		{
			_reader.Close();
		}
	}
}
