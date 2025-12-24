using System;
using System.IO;

namespace SMDB.Utils
{
    public static class ChecksumUtil
    {
        public static int ComputeSumChecksum(FileStream fs, long lengthToRead)
        {
            fs.Seek(0, SeekOrigin.Begin);

            int sum = 0;
            long read = 0;

            while (read < lengthToRead)
            {
                int b = fs.ReadByte();
                if (b == -1) break;

                sum += b; // 0..255
                read++;
            }

            return sum;
        }

        // Гарантира, че файлът завършва с 4 байта checksum (int32).
        // Ако файлът е нов или няма checksum -> добавя.
        // Ако вече има -> презаписва последните 4 байта.
        public static void WriteOrUpdateChecksum(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            long len = fs.Length;

            // Няма checksum поле (файл < 4 байта) -> добавяме checksum накрая
            if (len < 4)
            {
                int sum = ComputeSumChecksum(fs, len);
                fs.Seek(len, SeekOrigin.Begin);
                WriteInt32(fs, sum);
                return;
            }

            // Приемаме, че последните 4 байта са checksum поле
            long dataLen = len - 4;

            int newSum = ComputeSumChecksum(fs, dataLen);
            fs.Seek(dataLen, SeekOrigin.Begin);
            WriteInt32(fs, newSum);
        }

        public static bool ValidateChecksum(string path)
        {
            if (!File.Exists(path)) return false;

            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            long len = fs.Length;
            if (len < 4) return false;

            long dataLen = len - 4;

            int computed = ComputeSumChecksum(fs, dataLen);

            fs.Seek(dataLen, SeekOrigin.Begin);
            int stored = ReadInt32(fs);

            return stored == computed;
        }

        private static void WriteInt32(FileStream fs, int value)
        {
            fs.WriteByte((byte)(value & 0xFF));
            fs.WriteByte((byte)((value >> 8) & 0xFF));
            fs.WriteByte((byte)((value >> 16) & 0xFF));
            fs.WriteByte((byte)((value >> 24) & 0xFF));
        }

        private static int ReadInt32(FileStream fs)
        {
            int b0 = fs.ReadByte();
            int b1 = fs.ReadByte();
            int b2 = fs.ReadByte();
            int b3 = fs.ReadByte();

            if (b3 == -1) return 0;

            return (b0 & 0xFF)
                 | ((b1 & 0xFF) << 8)
                 | ((b2 & 0xFF) << 16)
                 | ((b3 & 0xFF) << 24);
        }
    }
}
