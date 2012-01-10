// ******************************************************************************
//  Copyright (C) CROC Inc. 2007-2009. All rights reserved.
//  Инструментальная среда CROC XFW3
// ******************************************************************************
using System;
using System.IO;
using System.Threading;

namespace Croc.XFW3.Utils
{
	/// <summary>
	/// Класс с набором утилитарных методов по работе с потоками (<see cref="Stream"/>)
	/// </summary>
	public static class StreamUtil
	{
		/// <summary>
		/// размер буфера по умолчанию
		/// </summary>
		private const int DefaultBufferSize = 8*1024;

		/// <summary>
		/// Считывает данные из переданного потока, возвращая их в виде массива байт.
		/// </summary>
		/// <param name="input">The stream to read from</param>
		/// <exception cref="ArgumentNullException">input is null</exception>
		/// <exception cref="IOException">An error occurs while reading from the stream</exception>
		/// <returns>The data read from the stream</returns>
		public static Byte[] ReadFully(Stream input)
		{
			return ReadFully(input, DefaultBufferSize);
		}

		/// <summary>
		/// Считывает данные из переданного потока, возвращая их в виде массива байт, 
		/// используя заданный размер для временного буфера.
		/// </summary>
		/// <param name="input">Поток для чтения. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="bufferSize">The size of buffer to use when reading</param>
		/// <exception cref="ArgumentNullException">input is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">bufferSize is less than 1</exception>
		/// <exception cref="IOException">An error occurs while reading from the stream</exception>
		/// <returns>The data read from the stream</returns>
		public static Byte[] ReadFully(Stream input, Int64 bufferSize)
		{
			if (bufferSize < 1)
			{
				throw new ArgumentOutOfRangeException("bufferSize");
			}
			return ReadFully(input, new Byte[bufferSize]);
		}

		/// <summary>
		/// Считывает данные из переданного потока, возвращая их в виде массива байт, 
		/// используя заданный <paramref name="buffer"/> как временный буфер.
		/// Начальное содержимое буфера игнорируется.
		/// </summary>
		/// <param name="input">Поток для чтения. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="buffer">Буфер для переноса данных</param>
		/// <exception cref="ArgumentNullException">input is null</exception>
		/// <exception cref="ArgumentNullException">buffer is null</exception>
		/// <exception cref="ArgumentException">buffer is a zero-length array</exception>
		/// <exception cref="IOException">An error occurs while reading from the stream</exception>
		/// <returns>Считанные данные</returns>
		public static Byte[] ReadFully(Stream input, Byte[] buffer)
		{
			if (buffer.Length == 0)
			{
				throw new ArgumentException("Buffer has length of 0");
			}

			// We could do all our own work here, but using MemoryStream is easier
			// and likely to be just as efficient.
			using (var tempStream = new MemoryStream())
			{
				CopyFully(input, tempStream, buffer);
				// No need to copy the buffer if it's the right size
				if (tempStream.Length == tempStream.GetBuffer().Length)
				{
					return tempStream.GetBuffer();
				}
				// Okay, make a copy that's the right size
				return tempStream.ToArray();
			}
		}

		/// <summary>
		/// Копирует все данные из одного потока в другой, используя внутренний буфер.
		/// </summary>
		/// <param name="input">Поток для чтения. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="output">Поток для записи. Если поддерживает Seek, устанавливается в начало.</param>
		/// <exception cref="ArgumentNullException">input is null</exception>
		/// <exception cref="ArgumentNullException">output is null</exception>
		/// <exception cref="IOException">An error occurs while reading or writing</exception>
		public static void CopyFully(Stream input, Stream output)
		{
			CopyFully(input, output, new byte[DefaultBufferSize], CancellationToken.None);
		}

		/// <summary>
		/// Копирует все данные из одного потока в другой, используя внутренний буфер.
		/// </summary>
		/// <param name="input">Поток для чтения. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="output">Поток для записи. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="cancellationToken">токен для прерывания операции.</param>
		/// <exception cref="ArgumentNullException">input is null</exception>
		/// <exception cref="ArgumentNullException">output is null</exception>
		/// <exception cref="IOException">An error occurs while reading or writing</exception>
		public static void CopyFully(Stream input, Stream output, CancellationToken cancellationToken)
		{
			CopyFully(input, output, new byte[DefaultBufferSize], cancellationToken);
		}

		/// <summary>
		/// Копирует все данные из одного потока в другой, используя переданный буфер.
		/// Начальное содержимое буфера игнорируется.
		/// </summary>
		/// <param name="input">Поток для чтения. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="output">Поток для записи. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="buffer">Буфер для переноса данных</param>
		/// <exception cref="ArgumentNullException">input is null</exception>
		/// <exception cref="ArgumentNullException">output is null</exception>
		/// <exception cref="ArgumentNullException">buffer is null</exception>
		/// <exception cref="ArgumentException">buffer is a zero-length array</exception>
		/// <exception cref="IOException">An error occurs while reading or writing</exception>
		public static void CopyFully(Stream input, Stream output, Byte[] buffer)
		{
			CopyFully(input, output, buffer, CancellationToken.None);
		}

		/// <summary>
		/// Копирует все данные из одного потока в другой, используя переданный буфер.
		/// Начальное содержимое буфера игнорируется.
		/// </summary>
		/// <param name="input">Поток для чтения. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="output">Поток для записи. Если поддерживает Seek, устанавливается в начало.</param>
		/// <param name="buffer">Буфер для переноса данных</param>
		/// <param name="cancellationToken">токен для прерывания операции.</param>
		/// <exception cref="ArgumentNullException">input is null</exception>
		/// <exception cref="ArgumentNullException">output is null</exception>
		/// <exception cref="ArgumentNullException">buffer is null</exception>
		/// <exception cref="ArgumentException">buffer is a zero-length array</exception>
		/// <exception cref="IOException">An error occurs while reading or writing</exception>
		public static void CopyFully(Stream input, Stream output, Byte[] buffer, CancellationToken cancellationToken)
		{
			if (buffer.Length == 0)
			{
				throw new ArgumentException("Buffer has length of 0");
			}

			if (input.CanSeek)
				input.Seek(0, SeekOrigin.Begin);
			if (output.CanSeek)
				output.Seek(0, SeekOrigin.Begin);

			int read;
			while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				if (cancellationToken != CancellationToken.None && cancellationToken.IsCancellationRequested)
					return;
				output.Write(buffer, 0, read);
			}
		}
	}
}