using System;

namespace Polar.Utilities;

public class BigInteger
{
	private const int maxLength = 200;

	public static readonly int[] primesBelow2000 = new int[303]
	{
		2, 3, 5, 7, 11, 13, 17, 19, 23, 29,
		31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
		73, 79, 83, 89, 97, 101, 103, 107, 109, 113,
		127, 131, 137, 139, 149, 151, 157, 163, 167, 173,
		179, 181, 191, 193, 197, 199, 211, 223, 227, 229,
		233, 239, 241, 251, 257, 263, 269, 271, 277, 281,
		283, 293, 307, 311, 313, 317, 331, 337, 347, 349,
		353, 359, 367, 373, 379, 383, 389, 397, 401, 409,
		419, 421, 431, 433, 439, 443, 449, 457, 461, 463,
		467, 479, 487, 491, 499, 503, 509, 521, 523, 541,
		547, 557, 563, 569, 571, 577, 587, 593, 599, 601,
		607, 613, 617, 619, 631, 641, 643, 647, 653, 659,
		661, 673, 677, 683, 691, 701, 709, 719, 727, 733,
		739, 743, 751, 757, 761, 769, 773, 787, 797, 809,
		811, 821, 823, 827, 829, 839, 853, 857, 859, 863,
		877, 881, 883, 887, 907, 911, 919, 929, 937, 941,
		947, 953, 967, 971, 977, 983, 991, 997, 1009, 1013,
		1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063, 1069,
		1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151,
		1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213, 1217, 1223,
		1229, 1231, 1237, 1249, 1259, 1277, 1279, 1283, 1289, 1291,
		1297, 1301, 1303, 1307, 1319, 1321, 1327, 1361, 1367, 1373,
		1381, 1399, 1409, 1423, 1427, 1429, 1433, 1439, 1447, 1451,
		1453, 1459, 1471, 1481, 1483, 1487, 1489, 1493, 1499, 1511,
		1523, 1531, 1543, 1549, 1553, 1559, 1567, 1571, 1579, 1583,
		1597, 1601, 1607, 1609, 1613, 1619, 1621, 1627, 1637, 1657,
		1663, 1667, 1669, 1693, 1697, 1699, 1709, 1721, 1723, 1733,
		1741, 1747, 1753, 1759, 1777, 1783, 1787, 1789, 1801, 1811,
		1823, 1831, 1847, 1861, 1867, 1871, 1873, 1877, 1879, 1889,
		1901, 1907, 1913, 1931, 1933, 1949, 1951, 1973, 1979, 1987,
		1993, 1997, 1999
	};

	private readonly uint[] data = null;

	public int dataLength;

	public BigInteger()
	{
		data = new uint[200];
		dataLength = 1;
	}

	public BigInteger(long value)
	{
		data = new uint[200];
		long tempVal = value;
		dataLength = 0;
		while (value != 0L && dataLength < 200)
		{
			data[dataLength] = (uint)(value & 0xFFFFFFFFu);
			value >>= 32;
			dataLength++;
		}
		if (tempVal > 0)
		{
			if (value != 0L || (data[199] & 0x80000000u) != 0)
			{
				throw new ArithmeticException("Positive overflow in constructor.");
			}
		}
		else if (tempVal < 0 && (value != -1 || (data[dataLength - 1] & 0x80000000u) == 0))
		{
			throw new ArithmeticException("Negative underflow in constructor.");
		}
		if (dataLength == 0)
		{
			dataLength = 1;
		}
	}

	public BigInteger(ulong value)
	{
		data = new uint[200];
		dataLength = 0;
		while (value != 0L && dataLength < 200)
		{
			data[dataLength] = (uint)(value & 0xFFFFFFFFu);
			value >>= 32;
			dataLength++;
		}
		if (value != 0L || (data[199] & 0x80000000u) != 0)
		{
			throw new ArithmeticException("Positive overflow in constructor.");
		}
		if (dataLength == 0)
		{
			dataLength = 1;
		}
	}

	public BigInteger(BigInteger bi)
	{
		data = new uint[200];
		dataLength = bi.dataLength;
		for (int i = 0; i < dataLength; i++)
		{
			data[i] = bi.data[i];
		}
	}

	public BigInteger(string value, int radix)
	{
		BigInteger multiplier = new BigInteger(1L);
		BigInteger result = new BigInteger();
		value = value.ToUpper().Trim();
		int limit = 0;
		if (value[0] == '-')
		{
			limit = 1;
		}
		for (int i = value.Length - 1; i >= limit; i--)
		{
			int posVal = value[i];
			posVal = ((posVal >= 48 && posVal <= 57) ? (posVal - 48) : ((posVal < 65 || posVal > 90) ? 9999999 : (posVal - 65 + 10)));
			if (posVal >= radix)
			{
				throw new ArithmeticException("Invalid string in constructor.");
			}
			if (value[0] == '-')
			{
				posVal = -posVal;
			}
			result += multiplier * posVal;
			if (i - 1 >= limit)
			{
				multiplier *= (BigInteger)radix;
			}
		}
		if (value[0] == '-')
		{
			if ((result.data[199] & 0x80000000u) == 0)
			{
				throw new ArithmeticException("Negative underflow in constructor.");
			}
		}
		else if ((result.data[199] & 0x80000000u) != 0)
		{
			throw new ArithmeticException("Positive overflow in constructor.");
		}
		data = new uint[200];
		for (int i = 0; i < result.dataLength; i++)
		{
			data[i] = result.data[i];
		}
		dataLength = result.dataLength;
	}

	public BigInteger(byte[] inData)
	{
		dataLength = inData.Length >> 2;
		int leftOver = inData.Length & 3;
		if (leftOver != 0)
		{
			dataLength++;
		}
		if (dataLength > 200)
		{
			throw new ArithmeticException("Byte overflow in constructor.");
		}
		data = new uint[200];
		int i = inData.Length - 1;
		int j = 0;
		while (i >= 3)
		{
			data[j] = (uint)((inData[i - 3] << 24) + (inData[i - 2] << 16) + (inData[i - 1] << 8) + inData[i]);
			i -= 4;
			j++;
		}
		switch (leftOver)
		{
		case 1:
			data[dataLength - 1] = inData[0];
			break;
		case 2:
			data[dataLength - 1] = (uint)((inData[0] << 8) + inData[1]);
			break;
		case 3:
			data[dataLength - 1] = (uint)((inData[0] << 16) + (inData[1] << 8) + inData[2]);
			break;
		}
		while (dataLength > 1 && data[dataLength - 1] == 0)
		{
			dataLength--;
		}
	}

	public BigInteger(byte[] inData, int inLen)
	{
		dataLength = inLen >> 2;
		int leftOver = inLen & 3;
		if (leftOver != 0)
		{
			dataLength++;
		}
		if (dataLength > 200 || inLen > inData.Length)
		{
			throw new ArithmeticException("Byte overflow in constructor.");
		}
		data = new uint[200];
		int i = inLen - 1;
		int j = 0;
		while (i >= 3)
		{
			data[j] = (uint)((inData[i - 3] << 24) + (inData[i - 2] << 16) + (inData[i - 1] << 8) + inData[i]);
			i -= 4;
			j++;
		}
		switch (leftOver)
		{
		case 1:
			data[dataLength - 1] = inData[0];
			break;
		case 2:
			data[dataLength - 1] = (uint)((inData[0] << 8) + inData[1]);
			break;
		case 3:
			data[dataLength - 1] = (uint)((inData[0] << 16) + (inData[1] << 8) + inData[2]);
			break;
		}
		if (dataLength == 0)
		{
			dataLength = 1;
		}
		while (dataLength > 1 && data[dataLength - 1] == 0)
		{
			dataLength--;
		}
	}

	public BigInteger(uint[] inData)
	{
		dataLength = inData.Length;
		if (dataLength > 200)
		{
			throw new ArithmeticException("Byte overflow in constructor.");
		}
		data = new uint[200];
		int i = dataLength - 1;
		int j = 0;
		while (i >= 0)
		{
			data[j] = inData[i];
			i--;
			j++;
		}
		while (dataLength > 1 && data[dataLength - 1] == 0)
		{
			dataLength--;
		}
	}

