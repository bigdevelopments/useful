using System;
using System.Collections;
using System.Text;

namespace BigDevelopments.Useful
{
	/// <summary>
	/// 125 bit near-guid expressible like a serial key
	/// </summary>
	public class SerialCode : IEquatable<SerialCode>
	{
		// alphabet of 32 non-ambigious characters, apart from 2 and Z and B and 8 maybe
		private static string Alphabet = "FXB4SAJZL3GY5EMTH68PRKW9DNVQ2CU7";

		// guid and coded form
		private readonly Guid _guid;
		private readonly string _code;

		public SerialCode(SerialCode existing)
		{
			_guid = existing._guid;
			_code = existing._code;
		}

		private SerialCode(Guid guid)
		{
			_guid = guid;
			_code = GetCode(guid);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as SerialCode);
		}

		public bool Equals(Code other)
		{
			return _guid == other?._guid;
		}

		public override int GetHashCode()
		{
			return _guid.GetHashCode();
		}

		public override string ToString()
		{
			return _code;
		}

		public static SerialCode New()
		{
			byte[] code = Guid.NewGuid().ToByteArray();
			// clear top 3 bits, to give us 125 bits
			code[15] &= 31;
			return new SerialCode(new Guid(code));
		}

		public static SerialCode Parse(string code)
		{
			return new SerialCode(CodeToGuid(code));
		}

		public static bool operator==(SerialCode codeA, SerialCode codeB)
		{
			return codeA._guid == codeB._guid;
		}

		public static bool operator !=(SerialCode codeA, SerialCode codeB)
		{
			return codeA._guid != codeB._guid;
		}

		private static string GetCode(Guid guid)
		{
			BitArray bits = new BitArray(guid.ToByteArray());
			StringBuilder sb = new StringBuilder();

			int index = 0;
			while (index < 125)
			{
				int x = bits[index++] ? 1 : 0;
				x += bits[index++] ? 2 : 0;
				x += bits[index++] ? 4 : 0;
				x += bits[index++] ? 8 : 0;
				x += bits[index++] ? 16 : 0;
				sb.Append(Alphabet[x]);
				if (index % 25 == 0 && index < 125) sb.Append(" - ");
			}

			return sb.ToString();
		}

		private static Guid CodeToGuid(string code)
		{
			if (code == null) throw new ArgumentNullException(code);
			if (code.Length != 37) throw new ArgumentException("Invalid code", nameof(code));
			if (code.Substring(5, 3) != " - ") throw new ArgumentException("Invalid code", nameof(code));
			if (code.Substring(13, 3) != " - ") throw new ArgumentException("Invalid code", nameof(code));
			if (code.Substring(21, 3) != " - ") throw new ArgumentException("Invalid code", nameof(code));
			if (code.Substring(29, 3) != " - ") throw new ArgumentException("Invalid code", nameof(code));

			BitArray bits = new BitArray(128);
			int counter = 0;

			for (int index = 0; index < 37; index++)
			{
				if (index == 5 || index == 6 || index == 7
					|| index == 13 || index == 14 || index == 15
					|| index == 21 || index == 22 || index == 23
					|| index == 29 || index == 30 || index == 31) continue;

				int value = Alphabet.IndexOf(code[index]);

				if (value < 0 || value > 31) throw new ArgumentException("Invalid code", nameof(code));

				bits.Set(counter++, (value & 1) == 1);
				bits.Set(counter++, (value & 2) == 2);
				bits.Set(counter++, (value & 4) == 4);
				bits.Set(counter++, (value & 8) == 8);
				bits.Set(counter++, (value & 16) == 16);
			}

			byte[] data = new byte[16];
			bits.CopyTo(data, 0);
			return new Guid(data);
		}
	}
}
