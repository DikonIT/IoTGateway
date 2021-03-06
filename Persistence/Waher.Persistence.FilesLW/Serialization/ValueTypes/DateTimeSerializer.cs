﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Waher.Persistence.Files.Serialization.ValueTypes
{
	public class DateTimeSerializer : ValueTypeSerializer
	{
		public DateTimeSerializer()
		{
		}

		public override Type ValueType
		{
			get
			{
				return typeof(DateTime);
			}
		}

		public override object Deserialize(BinaryDeserializer Reader, uint? DataType, bool Embedded)
		{
			if (!DataType.HasValue)
				DataType = Reader.ReadBits(6);

			switch (DataType.Value)
			{
				case ObjectSerializer.TYPE_DATETIME: return Reader.ReadDateTime();
				case ObjectSerializer.TYPE_STRING: return DateTime.Parse(Reader.ReadString());
				case ObjectSerializer.TYPE_MIN: return DateTime.MinValue;
				case ObjectSerializer.TYPE_MAX: return DateTime.MaxValue;
				case ObjectSerializer.TYPE_NULL: return null;
				default: throw new Exception("Expected a DateTime value.");
			}
		}

		public override void Serialize(BinarySerializer Writer, bool WriteTypeCode, bool Embedded, object Value)
		{
			if (WriteTypeCode)
				Writer.WriteBits(ObjectSerializer.TYPE_DATETIME, 6);

			Writer.Write((DateTime)Value);
		}

	}
}