	public static implicit operator BigInteger(long value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(ulong value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(int value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(uint value)
	{
		return new BigInteger((ulong)value);
	}

	public static BigInteger operator +(BigInteger bi1, BigInteger bi2)
	{
		BigInteger result = new BigInteger();
		result.dataLength = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
		long carry = 0L;
		for (int i = 0; i < result.dataLength; i++)
		{
			long sum = (long)bi1.data[i] + (long)bi2.data[i] + carry;
			carry = sum >> 32;
			result.data[i] = (uint)(sum & 0xFFFFFFFFu);
		}
		if (carry != 0L && result.dataLength < 200)
		{
			result.data[result.dataLength] = (uint)carry;
			result.dataLength++;
		}
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		int lastPos = 199;
		if ((bi1.data[lastPos] & 0x80000000u) == (bi2.data[lastPos] & 0x80000000u) && (result.data[lastPos] & 0x80000000u) != (bi1.data[lastPos] & 0x80000000u))
		{
			throw new ArithmeticException();
		}
		return result;
	}

	public static BigInteger operator ++(BigInteger bi1)
	{
		BigInteger result = new BigInteger(bi1);
		long carry = 1L;
		int index = 0;
		while (carry != 0L && index < 200)
		{
			long val = result.data[index];
			val++;
			result.data[index] = (uint)(val & 0xFFFFFFFFu);
			carry = val >> 32;
			index++;
		}
		if (index > result.dataLength)
		{
			result.dataLength = index;
		}
		else
		{
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
		}
		int lastPos = 199;
		if ((bi1.data[lastPos] & 0x80000000u) == 0 && (result.data[lastPos] & 0x80000000u) != (bi1.data[lastPos] & 0x80000000u))
		{
			throw new ArithmeticException("Overflow in ++.");
		}
		return result;
	}

	public static BigInteger operator -(BigInteger bi1, BigInteger bi2)
	{
		BigInteger result = new BigInteger();
		result.dataLength = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
		long carryIn = 0L;
		for (int i = 0; i < result.dataLength; i++)
		{
			long diff = (long)bi1.data[i] - (long)bi2.data[i] - carryIn;
			result.data[i] = (uint)(diff & 0xFFFFFFFFu);
			carryIn = ((diff >= 0) ? 0 : 1);
		}
		if (carryIn != 0)
		{
			for (int i = result.dataLength; i < 200; i++)
			{
				result.data[i] = uint.MaxValue;
			}
			result.dataLength = 200;
		}
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		int lastPos = 199;
		if ((bi1.data[lastPos] & 0x80000000u) != (bi2.data[lastPos] & 0x80000000u) && (result.data[lastPos] & 0x80000000u) != (bi1.data[lastPos] & 0x80000000u))
		{
			throw new ArithmeticException();
		}
		return result;
	}

	public static BigInteger operator --(BigInteger bi1)
	{
		BigInteger result = new BigInteger(bi1);
		bool carryIn = true;
		int index = 0;
		while (carryIn && index < 200)
		{
			long val = result.data[index];
			val--;
			result.data[index] = (uint)(val & 0xFFFFFFFFu);
			if (val >= 0)
			{
				carryIn = false;
			}
			index++;
		}
		if (index > result.dataLength)
		{
			result.dataLength = index;
		}
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		int lastPos = 199;
		if ((bi1.data[lastPos] & 0x80000000u) != 0 && (result.data[lastPos] & 0x80000000u) != (bi1.data[lastPos] & 0x80000000u))
		{
			throw new ArithmeticException("Underflow in --.");
		}
		return result;
	}

	public static BigInteger operator *(BigInteger bi1, BigInteger bi2)
	{
		int lastPos = 199;
		bool bi1Neg = false;
		bool bi2Neg = false;
		try
		{
			if ((bi1.data[lastPos] & 0x80000000u) != 0)
			{
				bi1Neg = true;
				bi1 = -bi1;
			}
			if ((bi2.data[lastPos] & 0x80000000u) != 0)
			{
				bi2Neg = true;
				bi2 = -bi2;
			}
		}
		catch (Exception)
		{
		}
		BigInteger result = new BigInteger();
		try
		{
			for (int i = 0; i < bi1.dataLength; i++)
			{
				if (bi1.data[i] != 0)
				{
					ulong mcarry = 0uL;
					int j = 0;
					int k = i;
					while (j < bi2.dataLength)
					{
						ulong val = (ulong)((long)bi1.data[i] * (long)bi2.data[j] + result.data[k]) + mcarry;
						result.data[k] = (uint)(val & 0xFFFFFFFFu);
						mcarry = val >> 32;
						j++;
						k++;
					}
					if (mcarry != 0)
					{
						result.data[i + bi2.dataLength] = (uint)mcarry;
					}
				}
			}
		}
		catch (Exception)
		{
			throw new ArithmeticException("Multiplication overflow.");
		}
		result.dataLength = bi1.dataLength + bi2.dataLength;
		if (result.dataLength > 200)
		{
			result.dataLength = 200;
		}
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		if ((result.data[lastPos] & 0x80000000u) != 0)
		{
			if (bi1Neg != bi2Neg && result.data[lastPos] == 2147483648u)
			{
				if (result.dataLength == 1)
				{
					return result;
				}
				bool isMaxNeg = true;
				for (int i = 0; i < result.dataLength - 1 && isMaxNeg; i++)
				{
					if (result.data[i] != 0)
					{
						isMaxNeg = false;
					}
				}
				if (isMaxNeg)
				{
					return result;
				}
			}
			throw new ArithmeticException("Multiplication overflow.");
		}
		if (bi1Neg != bi2Neg)
		{
			return -result;
		}
		return result;
	}

	public static BigInteger operator <<(BigInteger bi1, int shiftVal)
	{
		BigInteger result = new BigInteger(bi1);
		result.dataLength = shiftLeft(result.data, shiftVal);
		return result;
	}

	private static int shiftLeft(uint[] buffer, int shiftVal)
	{
		int shiftAmount = 32;
		int bufLen = buffer.Length;
		while (bufLen > 1 && buffer[bufLen - 1] == 0)
		{
			bufLen--;
		}
		for (int count = shiftVal; count > 0; count -= shiftAmount)
		{
			if (count < shiftAmount)
			{
				shiftAmount = count;
			}
			ulong carry = 0uL;
			for (int i = 0; i < bufLen; i++)
			{
				ulong val = (ulong)buffer[i] << shiftAmount;
				val |= carry;
				buffer[i] = (uint)(val & 0xFFFFFFFFu);
				carry = val >> 32;
			}
			if (carry != 0 && bufLen + 1 <= buffer.Length)
			{
				buffer[bufLen] = (uint)carry;
				bufLen++;
			}
		}
		return bufLen;
	}

	public static BigInteger operator >>(BigInteger bi1, int shiftVal)
	{
		BigInteger result = new BigInteger(bi1);
		result.dataLength = shiftRight(result.data, shiftVal);
		if ((bi1.data[199] & 0x80000000u) != 0)
		{
			for (int i = 199; i >= result.dataLength; i--)
			{
				result.data[i] = uint.MaxValue;
			}
			uint mask = 2147483648u;
			for (int i = 0; i < 32; i++)
			{
				if ((result.data[result.dataLength - 1] & mask) != 0)
				{
					break;
				}
				result.data[result.dataLength - 1] |= mask;
				mask >>= 1;
			}
			result.dataLength = 200;
		}
		return result;
	}

	private static int shiftRight(uint[] buffer, int shiftVal)
	{
		int shiftAmount = 32;
		int invShift = 0;
		int bufLen = buffer.Length;
		while (bufLen > 1 && buffer[bufLen - 1] == 0)
		{
			bufLen--;
		}
		for (int count = shiftVal; count > 0; count -= shiftAmount)
		{
			if (count < shiftAmount)
			{
				shiftAmount = count;
				invShift = 32 - shiftAmount;
			}
			ulong carry = 0uL;
			for (int i = bufLen - 1; i >= 0; i--)
			{
				ulong val = (ulong)buffer[i] >> shiftAmount;
				val |= carry;
				carry = (ulong)buffer[i] << invShift;
				buffer[i] = (uint)val;
			}
		}
		while (bufLen > 1 && buffer[bufLen - 1] == 0)
		{
			bufLen--;
		}
		return bufLen;
	}

	public static BigInteger operator ~(BigInteger bi1)
	{
		BigInteger result = new BigInteger(bi1);
		for (int i = 0; i < 200; i++)
		{
			result.data[i] = ~bi1.data[i];
		}
		result.dataLength = 200;
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		return result;
	}

	public static BigInteger operator -(BigInteger bi1)
	{
		if (bi1.dataLength == 1 && bi1.data[0] == 0)
		{
			return new BigInteger();
		}
		BigInteger result = new BigInteger(bi1);
		for (int i = 0; i < 200; i++)
		{
			result.data[i] = ~bi1.data[i];
		}
		long carry = 1L;
		int index = 0;
		while (carry != 0L && index < 200)
		{
			long val = result.data[index];
			val++;
			result.data[index] = (uint)(val & 0xFFFFFFFFu);
			carry = val >> 32;
			index++;
		}
		if ((bi1.data[199] & 0x80000000u) == (result.data[199] & 0x80000000u))
		{
			throw new ArithmeticException("Overflow in negation.\n");
		}
		result.dataLength = 200;
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		return result;
	}

	public static bool operator ==(BigInteger bi1, BigInteger bi2)
	{
		return bi1.Equals(bi2);
	}

	public static bool operator !=(BigInteger bi1, BigInteger bi2)
	{
		return !bi1.Equals(bi2);
	}

	public override bool Equals(object o)
	{
		BigInteger bi = (BigInteger)o;
		if (dataLength != bi.dataLength)
		{
			return false;
		}
		for (int i = 0; i < dataLength; i++)
		{
			if (data[i] != bi.data[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}

	public static bool operator >(BigInteger bi1, BigInteger bi2)
	{
		int pos = 199;
		if ((bi1.data[pos] & 0x80000000u) != 0 && (bi2.data[pos] & 0x80000000u) == 0)
		{
			return false;
		}
		if ((bi1.data[pos] & 0x80000000u) == 0 && (bi2.data[pos] & 0x80000000u) != 0)
		{
			return true;
		}
		int len = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
		pos = len - 1;
		while (pos >= 0 && bi1.data[pos] == bi2.data[pos])
		{
			pos--;
		}
		if (pos >= 0)
		{
			if (bi1.data[pos] > bi2.data[pos])
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public static bool operator <(BigInteger bi1, BigInteger bi2)
	{
		int pos = 199;
		if ((bi1.data[pos] & 0x80000000u) != 0 && (bi2.data[pos] & 0x80000000u) == 0)
		{
			return true;
		}
		if ((bi1.data[pos] & 0x80000000u) == 0 && (bi2.data[pos] & 0x80000000u) != 0)
		{
			return false;
		}
		int len = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
		pos = len - 1;
		while (pos >= 0 && bi1.data[pos] == bi2.data[pos])
		{
			pos--;
		}
		if (pos >= 0)
		{
			if (bi1.data[pos] < bi2.data[pos])
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public static bool operator >=(BigInteger bi1, BigInteger bi2)
	{
		return bi1 == bi2 || bi1 > bi2;
	}

	public static bool operator <=(BigInteger bi1, BigInteger bi2)
	{
		return bi1 == bi2 || bi1 < bi2;
	}

	private static void multiByteDivide(BigInteger bi1, BigInteger bi2, BigInteger outQuotient, BigInteger outRemainder)
	{
		uint[] result = new uint[200];
		int remainderLen = bi1.dataLength + 1;
		uint[] remainder = new uint[remainderLen];
		uint mask = 2147483648u;
		uint val = bi2.data[bi2.dataLength - 1];
		int shift = 0;
		int resultPos = 0;
		while (mask != 0 && (val & mask) == 0)
		{
			shift++;
			mask >>= 1;
		}
		for (int i = 0; i < bi1.dataLength; i++)
		{
			remainder[i] = bi1.data[i];
		}
		shiftLeft(remainder, shift);
		bi2 <<= shift;
		int j = remainderLen - bi2.dataLength;
		int pos = remainderLen - 1;
		ulong firstDivisorByte = bi2.data[bi2.dataLength - 1];
		ulong secondDivisorByte = bi2.data[bi2.dataLength - 2];
		int divisorLen = bi2.dataLength + 1;
		uint[] dividendPart = new uint[divisorLen];
		while (j > 0)
		{
			ulong dividend = ((ulong)remainder[pos] << 32) + remainder[pos - 1];
			ulong q_hat = dividend / firstDivisorByte;
			ulong r_hat = dividend % firstDivisorByte;
			bool done = false;
			while (!done)
			{
				done = true;
				if (q_hat == 4294967296L || q_hat * secondDivisorByte > (r_hat << 32) + remainder[pos - 2])
				{
					q_hat--;
					r_hat += firstDivisorByte;
					if (r_hat < 4294967296L)
					{
						done = false;
					}
				}
			}
			for (int h = 0; h < divisorLen; h++)
			{
				dividendPart[h] = remainder[pos - h];
			}
			BigInteger kk = new BigInteger(dividendPart);
			BigInteger ss;
			for (ss = bi2 * (long)q_hat; ss > kk; ss -= bi2)
			{
				q_hat--;
			}
			BigInteger yy = kk - ss;
			for (int h = 0; h < divisorLen; h++)
			{
				remainder[pos - h] = yy.data[bi2.dataLength - h];
			}
			result[resultPos++] = (uint)q_hat;
			pos--;
			j--;
		}
		outQuotient.dataLength = resultPos;
		int y = 0;
		int x = outQuotient.dataLength - 1;
		while (x >= 0)
		{
			outQuotient.data[y] = result[x];
			x--;
			y++;
		}
		for (; y < 200; y++)
		{
			outQuotient.data[y] = 0u;
		}
		while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0)
		{
			outQuotient.dataLength--;
		}
		if (outQuotient.dataLength == 0)
		{
			outQuotient.dataLength = 1;
		}
		outRemainder.dataLength = shiftRight(remainder, shift);
		for (y = 0; y < outRemainder.dataLength; y++)
		{
			outRemainder.data[y] = remainder[y];
		}
		for (; y < 200; y++)
		{
			outRemainder.data[y] = 0u;
		}
	}

	private static void singleByteDivide(BigInteger bi1, BigInteger bi2, BigInteger outQuotient, BigInteger outRemainder)
	{
		uint[] result = new uint[200];
		int resultPos = 0;
		int i;
		for (i = 0; i < 200; i++)
		{
			outRemainder.data[i] = bi1.data[i];
		}
		outRemainder.dataLength = bi1.dataLength;
		while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0)
		{
			outRemainder.dataLength--;
		}
		ulong divisor = bi2.data[0];
		int pos = outRemainder.dataLength - 1;
		ulong dividend = outRemainder.data[pos];
		if (dividend >= divisor)
		{
			ulong quotient = dividend / divisor;
			result[resultPos++] = (uint)quotient;
			outRemainder.data[pos] = (uint)(dividend % divisor);
		}
		pos--;
		while (pos >= 0)
		{
			dividend = ((ulong)outRemainder.data[pos + 1] << 32) + outRemainder.data[pos];
			ulong quotient = dividend / divisor;
			result[resultPos++] = (uint)quotient;
			outRemainder.data[pos + 1] = 0u;
			outRemainder.data[pos--] = (uint)(dividend % divisor);
		}
		outQuotient.dataLength = resultPos;
		int j = 0;
		i = outQuotient.dataLength - 1;
		while (i >= 0)
		{
			outQuotient.data[j] = result[i];
			i--;
			j++;
		}
		for (; j < 200; j++)
		{
			outQuotient.data[j] = 0u;
		}
		while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0)
		{
			outQuotient.dataLength--;
		}
		if (outQuotient.dataLength == 0)
		{
			outQuotient.dataLength = 1;
		}
		while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0)
		{
			outRemainder.dataLength--;
		}
	}

	public static BigInteger operator /(BigInteger bi1, BigInteger bi2)
	{
		BigInteger quotient = new BigInteger();
		BigInteger remainder = new BigInteger();
		int lastPos = 199;
		bool divisorNeg = false;
		bool dividendNeg = false;
		if ((bi1.data[lastPos] & 0x80000000u) != 0)
		{
			bi1 = -bi1;
			dividendNeg = true;
		}
		if ((bi2.data[lastPos] & 0x80000000u) != 0)
		{
			bi2 = -bi2;
			divisorNeg = true;
		}
		if (bi1 < bi2)
		{
			return quotient;
		}
		if (bi2.dataLength == 1)
		{
			singleByteDivide(bi1, bi2, quotient, remainder);
		}
		else
		{
			multiByteDivide(bi1, bi2, quotient, remainder);
		}
		if (dividendNeg != divisorNeg)
		{
			return -quotient;
		}
		return quotient;
	}

	public static BigInteger operator %(BigInteger bi1, BigInteger bi2)
	{
		BigInteger quotient = new BigInteger();
		BigInteger remainder = new BigInteger(bi1);
		int lastPos = 199;
		bool dividendNeg = false;
		if ((bi1.data[lastPos] & 0x80000000u) != 0)
		{
			bi1 = -bi1;
			dividendNeg = true;
		}
		if ((bi2.data[lastPos] & 0x80000000u) != 0)
		{
			bi2 = -bi2;
		}
		if (bi1 < bi2)
		{
			return remainder;
		}
		if (bi2.dataLength == 1)
		{
			singleByteDivide(bi1, bi2, quotient, remainder);
		}
		else
		{
			multiByteDivide(bi1, bi2, quotient, remainder);
		}
		if (dividendNeg)
		{
			return -remainder;
		}
		return remainder;
	}

	public static BigInteger operator &(BigInteger bi1, BigInteger bi2)
	{
		BigInteger result = new BigInteger();
		int len = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
		for (int i = 0; i < len; i++)
		{
			uint sum = bi1.data[i] & bi2.data[i];
			result.data[i] = sum;
		}
		result.dataLength = 200;
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		return result;
	}

	public static BigInteger operator |(BigInteger bi1, BigInteger bi2)
	{
		BigInteger result = new BigInteger();
		int len = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
		for (int i = 0; i < len; i++)
		{
			uint sum = bi1.data[i] | bi2.data[i];
			result.data[i] = sum;
		}
		result.dataLength = 200;
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		return result;
	}

	public static BigInteger operator ^(BigInteger bi1, BigInteger bi2)
	{
		BigInteger result = new BigInteger();
		int len = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
		for (int i = 0; i < len; i++)
		{
			uint sum = bi1.data[i] ^ bi2.data[i];
			result.data[i] = sum;
		}
		result.dataLength = 200;
		while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
		{
			result.dataLength--;
		}
		return result;
	}

	public BigInteger max(BigInteger bi)
	{
		if (this > bi)
		{
			return new BigInteger(this);
		}
		return new BigInteger(bi);
	}

	public BigInteger min(BigInteger bi)
	{
		if (this < bi)
		{
			return new BigInteger(this);
		}
		return new BigInteger(bi);
	}

	public BigInteger abs()
	{
		if ((data[199] & 0x80000000u) != 0)
		{
			return -this;
		}
		return new BigInteger(this);
	}

	public override string ToString()
	{
		return ToString(10);
	}

	public string ToString(int radix)
	{
		if (radix < 2 || radix > 36)
		{
			throw new ArgumentException("Radix must be >= 2 and <= 36");
		}
		string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		string result = "";
		BigInteger a = this;
		bool negative = false;
		if ((a.data[199] & 0x80000000u) != 0)
		{
			negative = true;
			try
			{
				a = -a;
			}
			catch (Exception)
			{
			}
		}
		BigInteger quotient = new BigInteger();
		BigInteger remainder = new BigInteger();
		BigInteger biRadix = new BigInteger(radix);
		if (a.dataLength == 1 && a.data[0] == 0)
		{
			result = "0";
		}
		else
		{
			while (a.dataLength > 1 || (a.dataLength == 1 && a.data[0] != 0))
			{
				singleByteDivide(a, biRadix, quotient, remainder);
				result = ((remainder.data[0] >= 10) ? (charSet[(int)(remainder.data[0] - 10)] + result) : (remainder.data[0] + result));
				a = quotient;
			}
			if (negative)
			{
				result = "-" + result;
			}
		}
		return result;
	}

	public string ToHexString()
	{
		string result = data[dataLength - 1].ToString("X");
		for (int i = dataLength - 2; i >= 0; i--)
		{
			result += data[i].ToString("X8");
		}
		return result;
	}

	public BigInteger Pow(BigInteger exp)
	{
		bool isneg = false;
		if (exp < 0)
		{
			exp = exp * -1 + 1;
			isneg = true;
		}
		BigInteger res = new BigInteger(1L);
		if (exp == 0)
		{
			return res;
		}
		BigInteger org = new BigInteger(this);
		BigInteger factor = new BigInteger(this);
		while (exp > 0)
		{
			if (exp % 2 == 1)
			{
				res *= factor;
			}
			exp /= (BigInteger)2;
			if (exp > 0)
			{
				factor *= factor;
			}
		}
		if (isneg)
		{
			Console.WriteLine("{0} / {1}", org, res);
			res = org / res;
			Console.WriteLine(res);
		}
		return res;
	}

	public BigInteger modPow(BigInteger exp, BigInteger n)
	{
		if ((exp.data[199] & 0x80000000u) != 0)
		{
			throw new ArithmeticException("Positive exponents only.");
		}
		BigInteger resultNum = 1;
		bool thisNegative = false;
		BigInteger tempNum;
		if ((data[199] & 0x80000000u) != 0)
		{
			tempNum = -this % n;
			thisNegative = true;
		}
		else
		{
			tempNum = this % n;
		}
		if ((n.data[199] & 0x80000000u) != 0)
		{
			n = -n;
		}
		BigInteger constant = new BigInteger();
		int i = n.dataLength << 1;
		constant.data[i] = 1u;
		constant.dataLength = i + 1;
		constant /= n;
		int totalBits = exp.bitCount();
		int count = 0;
		for (int pos = 0; pos < exp.dataLength; pos++)
		{
			uint mask = 1u;
			for (int index = 0; index < 32; index++)
			{
				if ((exp.data[pos] & mask) != 0)
				{
					resultNum = BarrettReduction(resultNum * tempNum, n, constant);
				}
				mask <<= 1;
				tempNum = BarrettReduction(tempNum * tempNum, n, constant);
				if (tempNum.dataLength == 1 && tempNum.data[0] == 1)
				{
					if (thisNegative && (exp.data[0] & (true ? 1u : 0u)) != 0)
					{
						return -resultNum;
					}
					return resultNum;
				}
				count++;
				if (count == totalBits)
				{
					break;
				}
			}
		}
		if (thisNegative && (exp.data[0] & (true ? 1u : 0u)) != 0)
		{
			return -resultNum;
		}
		return resultNum;
	}

	private BigInteger BarrettReduction(BigInteger x, BigInteger n, BigInteger constant)
	{
		int k = n.dataLength;
		int kPlusOne = k + 1;
		int kMinusOne = k - 1;
		BigInteger q1 = new BigInteger();
		int i = kMinusOne;
		int j = 0;
		while (i < x.dataLength)
		{
			q1.data[j] = x.data[i];
			i++;
			j++;
		}
		q1.dataLength = x.dataLength - kMinusOne;
		if (q1.dataLength <= 0)
		{
			q1.dataLength = 1;
		}
		BigInteger q2 = q1 * constant;
		BigInteger q3 = new BigInteger();
		i = kPlusOne;
		j = 0;
		while (i < q2.dataLength)
		{
			q3.data[j] = q2.data[i];
			i++;
			j++;
		}
		q3.dataLength = q2.dataLength - kPlusOne;
		if (q3.dataLength <= 0)
		{
			q3.dataLength = 1;
		}
		BigInteger r1 = new BigInteger();
		int lengthToCopy = ((x.dataLength > kPlusOne) ? kPlusOne : x.dataLength);
		for (i = 0; i < lengthToCopy; i++)
		{
			r1.data[i] = x.data[i];
		}
		r1.dataLength = lengthToCopy;
		BigInteger r2 = new BigInteger();
		for (i = 0; i < q3.dataLength; i++)
		{
			if (q3.data[i] != 0)
			{
				ulong mcarry = 0uL;
				int t = i;
				j = 0;
				while (j < n.dataLength && t < kPlusOne)
				{
					ulong val = (ulong)((long)q3.data[i] * (long)n.data[j] + r2.data[t]) + mcarry;
					r2.data[t] = (uint)(val & 0xFFFFFFFFu);
					mcarry = val >> 32;
					j++;
					t++;
				}
				if (t < kPlusOne)
				{
					r2.data[t] = (uint)mcarry;
				}
			}
		}
		r2.dataLength = kPlusOne;
		while (r2.dataLength > 1 && r2.data[r2.dataLength - 1] == 0)
		{
			r2.dataLength--;
		}
		r1 -= r2;
		if ((r1.data[199] & 0x80000000u) != 0)
		{
			BigInteger val = new BigInteger();
			val.data[kPlusOne] = 1u;
			val.dataLength = kPlusOne + 1;
			r1 += val;
		}
		for (; r1 >= n; r1 -= n)
		{
		}
		return r1;
	}

	public BigInteger gcd(BigInteger bi)
	{
		BigInteger x = (((data[199] & 0x80000000u) == 0) ? this : (-this));
		BigInteger y = (((bi.data[199] & 0x80000000u) == 0) ? bi : (-bi));
		BigInteger g = y;
		while (x.dataLength > 1 || (x.dataLength == 1 && x.data[0] != 0))
		{
			g = x;
			x = y % x;
			y = g;
		}
		return g;
	}

	public void genRandomBits(int bits, Random rand)
	{
		int dwords = bits >> 5;
		int remBits = bits & 0x1F;
		if (remBits != 0)
		{
			dwords++;
		}
		if (dwords > 200)
		{
			throw new ArithmeticException("Number of required bits > maxLength.");
		}
		for (int i = 0; i < dwords; i++)
		{
			data[i] = (uint)(rand.NextDouble() * 4294967296.0);
		}
		for (int i = dwords; i < 200; i++)
		{
			data[i] = 0u;
		}
		if (remBits != 0)
		{
			uint mask = (uint)(1 << remBits - 1);
			data[dwords - 1] |= mask;
			mask = uint.MaxValue >> 32 - remBits;
			data[dwords - 1] &= mask;
		}
		else
		{
			data[dwords - 1] |= 2147483648u;
		}
		dataLength = dwords;
		if (dataLength == 0)
		{
			dataLength = 1;
		}
	}

	public int bitCount()
	{
		while (dataLength > 1 && data[dataLength - 1] == 0)
		{
			dataLength--;
		}
		uint value = data[dataLength - 1];
		uint mask = 2147483648u;
		int bits = 32;
		while (bits > 0 && (value & mask) == 0)
		{
			bits--;
			mask >>= 1;
		}
		return bits + (dataLength - 1 << 5);
	}

	public bool FermatLittleTest(int confidence)
	{
		BigInteger thisVal = (((data[199] & 0x80000000u) == 0) ? this : (-this));
		if (thisVal.dataLength == 1)
		{
			if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
			{
				return false;
			}
			if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
			{
				return true;
			}
		}
		if ((thisVal.data[0] & 1) == 0)
		{
			return false;
		}
		int bits = thisVal.bitCount();
		BigInteger a = new BigInteger();
		BigInteger p_sub1 = thisVal - new BigInteger(1L);
		Random rand = new Random();
		for (int round = 0; round < confidence; round++)
		{
			bool done = false;
			while (!done)
			{
				int testBits;
				for (testBits = 0; testBits < 2; testBits = (int)(rand.NextDouble() * (double)bits))
				{
				}
				a.genRandomBits(testBits, rand);
				int byteLen = a.dataLength;
				if (byteLen > 1 || (byteLen == 1 && a.data[0] != 1))
				{
					done = true;
				}
			}
			BigInteger gcdTest = a.gcd(thisVal);
			if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
			{
				return false;
			}
			BigInteger expResult = a.modPow(p_sub1, thisVal);
			int resultLen = expResult.dataLength;
			if (resultLen > 1 || (resultLen == 1 && expResult.data[0] != 1))
			{
				return false;
			}
		}
		return true;
	}

	public bool RabinMillerTest(int confidence)
	{
		BigInteger thisVal = (((data[199] & 0x80000000u) == 0) ? this : (-this));
		if (thisVal.dataLength == 1)
		{
			if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
			{
				return false;
			}
			if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
			{
				return true;
			}
		}
		if ((thisVal.data[0] & 1) == 0)
		{
			return false;
		}
		BigInteger p_sub1 = thisVal - new BigInteger(1L);
		int s = 0;
		for (int index = 0; index < p_sub1.dataLength; index++)
		{
			uint mask = 1u;
			for (int i = 0; i < 32; i++)
			{
				if ((p_sub1.data[index] & mask) != 0)
				{
					index = p_sub1.dataLength;
					break;
				}
				mask <<= 1;
				s++;
			}
		}
		BigInteger t = p_sub1 >> s;
		int bits = thisVal.bitCount();
		BigInteger a = new BigInteger();
		Random rand = new Random();
		for (int round = 0; round < confidence; round++)
		{
			bool done = false;
			while (!done)
			{
				int testBits;
				for (testBits = 0; testBits < 2; testBits = (int)(rand.NextDouble() * (double)bits))
				{
				}
				a.genRandomBits(testBits, rand);
				int byteLen = a.dataLength;
				if (byteLen > 1 || (byteLen == 1 && a.data[0] != 1))
				{
					done = true;
				}
			}
			BigInteger gcdTest = a.gcd(thisVal);
			if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
			{
				return false;
			}
			BigInteger b = a.modPow(t, thisVal);
			bool result = false;
			if (b.dataLength == 1 && b.data[0] == 1)
			{
				result = true;
			}
			int j = 0;
			while (!result && j < s)
			{
				if (b == p_sub1)
				{
					result = true;
					break;
				}
				b = b * b % thisVal;
				j++;
			}
			if (!result)
			{
				return false;
			}
		}
		return true;
	}

	public bool SolovayStrassenTest(int confidence)
	{
		BigInteger thisVal = (((data[199] & 0x80000000u) == 0) ? this : (-this));
		if (thisVal.dataLength == 1)
		{
			if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
			{
				return false;
			}
			if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
			{
				return true;
			}
		}
		if ((thisVal.data[0] & 1) == 0)
		{
			return false;
		}
		int bits = thisVal.bitCount();
		BigInteger a = new BigInteger();
		BigInteger p_sub1 = thisVal - 1;
		BigInteger p_sub1_shift = p_sub1 >> 1;
		Random rand = new Random();
		for (int round = 0; round < confidence; round++)
		{
			bool done = false;
			while (!done)
			{
				int testBits;
				for (testBits = 0; testBits < 2; testBits = (int)(rand.NextDouble() * (double)bits))
				{
				}
				a.genRandomBits(testBits, rand);
				int byteLen = a.dataLength;
				if (byteLen > 1 || (byteLen == 1 && a.data[0] != 1))
				{
					done = true;
				}
			}
			BigInteger gcdTest = a.gcd(thisVal);
			if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
			{
				return false;
			}
			BigInteger expResult = a.modPow(p_sub1_shift, thisVal);
			if (expResult == p_sub1)
			{
				expResult = -1;
			}
			BigInteger jacob = Jacobi(a, thisVal);
			if (expResult != jacob)
			{
				return false;
			}
		}
		return true;
	}

	public bool LucasStrongTest()
	{
		BigInteger thisVal = (((data[199] & 0x80000000u) == 0) ? this : (-this));
		if (thisVal.dataLength == 1)
		{
			if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
			{
				return false;
			}
			if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
			{
				return true;
			}
		}
		if ((thisVal.data[0] & 1) == 0)
		{
			return false;
		}
		return LucasStrongTestHelper(thisVal);
	}

	private bool LucasStrongTestHelper(BigInteger thisVal)
	{
		long D = 5L;
		long sign = -1L;
		long dCount = 0L;
		for (bool done = false; !done; dCount++)
		{
			switch (Jacobi(D, thisVal))
			{
			case -1:
				done = true;
				continue;
			case 0:
				if (Math.Abs(D) < thisVal)
				{
					return false;
				}
				break;
			}
			if (dCount == 20)
			{
				BigInteger root = thisVal.sqrt();
				if (root * root == thisVal)
				{
					return false;
				}
			}
			D = (Math.Abs(D) + 2) * sign;
			sign = -sign;
		}
		long Q = 1 - D >> 2;
		BigInteger p_add1 = thisVal + 1;
		int s = 0;
		for (int index = 0; index < p_add1.dataLength; index++)
		{
			uint mask = 1u;
			for (int i = 0; i < 32; i++)
			{
				if ((p_add1.data[index] & mask) != 0)
				{
					index = p_add1.dataLength;
					break;
				}
				mask <<= 1;
				s++;
			}
		}
		BigInteger t = p_add1 >> s;
		BigInteger constant = new BigInteger();
		int nLen = thisVal.dataLength << 1;
		constant.data[nLen] = 1u;
		constant.dataLength = nLen + 1;
		constant /= thisVal;
		BigInteger[] lucas = LucasSequenceHelper(1, Q, t, thisVal, constant, 0);
		bool isPrime = false;
		if ((lucas[0].dataLength == 1 && lucas[0].data[0] == 0) || (lucas[1].dataLength == 1 && lucas[1].data[0] == 0))
		{
			isPrime = true;
		}
		for (int i = 1; i < s; i++)
		{
			if (!isPrime)
			{
				lucas[1] = thisVal.BarrettReduction(lucas[1] * lucas[1], thisVal, constant);
				lucas[1] = (lucas[1] - (lucas[2] << 1)) % thisVal;
				if (lucas[1].dataLength == 1 && lucas[1].data[0] == 0)
				{
					isPrime = true;
				}
			}
			lucas[2] = thisVal.BarrettReduction(lucas[2] * lucas[2], thisVal, constant);
		}
		if (isPrime)
		{
			BigInteger g = thisVal.gcd(Q);
			if (g.dataLength == 1 && g.data[0] == 1)
			{
				if ((lucas[2].data[199] & 0x80000000u) != 0)
				{
					BigInteger[] array = lucas;
					array[2] += thisVal;
				}
				BigInteger temp = Q * Jacobi(Q, thisVal) % thisVal;
				if ((temp.data[199] & 0x80000000u) != 0)
				{
					temp += thisVal;
				}
				if (lucas[2] != temp)
				{
					isPrime = false;
				}
			}
		}
		return isPrime;
	}

	public bool isProbablePrime(int confidence)
	{
		BigInteger thisVal = (((data[199] & 0x80000000u) == 0) ? this : (-this));
		for (int p = 0; p < primesBelow2000.Length; p++)
		{
			BigInteger divisor = primesBelow2000[p];
			if (divisor >= thisVal)
			{
				break;
			}
			BigInteger resultNum = thisVal % divisor;
			if (resultNum.IntValue() == 0)
			{
				return false;
			}
		}
		if (thisVal.RabinMillerTest(confidence))
		{
			return true;
		}
		return false;
	}

	public bool isProbablePrime()
	{
		BigInteger thisVal = (((data[199] & 0x80000000u) == 0) ? this : (-this));
		if (thisVal.dataLength == 1)
		{
			if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
			{
				return false;
			}
			if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
			{
				return true;
			}
		}
		if ((thisVal.data[0] & 1) == 0)
		{
			return false;
		}
		for (int p = 0; p < primesBelow2000.Length; p++)
		{
			BigInteger divisor = primesBelow2000[p];
			if (divisor >= thisVal)
			{
				break;
			}
			BigInteger resultNum = thisVal % divisor;
			if (resultNum.IntValue() == 0)
			{
				return false;
			}
		}
		BigInteger p_sub1 = thisVal - new BigInteger(1L);
		int s = 0;
		for (int index = 0; index < p_sub1.dataLength; index++)
		{
			uint mask = 1u;
			for (int i = 0; i < 32; i++)
			{
				if ((p_sub1.data[index] & mask) != 0)
				{
					index = p_sub1.dataLength;
					break;
				}
				mask <<= 1;
				s++;
			}
		}
		BigInteger t = p_sub1 >> s;
		int bits = thisVal.bitCount();
		BigInteger a = 2;
		BigInteger b = a.modPow(t, thisVal);
		bool result = false;
		if (b.dataLength == 1 && b.data[0] == 1)
		{
			result = true;
		}
		int j = 0;
		while (!result && j < s)
		{
			if (b == p_sub1)
			{
				result = true;
				break;
			}
			b = b * b % thisVal;
			j++;
		}
		if (result)
		{
			result = LucasStrongTestHelper(thisVal);
		}
		return result;
	}

	public int IntValue()
	{
		return (int)data[0];
	}

	public long LongValue()
	{
		long val = 0L;
		val = data[0];
		try
		{
			val |= (long)((ulong)data[1] << 32);
		}
		catch (Exception)
		{
			if ((data[0] & 0x80000000u) != 0)
			{
				val = (int)data[0];
			}
		}
		return val;
	}

	public static int Jacobi(BigInteger a, BigInteger b)
	{
		if ((b.data[0] & 1) == 0)
		{
			throw new ArgumentException("Jacobi defined only for odd integers.");
		}
		if (a >= b)
		{
			a %= b;
		}
		if (a.dataLength == 1 && a.data[0] == 0)
		{
			return 0;
		}
		if (a.dataLength == 1 && a.data[0] == 1)
		{
			return 1;
		}
		if (a < 0)
		{
			if (((b - 1).data[0] & 2) == 0)
			{
				return Jacobi(-a, b);
			}
			return -Jacobi(-a, b);
		}
		int e = 0;
		for (int index = 0; index < a.dataLength; index++)
		{
			uint mask = 1u;
			for (int i = 0; i < 32; i++)
			{
				if ((a.data[index] & mask) != 0)
				{
					index = a.dataLength;
					break;
				}
				mask <<= 1;
				e++;
			}
		}
		BigInteger a1 = a >> e;
		int s = 1;
		if (((uint)e & (true ? 1u : 0u)) != 0 && ((b.data[0] & 7) == 3 || (b.data[0] & 7) == 5))
		{
			s = -1;
		}
		if ((b.data[0] & 3) == 3 && (a1.data[0] & 3) == 3)
		{
			s = -s;
		}
		if (a1.dataLength == 1 && a1.data[0] == 1)
		{
			return s;
		}
		return s * Jacobi(b % a1, a1);
	}

	public static BigInteger genPseudoPrime(int bits, int confidence, Random rand)
	{
		BigInteger result = new BigInteger();
		bool done = false;
		while (!done)
		{
			result.genRandomBits(bits, rand);
			result.data[0] |= 1u;
			done = result.isProbablePrime(confidence);
		}
		return result;
	}

	public BigInteger genCoPrime(int bits, Random rand)
	{
		bool done = false;
		BigInteger result = new BigInteger();
		while (!done)
		{
			result.genRandomBits(bits, rand);
			BigInteger g = result.gcd(this);
			if (g.dataLength == 1 && g.data[0] == 1)
			{
				done = true;
			}
		}
		return result;
	}

	public BigInteger modInverse(BigInteger modulus)
	{
		BigInteger[] p = new BigInteger[2] { 0, 1 };
		BigInteger[] q = new BigInteger[2];
		BigInteger[] r = new BigInteger[2] { 0, 0 };
		int step = 0;
		BigInteger a = modulus;
		BigInteger b = this;
		while (b.dataLength > 1 || (b.dataLength == 1 && b.data[0] != 0))
		{
			BigInteger quotient = new BigInteger();
			BigInteger remainder = new BigInteger();
			if (step > 1)
			{
				BigInteger pval = (p[0] - p[1] * q[0]) % modulus;
				p[0] = p[1];
				p[1] = pval;
			}
			if (b.dataLength == 1)
			{
				singleByteDivide(a, b, quotient, remainder);
			}
			else
			{
				multiByteDivide(a, b, quotient, remainder);
			}
			q[0] = q[1];
			r[0] = r[1];
			q[1] = quotient;
			r[1] = remainder;
			a = b;
			b = remainder;
			step++;
		}
		if (r[0].dataLength > 1 || (r[0].dataLength == 1 && r[0].data[0] != 1))
		{
			throw new ArithmeticException("No inverse!");
		}
		BigInteger result = (p[0] - p[1] * q[0]) % modulus;
		if ((result.data[199] & 0x80000000u) != 0)
		{
			result += modulus;
		}
		return result;
	}

	public byte[] getBytes()
	{
		int numBits = bitCount();
		int numBytes = numBits >> 3;
		if (((uint)numBits & 7u) != 0)
		{
			numBytes++;
		}
		byte[] result = new byte[numBytes];
		int pos = 0;
		uint val = data[dataLength - 1];
		uint tempVal;
		if ((tempVal = (val >> 24) & 0xFFu) != 0)
		{
			result[pos++] = (byte)tempVal;
		}
		if ((tempVal = (val >> 16) & 0xFFu) != 0)
		{
			result[pos++] = (byte)tempVal;
		}
		if ((tempVal = (val >> 8) & 0xFFu) != 0)
		{
			result[pos++] = (byte)tempVal;
		}
		if ((tempVal = val & 0xFFu) != 0)
		{
			result[pos++] = (byte)tempVal;
		}
		int i = dataLength - 2;
		while (i >= 0)
		{
			val = data[i];
			result[pos + 3] = (byte)(val & 0xFFu);
			val >>= 8;
			result[pos + 2] = (byte)(val & 0xFFu);
			val >>= 8;
			result[pos + 1] = (byte)(val & 0xFFu);
			val >>= 8;
			result[pos] = (byte)(val & 0xFFu);
			i--;
			pos += 4;
		}
		return result;
	}

	public void setBit(uint bitNum)
	{
		uint bytePos = bitNum >> 5;
		byte bitPos = (byte)(bitNum & 0x1Fu);
		uint mask = (uint)(1 << (int)bitPos);
		data[bytePos] |= mask;
		if (bytePos >= dataLength)
		{
			dataLength = (int)(bytePos + 1);
		}
	}

	public void unsetBit(uint bitNum)
	{
		uint bytePos = bitNum >> 5;
		if (bytePos < dataLength)
		{
			byte bitPos = (byte)(bitNum & 0x1Fu);
			uint mask = (uint)(1 << (int)bitPos);
			uint mask2 = 0xFFFFFFFFu ^ mask;
			data[bytePos] &= mask2;
			if (dataLength > 1 && data[dataLength - 1] == 0)
			{
				dataLength--;
			}
		}
	}

	public BigInteger sqrt()
	{
		uint numBits = (uint)bitCount();
		numBits = (((numBits & 1) == 0) ? (numBits >> 1) : ((numBits >> 1) + 1));
		uint bytePos = numBits >> 5;
		byte bitPos = (byte)(numBits & 0x1Fu);
		BigInteger result = new BigInteger();
		uint mask;
		if (bitPos == 0)
		{
			mask = 2147483648u;
		}
		else
		{
			mask = (uint)(1 << (int)bitPos);
			bytePos++;
		}
		result.dataLength = (int)bytePos;
		for (int i = (int)(bytePos - 1); i >= 0; i--)
		{
			while (mask != 0)
			{
				result.data[i] ^= mask;
				if (result * result > this)
				{
					result.data[i] ^= mask;
				}
				mask >>= 1;
			}
			mask = 2147483648u;
		}
		return result;
	}

	public static BigInteger[] LucasSequence(BigInteger P, BigInteger Q, BigInteger k, BigInteger n)
	{
		if (k.dataLength == 1 && k.data[0] == 0)
		{
			return new BigInteger[3]
			{
				0,
				2 % n,
				1 % n
			};
		}
		BigInteger constant = new BigInteger();
		int nLen = n.dataLength << 1;
		constant.data[nLen] = 1u;
		constant.dataLength = nLen + 1;
		constant /= n;
		int s = 0;
		for (int index = 0; index < k.dataLength; index++)
		{
			uint mask = 1u;
			for (int i = 0; i < 32; i++)
			{
				if ((k.data[index] & mask) != 0)
				{
					index = k.dataLength;
					break;
				}
				mask <<= 1;
				s++;
			}
		}
		BigInteger t = k >> s;
		return LucasSequenceHelper(P, Q, t, n, constant, s);
	}

	private static BigInteger[] LucasSequenceHelper(BigInteger P, BigInteger Q, BigInteger k, BigInteger n, BigInteger constant, int s)
	{
		BigInteger[] result = new BigInteger[3];
		if ((k.data[0] & 1) == 0)
		{
			throw new ArgumentException("Argument k must be odd.");
		}
		int numbits = k.bitCount();
		uint mask = (uint)(1 << (numbits & 0x1F) - 1);
		BigInteger v = 2 % n;
		BigInteger Q_k = 1 % n;
		BigInteger v1 = P % n;
		BigInteger u1 = Q_k;
		bool flag = true;
		for (int i = k.dataLength - 1; i >= 0; i--)
		{
			while (mask != 0 && (i != 0 || mask != 1))
			{
				if ((k.data[i] & mask) != 0)
				{
					u1 = u1 * v1 % n;
					v = (v * v1 - P * Q_k) % n;
					v1 = n.BarrettReduction(v1 * v1, n, constant);
					v1 = (v1 - (Q_k * Q << 1)) % n;
					if (flag)
					{
						flag = false;
					}
					else
					{
						Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
					}
					Q_k = Q_k * Q % n;
				}
				else
				{
					u1 = (u1 * v - Q_k) % n;
					v1 = (v * v1 - P * Q_k) % n;
					v = n.BarrettReduction(v * v, n, constant);
					v = (v - (Q_k << 1)) % n;
					if (flag)
					{
						Q_k = Q % n;
						flag = false;
					}
					else
					{
						Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
					}
				}
				mask >>= 1;
			}
			mask = 2147483648u;
		}
		u1 = (u1 * v - Q_k) % n;
		v = (v * v1 - P * Q_k) % n;
		if (flag)
		{
			flag = false;
		}
		else
		{
			Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
		}
		Q_k = Q_k * Q % n;
		for (int i = 0; i < s; i++)
		{
			u1 = u1 * v % n;
			v = (v * v - (Q_k << 1)) % n;
			if (flag)
			{
				Q_k = Q % n;
				flag = false;
			}
			else
			{
				Q_k = n.BarrettReduction(Q_k * Q_k, n, constant);
			}
		}
		result[0] = u1;
		result[1] = v;
		result[2] = Q_k;
		return result;
	}

	public static void MulDivTest(int rounds)
	{
		Random rand = new Random();
		byte[] val = new byte[64];
		byte[] val2 = new byte[64];
		for (int count = 0; count < rounds; count++)
		{
			int t1;
			for (t1 = 0; t1 == 0; t1 = (int)(rand.NextDouble() * 65.0))
			{
			}
			int t2;
			for (t2 = 0; t2 == 0; t2 = (int)(rand.NextDouble() * 65.0))
			{
			}
			bool done = false;
			while (!done)
			{
				for (int i = 0; i < 64; i++)
				{
					if (i < t1)
					{
						val[i] = (byte)(rand.NextDouble() * 256.0);
					}
					else
					{
						val[i] = 0;
					}
					if (val[i] != 0)
					{
						done = true;
					}
				}
			}
			done = false;
			while (!done)
			{
				for (int i = 0; i < 64; i++)
				{
					if (i < t2)
					{
						val2[i] = (byte)(rand.NextDouble() * 256.0);
					}
					else
					{
						val2[i] = 0;
					}
					if (val2[i] != 0)
					{
						done = true;
					}
				}
			}
			while (val[0] == 0)
			{
				val[0] = (byte)(rand.NextDouble() * 256.0);
			}
			while (val2[0] == 0)
			{
				val2[0] = (byte)(rand.NextDouble() * 256.0);
			}
			BigInteger bn1 = new BigInteger(val, t1);
			BigInteger bn2 = new BigInteger(val2, t2);
			BigInteger bn3 = bn1 / bn2;
			BigInteger bn4 = bn1 % bn2;
			BigInteger bn5 = bn3 * bn2 + bn4;
			if (bn5 != bn1)
			{
				Console.WriteLine("Error at " + count);
				Console.WriteLine(bn1?.ToString() + "\n");
				Console.WriteLine(bn2?.ToString() + "\n");
				Console.WriteLine(bn3?.ToString() + "\n");
				Console.WriteLine(bn4?.ToString() + "\n");
				Console.WriteLine(bn5?.ToString() + "\n");
				break;
			}
		}
	}

	public static void RSATest(int rounds)
	{
		Random rand = new Random(1);
		byte[] val = new byte[64];
		BigInteger bi_e = new BigInteger("a932b948feed4fb2b692609bd22164fc9edb59fae7880cc1eaff7b3c9626b7e5b241c27a974833b2622ebe09beb451917663d47232488f23a117fc97720f1e7", 16);
		BigInteger bi_d = new BigInteger("4adf2f7a89da93248509347d2ae506d683dd3a16357e859a980c4f77a4e2f7a01fae289f13a851df6e9db5adaa60bfd2b162bbbe31f7c8f828261a6839311929d2cef4f864dde65e556ce43c89bbbf9f1ac5511315847ce9cc8dc92470a747b8792d6a83b0092d2e5ebaf852c85cacf34278efa99160f2f8aa7ee7214de07b7", 16);
		BigInteger bi_n = new BigInteger("e8e77781f36a7b3188d711c2190b560f205a52391b3479cdb99fa010745cbeba5f2adc08e1de6bf38398a0487c4a73610d94ec36f17f3f46ad75e17bc1adfec99839589f45f95ccc94cb2a5c500b477eb3323d8cfab0c8458c96f0147a45d27e45a4d11d54d77684f65d48f15fafcc1ba208e71e921b9bd9017c16a5231af7f", 16);
		Console.WriteLine("e =\n" + bi_e.ToString(10));
		Console.WriteLine("\nd =\n" + bi_d.ToString(10));
		Console.WriteLine("\nn =\n" + bi_n.ToString(10) + "\n");
		for (int count = 0; count < rounds; count++)
		{
			int t1;
			for (t1 = 0; t1 == 0; t1 = (int)(rand.NextDouble() * 65.0))
			{
			}
			bool done = false;
			while (!done)
			{
				for (int i = 0; i < 64; i++)
				{
					if (i < t1)
					{
						val[i] = (byte)(rand.NextDouble() * 256.0);
					}
					else
					{
						val[i] = 0;
					}
					if (val[i] != 0)
					{
						done = true;
					}
				}
			}
			while (val[0] == 0)
			{
				val[0] = (byte)(rand.NextDouble() * 256.0);
			}
			Console.Write("Round = " + count);
			BigInteger bi_data = new BigInteger(val, t1);
			BigInteger bi_encrypted = bi_data.modPow(bi_e, bi_n);
			BigInteger bi_decrypted = bi_encrypted.modPow(bi_d, bi_n);
			if (bi_decrypted != bi_data)
			{
				Console.WriteLine("\nError at round " + count);
				Console.WriteLine(bi_data?.ToString() + "\n");
				break;
			}
			Console.WriteLine(" <PASSED>.");
		}
	}

	public static void RSATest2(int rounds)
	{
		Random rand = new Random();
		byte[] val = new byte[64];
		byte[] pseudoPrime1 = new byte[64]
		{
			133, 132, 100, 253, 112, 106, 159, 240, 148, 12,
			62, 44, 116, 52, 5, 201, 85, 179, 133, 50,
			152, 113, 249, 65, 33, 95, 2, 158, 234, 86,
			141, 140, 68, 204, 238, 238, 61, 44, 157, 44,
			18, 65, 30, 241, 197, 50, 195, 170, 49, 74,
			82, 216, 232, 175, 66, 244, 114, 161, 42, 13,
			151, 177, 49, 179
		};
		byte[] pseudoPrime2 = new byte[64]
		{
			153, 152, 202, 184, 94, 215, 229, 220, 40, 92,
			111, 14, 21, 9, 89, 110, 132, 243, 129, 205,
			222, 66, 220, 147, 194, 122, 98, 172, 108, 175,
			222, 116, 227, 203, 96, 32, 56, 156, 33, 195,
			220, 200, 162, 77, 198, 42, 53, 127, 243, 169,
			232, 29, 123, 44, 120, 250, 184, 2, 85, 128,
			155, 194, 165, 203
		};
		BigInteger bi_p = new BigInteger(pseudoPrime1);
		BigInteger bi_q = new BigInteger(pseudoPrime2);
		BigInteger bi_pq = (bi_p - 1) * (bi_q - 1);
		BigInteger bi_n = bi_p * bi_q;
		for (int count = 0; count < rounds; count++)
		{
			BigInteger bi_e = bi_pq.genCoPrime(512, rand);
			BigInteger bi_d = bi_e.modInverse(bi_pq);
			Console.WriteLine("\ne =\n" + bi_e.ToString(10));
			Console.WriteLine("\nd =\n" + bi_d.ToString(10));
			Console.WriteLine("\nn =\n" + bi_n.ToString(10) + "\n");
			int t1;
			for (t1 = 0; t1 == 0; t1 = (int)(rand.NextDouble() * 65.0))
			{
			}
			bool done = false;
			while (!done)
			{
				for (int i = 0; i < 64; i++)
				{
					if (i < t1)
					{
						val[i] = (byte)(rand.NextDouble() * 256.0);
					}
					else
					{
						val[i] = 0;
					}
					if (val[i] != 0)
					{
						done = true;
					}
				}
			}
			while (val[0] == 0)
			{
				val[0] = (byte)(rand.NextDouble() * 256.0);
			}
			Console.Write("Round = " + count);
			BigInteger bi_data = new BigInteger(val, t1);
			BigInteger bi_encrypted = bi_data.modPow(bi_e, bi_n);
			BigInteger bi_decrypted = bi_encrypted.modPow(bi_d, bi_n);
			if (bi_decrypted != bi_data)
			{
				Console.WriteLine("\nError at round " + count);
				Console.WriteLine(bi_data?.ToString() + "\n");
				break;
			}
			Console.WriteLine(" <PASSED>.");
		}
	}

	public static void SqrtTest(int rounds)
	{
		Random rand = new Random();
		for (int count = 0; count < rounds; count++)
		{
			int t1;
			for (t1 = 0; t1 == 0; t1 = (int)(rand.NextDouble() * 1024.0))
			{
			}
			Console.Write("Round = " + count);
			BigInteger a = new BigInteger();
			a.genRandomBits(t1, rand);
			BigInteger b = a.sqrt();
			BigInteger c = (b + 1) * (b + 1);
			if (c <= a)
			{
				Console.WriteLine("\nError at round " + count);
				Console.WriteLine(a?.ToString() + "\n");
				break;
			}
			Console.WriteLine(" <PASSED>.");
		}
	}
}
