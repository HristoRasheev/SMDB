using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SMDB.Core.Models
{
    public struct Column
    {
        public string Name;
        public string Type;
        public int Size;
        public string DefaultValue;
    }

    public struct Cond
    {
        public string Col;
        public string Op;
        public string ValStr;
        public bool Not;
    }

    public class Table
    {
        public string Name { get; }

        public Table(string name)
        {
            Name = name;
        }

        private string GetStorageDir()
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "Storage");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private string GetMetaPath()
        {
            return Path.Combine(GetStorageDir(), Name + ".meta");
        }

        private string GetDataPath()
        {
            return Path.Combine(GetStorageDir(), Name + ".tbl");
        }

        private int FindColumnIndex(Column[] columns, string name)
        {
            for (int i = 0; i < columns.Length; i++)
                if (columns[i].Name == name)
                    return i;
            return -1;
        }

        private int FindColumnIndex(string[] insertColumns, string target)
        {
            for (int i = 0; i < insertColumns.Length; i++)
                if (insertColumns[i] == target)
                    return i;
            return -1;
        }

        private bool IsDeletedRow(int row, int[] freeSlots, int freeCount)
        {
            for (int i = 0; i < freeCount; i++)
                if (freeSlots[i] == row) return true;
            return false;
        }
        private void WriteFixedString(BinaryWriter bw, string text, int maxSize)
        {
            if (text == null) text = "";
            if (text.Length > maxSize) throw new Exception("String too long");

            for (int i = 0; i < text.Length; i++)
                bw.Write((byte)text[i]);

            for (int i = text.Length; i < maxSize; i++)
                bw.Write((byte)0);
        }

        private string ReadFixedString(BinaryReader br, int size)
        {
            char[] buf = new char[size];
            int len = 0;

            for (int i = 0; i < size; i++)
            {
                byte b = br.ReadByte();
                if (b != 0) buf[len++] = (char)b;
            }
            return new string(buf, 0, len);
        }

        private int ParseIntOrError(string s)
        {
            if (s == null || s.Length == 0) return int.MinValue;

            int sign = 1;
            int i = 0;

            if (s[0] == '-')
            {
                sign = -1;
                i = 1;
                if (i >= s.Length) return int.MinValue;
            }

            int value = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (c < '0' || c > '9') return int.MinValue;
                value = value * 10 + (c - '0');
                i++;
            }

            return sign * value;
        }

        private int ParseDateToInt(string s)
        {
            if (s == null) return -1;
            if (s.Length != 10) return -1;
            if (s[2] != '.' || s[5] != '.') return -1;

            int d1 = s[0] - '0', d2 = s[1] - '0';
            int m1 = s[3] - '0', m2 = s[4] - '0';
            int y1 = s[6] - '0', y2 = s[7] - '0', y3 = s[8] - '0', y4 = s[9] - '0';

            if (d1 < 0 || d1 > 9 || d2 < 0 || d2 > 9) return -1;
            if (m1 < 0 || m1 > 9 || m2 < 0 || m2 > 9) return -1;
            if (y1 < 0 || y1 > 9 || y2 < 0 || y2 > 9 || y3 < 0 || y3 > 9 || y4 < 0 || y4 > 9) return -1;

            int dd = d1 * 10 + d2;
            int mm = m1 * 10 + m2;
            int yyyy = y1 * 1000 + y2 * 100 + y3 * 10 + y4;

            if (mm < 1 || mm > 12) return -1;
            if (dd < 1 || dd > 31) return -1;

            return yyyy * 10000 + mm * 100 + dd;
        }

        private int GetRowSize(Column[] cols)
        {
            int s = 0;
            for (int i = 0; i < cols.Length; i++)
            {
                string t = cols[i].Type;
                if (t == "INT" || t == "DATE") s += 4;
                else if (t == "STRING")
                {
                    if (cols[i].Size <= 0) throw new Exception("STRING needs Size");
                    s += cols[i].Size;
                }
                else throw new Exception("Unknown type: " + cols[i].Type);
            }
            return s;
        }

        private void ReadMeta(out int rowCount, out Column[] columns,
                                out int freeCount, out int[] freeSlots, out int checksum)
        {
            string metaFile = GetMetaPath();
            if (!File.Exists(metaFile)) throw new Exception("Meta file missing.");

            using (BinaryReader r = new BinaryReader(File.Open(metaFile, FileMode.Open, FileAccess.Read)))
            {
                rowCount = r.ReadInt32();
                int columnCount = r.ReadInt32();

                columns = new Column[columnCount];
                for (int i = 0; i < columnCount; i++)
                {
                    columns[i].Name = r.ReadString();
                    columns[i].Type = r.ReadString();
                    columns[i].Size = r.ReadInt32();
                    columns[i].DefaultValue = r.ReadString();
                }

                freeCount = 0;
                freeSlots = new int[0];

                if (r.BaseStream.Position < r.BaseStream.Length)
                {
                    freeCount = r.ReadInt32();
                    if (freeCount > 0)
                    {
                        freeSlots = new int[freeCount];
                        for (int i = 0; i < freeCount; i++)
                            freeSlots[i] = r.ReadInt32();
                    }
                }

                checksum = 0;
                if (r.BaseStream.Position < r.BaseStream.Length)
                    checksum = r.ReadInt32();
            }
        }

        private void WriteMeta(int rowCount, Column[] columns, int freeCount, int[] freeSlots, int checksum)
        {
            string metaFile = GetMetaPath();

            using (BinaryWriter w = new BinaryWriter(File.Open(metaFile, FileMode.Create, FileAccess.Write)))
            {
                w.Write(rowCount);
                w.Write(columns.Length);

                for (int i = 0; i < columns.Length; i++)
                {
                    w.Write(columns[i].Name);
                    w.Write(columns[i].Type);
                    w.Write(columns[i].Size);
                    w.Write(columns[i].DefaultValue ?? "NULL");
                }

                w.Write(freeCount);
                for (int i = 0; i < freeCount; i++)
                    w.Write(freeSlots[i]);

                w.Write(checksum);
            }
        }

        private long GetDataStart()
        {
            string tableFile = GetDataPath();
            using (BinaryReader r = new BinaryReader(File.Open(tableFile, FileMode.Open, FileAccess.Read)))
            {
                r.ReadString(); // header
                return r.BaseStream.Position;
            }
        }

        private bool EvalWhere(Cond[] conds, string[] links, int condCount, int[] rowNums, Column[] columns)
        {
            if (condCount == 0) return true;
            bool result = false;
            bool hasResult = false;

            for (int i = 0; i < condCount; i++)
            {
                int colIndex = FindColumnIndex(columns, conds[i].Col);
                if (colIndex == -1) return false;

                string t = columns[colIndex].Type;
                if (t != "INT" && t != "DATE") return false; // WHERE int/date

                int right;
                if (t == "INT")
                {
                    right = ParseIntOrError(conds[i].ValStr);
                    if (right == int.MinValue) return false;
                }
                else // DATE
                {
                    right = ParseDateToInt(conds[i].ValStr);
                    if (right == -1) return false;
                }

                char opChar = conds[i].Op[0];

                bool ok =
                    (opChar == '>' && rowNums[colIndex] > right) ||
                    (opChar == '<' && rowNums[colIndex] < right) ||
                    (opChar == '=' && rowNums[colIndex] == right);

                if (conds[i].Not) ok = !ok;

                if (!hasResult)
                {
                    result = ok;
                    hasResult = true;
                }
                else
                {
                    if (links[i - 1] == "AND") result = result && ok;
                    else result = result || ok;
                }
            }
            return result;
        }

        public void Create(Column[] columns)
        {
            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (File.Exists(tableFile) || File.Exists(metaFile))
            {
                Console.WriteLine($"The table '{Name}' already exists!");
                return;
            }

            for (int i = 0; i < columns.Length; i++)
            {
                string t = columns[i].Type;
                if (t == "STRING" && columns[i].Size <= 0)
                {
                    Console.WriteLine($"Column '{columns[i].Name}' STRING needs Size (STRING(M)).");
                    return;
                }

                if (columns[i].DefaultValue == null) columns[i].DefaultValue = "NULL";
            }

            //tbl
            using (BinaryWriter writer = new BinaryWriter(File.Open(tableFile, FileMode.CreateNew)))
            {
                writer.Write(Name);
            }

            //meta
            int checksum = CalcDataChecksum(tableFile, GetDataStart());
            WriteMeta(0, columns, 0, new int[0], checksum);

            Console.WriteLine($"Table '{Name}' is created successfully!");
        }

        public void Drop()
        {
            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            bool deleted = false;

            if (File.Exists(tableFile)) { File.Delete(tableFile); deleted = true; }
            if (File.Exists(metaFile)) { File.Delete(metaFile); deleted = true; }

            if (deleted == false)
            {
                Console.WriteLine($"Table '{Name}' doesn't exist!");
                return;
            }

            Console.WriteLine($"Table '{Name}' was deleted successfully!");
        }

        public void PrintInfo()
        {
            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
            {
                Console.WriteLine($"Table '{Name}' does not exist!");
                return;
            }

            ReadMeta(out int rowCount, out Column[] columns, out int freeCount, out _, out _);

            long tableSize = new FileInfo(tableFile).Length;
            long metaSize = new FileInfo(metaFile).Length;

            
            Console.WriteLine($"Table: {Name}");
            Console.WriteLine();
            Console.WriteLine("Columns:");
            for (int i = 0; i < columns.Length; i++)
            {
                string t = columns[i].Type;
                if (t == "STRING") Console.WriteLine($"  - {columns[i].Name} : STRING({columns[i].Size})");
                else Console.WriteLine($"  - {columns[i].Name} : {t}");
            }
            Console.WriteLine();

            int activeRows = rowCount - freeCount;
            Console.WriteLine($"Rows: {activeRows}");
            Console.WriteLine($"Data file size (.tbl):  {tableSize} bytes");
            Console.WriteLine($"Meta file size (.meta): {metaSize} bytes");
            Console.WriteLine($"Total size:             {tableSize + metaSize} bytes");
        }

        public void Insert(string[] insertColumns, string[] values)
        {
            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
            {
                Console.WriteLine($"Table '{Name}' does not exist!");
                return;
            }

            ReadMeta(out int rowCount, out Column[] columns, out int freeCount, out int[] freeSlots, out _);

            if (values.Length != insertColumns.Length)
            {
                Console.WriteLine("Number of values does not match number of columns in INSERT.");
                return;
            }

            string[] finalValues = new string[columns.Length];

            for (int i = 0; i < columns.Length; i++)
            {
                int idx = FindColumnIndex(insertColumns, columns[i].Name);
                if (idx != -1) finalValues[i] = values[idx];
                else
                {
                    string def = columns[i].DefaultValue ?? "NULL";
                    if (def == "") finalValues[i] = def;
                    else
                    {
                        Console.WriteLine($"Missing value for column '{columns[i].Name}' and no default is defined.");
                        return;
                    }
                }
            }

            int[] intVals = new int[columns.Length];       // INT/DATE
            string[] strVals = new string[columns.Length]; // STRING

            for (int i = 0; i < columns.Length; i++)
            {
                string t = columns[i].Type;
                string v = finalValues[i];

                if (t == "INT")
                {
                    int x = ParseIntOrError(v);
                    if (x == int.MinValue)
                    {
                        Console.WriteLine($"Value '{v}' is not a valid INT for column '{columns[i].Name}'.");
                        return;
                    }
                    intVals[i] = x;
                }
                else if (t == "DATE")
                {
                    int d = ParseDateToInt(v);
                    if (d == -1)
                    {
                        Console.WriteLine($"Value '{v}' is not a valid DATE (dd.MM.yyyy) for column '{columns[i].Name}'.");
                        return;
                    }
                    intVals[i] = d;
                }
                else if (t == "STRING")
                {
                    string s = v;
                    if (columns[i].Size <= 0)
                    {
                        Console.WriteLine($"STRING size missing for column '{columns[i].Name}'.");
                        return;
                    }
                    if (s.Length > columns[i].Size)
                    {
                        Console.WriteLine($"Value '{s}' is too long for STRING({columns[i].Size}) column '{columns[i].Name}'.");
                        return;
                    }
                    strVals[i] = s;
                }
                else
                {
                    Console.WriteLine($"Unknown column type '{columns[i].Type}' for column '{columns[i].Name}'.");
                    return;
                }
            }

            int rowSize = GetRowSize(columns);
            long dataStart = GetDataStart();

            int targetRow;
            if (freeCount > 0)
            {
                targetRow = freeSlots[freeCount - 1];
                freeCount--;
            }
            else
            {
                targetRow = rowCount + 1;
                rowCount++;
            }

            using (BinaryWriter w = new BinaryWriter(File.Open(tableFile, FileMode.Open, FileAccess.Write)))
            {
                long offset = dataStart + (long)(targetRow - 1) * rowSize;
                w.BaseStream.Seek(offset, SeekOrigin.Begin);

                for (int i = 0; i < columns.Length; i++)
                {
                    string t = columns[i].Type;
                    if (t == "INT" || t == "DATE") w.Write(intVals[i]);
                    else if (t == "STRING") WriteFixedString(w, strVals[i], columns[i].Size);
                }
            }

            int checksum = CalcDataChecksum(tableFile, GetDataStart());
            WriteMeta(rowCount, columns, freeCount, freeSlots, checksum);

            Console.WriteLine("1 row inserted.");
        }

        public void GetRows(int[] rowNumbers)
        {
            if (rowNumbers == null || rowNumbers.Length == 0)
            {
                Console.WriteLine("No row numbers given.");
                return;
            }

            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
            {
                Console.WriteLine($"Table '{Name}' does not exist!");
                return;
            }

            ReadMeta(out int rowCount, out Column[] columns, out int freeCount, out int[] freeSlots, out _);

            int validCount = 0;
            for (int i = 0; i < rowNumbers.Length; i++)
            {
                int r = rowNumbers[i];
                if (r > 0 && r <= rowCount && !IsDeletedRow(r, freeSlots, freeCount)) validCount++;
            }

            if (validCount == 0)
            {
                Console.WriteLine("No valid row numbers (out of range or deleted).");
                return;
            }

            int[] requested = new int[validCount];
            int p = 0;
            for (int i = 0; i < rowNumbers.Length; i++)
            {
                int r = rowNumbers[i];
                if (r > 0 && r <= rowCount && !IsDeletedRow(r, freeSlots, freeCount))
                    requested[p++] = r;
            }

            for (int i = 0; i < requested.Length - 1; i++)
                for (int j = i + 1; j < requested.Length; j++)
                    if (requested[j] < requested[i])
                    {
                        int tmp = requested[i];
                        requested[i] = requested[j];
                        requested[j] = tmp;
                    }

            int rowSize = GetRowSize(columns);
            long dataStart = GetDataStart();

            for (int i = 0; i < columns.Length; i++)
                Console.Write(columns[i].Name + "\t");
            Console.WriteLine();

            using (BinaryReader r = new BinaryReader(File.Open(tableFile, FileMode.Open, FileAccess.Read)))
            {
                r.ReadString(); // header

                for (int k = 0; k < requested.Length; k++)
                {
                    int row = requested[k];
                    long offset = dataStart + (long)(row - 1) * rowSize;
                    r.BaseStream.Seek(offset, SeekOrigin.Begin);

                    for (int c = 0; c < columns.Length; c++)
                    {
                        string t = columns[c].Type;
                        if (t == "INT" || t == "DATE")
                            Console.Write(r.ReadInt32() + "\t");
                        else if (t == "STRING")
                            Console.Write(ReadFixedString(r, columns[c].Size) + "\t");
                    }
                    Console.WriteLine();
                }
            }
        }

        public void DeleteRows(int[] rowNumbers)
        {
            if (rowNumbers == null || rowNumbers.Length == 0)
            {
                Console.WriteLine("No rows specified to delete.");
                return;
            }

            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
            {
                Console.WriteLine($"Table '{Name}' does not exist!");
                return;
            }

            ReadMeta(out int rowCount, out Column[] columns, out int freeCount, out int[] freeSlots, out _);

            List<int> freeList = new List<int>();
            for (int i = 0; i < freeCount; i++) freeList.Add(freeSlots[i]);

            List<int> toDelete = new List<int>();

            for (int i = 0; i < rowNumbers.Length; i++)
            {
                int r = rowNumbers[i];
                if (r <= 0 || r > rowCount) continue;

                bool already = false;
                for (int j = 0; j < freeList.Count; j++)
                    if (freeList[j] == r) { already = true; break; }

                if (!already)
                {
                    freeList.Add(r);
                    toDelete.Add(r);
                }
            }

            if (toDelete.Count == 0)
            {
                Console.WriteLine("No rows deleted (either out of range or already deleted).");
                return;
            }

            int rowSize = GetRowSize(columns);
            long dataStart = GetDataStart();

            using (BinaryWriter w = new BinaryWriter(File.Open(tableFile, FileMode.Open, FileAccess.Write)))
            {
                for (int k = 0; k < toDelete.Count; k++)
                {
                    int row = toDelete[k];
                    long offset = dataStart + (long)(row - 1) * rowSize;
                    w.BaseStream.Seek(offset, SeekOrigin.Begin);

                    for (int b = 0; b < rowSize; b++)
                        w.Write((byte)0);
                }
            }

            int newFreeCount = freeList.Count;
            int[] newFreeSlots = new int[newFreeCount];
            for (int i = 0; i < newFreeCount; i++)
            {
                newFreeSlots[i] = freeList[i];
            }

            int checksum = CalcDataChecksum(tableFile, GetDataStart());
            WriteMeta(rowCount, columns, newFreeCount, newFreeSlots, checksum);

            Console.WriteLine($"{toDelete.Count} row(s) deleted.");
        }

        public int[] DeleteRowsWhere(Cond[] conds, string[] links, int condCount)
        {
            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
                return new int[0];

            ReadMeta(out int rowCount, out Column[] columns, out int freeCount, out int[] freeSlots, out _);

            int rowSize = GetRowSize(columns);
            long dataStart = GetDataStart();

            List<int> matches = new List<int>();

            using (BinaryReader r = new BinaryReader(File.Open(tableFile, FileMode.Open, FileAccess.Read)))
            {
                r.ReadString();
                for (int row = 1; row <= rowCount; row++)
                {
                    if (IsDeletedRow(row, freeSlots, freeCount))
                        continue;

                    long offset = dataStart + (long)(row - 1) * rowSize;
                    r.BaseStream.Seek(offset, SeekOrigin.Begin);

                    int[] numericByColIndex = new int[columns.Length];

                    for (int c = 0; c < columns.Length; c++)
                    {
                        string t = columns[c].Type;
                        if (t == "INT" || t == "DATE")
                        {
                            numericByColIndex[c] = r.ReadInt32();
                        }
                        else if (t == "STRING")
                        {
                            for (int b = 0; b < columns[c].Size; b++) r.ReadByte();
                        }
                    }

                    if (EvalWhere(conds, links, condCount, numericByColIndex, columns))
                        matches.Add(row);
                }
            }

            int[] res = new int[matches.Count];
            for (int i = 0; i < matches.Count; i++) res[i] = matches[i];
            return res;
        }

        private void ReadIndex(string idxFile, out int[] keys, out int[] rows)
        {
            using (BinaryReader r = new BinaryReader(File.Open(idxFile, FileMode.Open)))
            {
                r.ReadString(); // table name
                r.ReadString(); // column name
                int n = r.ReadInt32();

                keys = new int[n];
                rows = new int[n];

                for (int i = 0; i < n; i++)
                {
                    keys[i] = r.ReadInt32();
                    rows[i] = r.ReadInt32();
                }
            }
        }

        private int BinarySearch(int[] keys, int value)
        {
            int l = 0, r = keys.Length - 1;
            while (l <= r)
            {
                int m = (l + r) / 2;
                if (keys[m] == value) return m;
                if (keys[m] < value) l = m + 1;
                else r = m - 1;
            }
            return -1;
        }
        public void Select(
            string[] selectedCols,
            Cond[] conds, string[] links, int condCount,
            bool distinct, bool hasOrder, string orderCol, bool orderAsc)
        {

            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
            {
                Console.WriteLine($"Table '{Name}' does not exist!");
                return;
            }

            ReadMeta(out int rowCount, out Column[] columns,
                        out int freeCount, out int[] freeSlots, out _);

            //idx
            if (condCount == 1 &&
                conds[0].Op == "=" &&
                !conds[0].Not)
            {
                int colIdx = FindColumnIndex(columns, conds[0].Col);
                if (colIdx != -1 &&
                    (columns[colIdx].Type == "INT" ||
                     columns[colIdx].Type == "DATE"))
                {
                    string idxFile = Path.Combine(GetStorageDir(),$"{Name}_{conds[0].Col}.idx");

                    if (File.Exists(idxFile))
                    {
                        int value = 0;
                        if (columns[colIdx].Type == "INT")
                            value = ParseIntOrError(conds[0].ValStr);
                        else if (columns[colIdx].Type == "DATE")
                            value = ParseDateToInt(conds[0].ValStr);

                        ReadIndex(idxFile, out int[] keys, out int[] rows);
                        int pos = BinarySearch(keys, value);

                        if (pos != -1)
                        {
                            GetRows(new int[] { rows[pos] });
                        }
                        return;
                    }
                }
            }

            if (selectedCols.Length == 1 && selectedCols[0] == "*")
            {
                selectedCols = new string[columns.Length];
                for (int i = 0; i < columns.Length; i++)
                    selectedCols[i] = columns[i].Name;
            }

            int[] selIdx = new int[selectedCols.Length];
            for (int i = 0; i < selectedCols.Length; i++)
            {
                selIdx[i] = FindColumnIndex(columns, selectedCols[i]);
                if (selIdx[i] == -1)
                {
                    Console.WriteLine($"Unknown column '{selectedCols[i]}'");
                    return;
                }
            }

            int orderColIndex = -1;
            if (hasOrder)
            {
                orderColIndex = FindColumnIndex(columns, orderCol);
                if (orderColIndex == -1)
                {
                    Console.WriteLine($"Unknown column '{orderCol}' in ORDER BY.");
                    return;
                }

                string ot = columns[orderColIndex].Type;
                if (ot != "INT" && ot != "DATE")
                {
                    Console.WriteLine("ORDER BY is supported only for INT/DATE.");
                    return;
                }
            }

            for (int i = 0; i < selectedCols.Length; i++)
                Console.Write(selectedCols[i] + "\t");
            Console.WriteLine();

            bool mustCollect = distinct || hasOrder;

            List<string[]> outRows = new List<string[]>();
            List<int> orderKeys = new List<int>();

            int rowSize = GetRowSize(columns);
            long dataStart = GetDataStart();

            using (BinaryReader r = new BinaryReader(File.Open(tableFile, FileMode.Open, FileAccess.Read)))
            {
                r.ReadString(); // header

                for (int row = 1; row <= rowCount; row++)
                {
                    if (IsDeletedRow(row, freeSlots, freeCount))
                        continue;

                    long offset = dataStart + (long)(row - 1) * rowSize;
                    r.BaseStream.Seek(offset, SeekOrigin.Begin);

                    int[] numericByColIndex = new int[columns.Length];
                    string[] stringByColIndex = new string[columns.Length];

                    for (int c = 0; c < columns.Length; c++)
                    {
                        string t = columns[c].Type;
                        if (t == "INT" || t == "DATE")
                            numericByColIndex[c] = r.ReadInt32();
                        else if (t == "STRING")
                            stringByColIndex[c] = ReadFixedString(r, columns[c].Size);
                    }

                    if (!EvalWhere(conds, links, condCount, numericByColIndex, columns))
                        continue;

                    string[] outRow = new string[selIdx.Length];
                    for (int i = 0; i < selIdx.Length; i++)
                    {
                        int c = selIdx[i];
                        string t = columns[c].Type;
                        if (t == "INT" || t == "DATE") outRow[i] = numericByColIndex[c].ToString();
                        else outRow[i] = stringByColIndex[c] ?? "";
                    }

                    if (!mustCollect)
                    {
                        for (int i = 0; i < outRow.Length; i++)
                            Console.Write(outRow[i] + "\t");
                        Console.WriteLine();
                    }
                    else
                    {
                        outRows.Add(outRow);
                        if (hasOrder) orderKeys.Add(numericByColIndex[orderColIndex]);
                    }
                }
            }

            if (!mustCollect) return;

            if (distinct)
            {
                List<string[]> uniq = new List<string[]>();
                List<int> uniqKeys = new List<int>();

                for (int i = 0; i < outRows.Count; i++)
                {
                    bool exists = false;

                    for (int j = 0; j < uniq.Count; j++)
                    {
                        bool same = true;
                        for (int k = 0; k < outRows[i].Length; k++)
                        {
                            if (outRows[i][k] != uniq[j][k])
                            {
                                same = false;
                                break;
                            }
                        }
                        if (same) { exists = true; break; }
                    }

                    if (!exists)
                    {
                        uniq.Add(outRows[i]);
                        if (hasOrder) uniqKeys.Add(orderKeys[i]);
                    }
                }

                outRows = uniq;
                if (hasOrder) orderKeys = uniqKeys;
            }

            if (hasOrder)
            {
                for (int i = 0; i < outRows.Count - 1; i++)
                {
                    for (int j = i + 1; j < outRows.Count; j++)
                    {
                        bool swap = orderAsc ? (orderKeys[j] < orderKeys[i]) : (orderKeys[j] > orderKeys[i]);
                        if (swap)
                        {
                            var tr = outRows[i]; outRows[i] = outRows[j]; outRows[j] = tr;
                            int tk = orderKeys[i]; orderKeys[i] = orderKeys[j]; orderKeys[j] = tk;
                        }
                    }
                }
            }

            for (int i = 0; i < outRows.Count; i++)
            {
                for (int c = 0; c < outRows[i].Length; c++)
                    Console.Write(outRows[i][c] + "\t");
                Console.WriteLine();
            }
        }

        public void CreateIndex(string indexName, string columnName)
        {
            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
            {
                Console.WriteLine($"Table '{Name}' does not exist!");
                return;
            }

            ReadMeta(out int rowCount, out Column[] columns, out int freeCount, out int[] freeSlots, out _);

            int colIndex = FindColumnIndex(columns, columnName);
            if (colIndex == -1) { Console.WriteLine("Unknown column."); return; }

            string ct = columns[colIndex].Type;
            if (ct != "INT" && ct != "DATE")
            {
                Console.WriteLine("Index is supported only for INT/DATE.");
                return;
            }

            string idxFile = Path.Combine(GetStorageDir(), $"{Name}_{columnName}.idx");

            int rowSize = GetRowSize(columns);
            long dataStart = GetDataStart();

            List<int> keys = new List<int>();
            List<int> rows = new List<int>();

            using (BinaryReader r = new BinaryReader(File.Open(tableFile, FileMode.Open, FileAccess.Read)))
            {
                r.ReadString();
                for (int row = 1; row <= rowCount; row++)
                {
                    if (IsDeletedRow(row, freeSlots, freeCount))
                        continue;

                    long offset = dataStart + (long)(row - 1) * rowSize;
                    r.BaseStream.Seek(offset, SeekOrigin.Begin);

                    int key = 0;
                    for (int c = 0; c < columns.Length; c++)
                    {
                        string t = columns[c].Type;
                        if (t == "INT" || t == "DATE")
                        {
                            int v = r.ReadInt32();
                            if (c == colIndex) key = v;
                        }
                        else if (t == "STRING")
                        {
                            for (int b = 0; b < columns[c].Size; b++) r.ReadByte();
                        }
                    }

                    keys.Add(key);
                    rows.Add(row);
                }
            }

            //srt
            for (int i = 0; i < keys.Count - 1; i++)
                for (int j = i + 1; j < keys.Count; j++)
                    if (keys[j] < keys[i])
                    {
                        int tk = keys[i]; keys[i] = keys[j]; keys[j] = tk;
                        int tr = rows[i]; rows[i] = rows[j]; rows[j] = tr;
                    }

            using (BinaryWriter w = new BinaryWriter(File.Open(idxFile, FileMode.Create, FileAccess.Write)))
            {
                w.Write(Name);
                w.Write(columnName);
                w.Write(keys.Count);
                for (int i = 0; i < keys.Count; i++)
                {
                    w.Write(keys[i]);
                    w.Write(rows[i]);
                }
            }

            Console.WriteLine($"Index '{indexName}' created.");
        }

        private int CalcDataChecksum(string path, long startOffset)
        {
            int sum = 0;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(startOffset, SeekOrigin.Begin);
                int b;
                while ((b = fs.ReadByte()) != -1)
                    sum += b;
            }
            return sum;
        }

        public static void DropIndex(string indexName)
        {
            string storageDir = Path.Combine(AppContext.BaseDirectory, "Storage");
            Directory.CreateDirectory(storageDir);

            string idxFile = Path.Combine(storageDir, indexName + ".idx");

            if (!File.Exists(idxFile))
            {
                Console.WriteLine($"Index '{indexName}' does not exist!");
                return;
            }

            File.Delete(idxFile);
            Console.WriteLine($"Index '{indexName}' dropped.");
        }

        public string GetIndexesInfo()
        {
            string storage = GetStorageDir();

            string[] idxFiles = Directory.GetFiles(
                storage,
                $"{Name}_*.idx"
            );

            if (idxFiles.Length == 0)
                return "No indexes defined.";

            string result = $"Indexes on table {Name}:\n\n";

            for (int i = 0; i < idxFiles.Length; i++)
            {
                using (BinaryReader r = new BinaryReader(File.Open(idxFiles[i], FileMode.Open)))
                {
                    string tableName = r.ReadString();
                    string columnName = r.ReadString();
                    int count = r.ReadInt32();

                    result += $"• {Path.GetFileNameWithoutExtension(idxFiles[i])}\n";
                    result += $"  Column: {columnName}\n";
                    result += $"  Entries: {count}\n\n";
                }
            }

            return result;
        }
        public string CheckIntegrity()
        {
            string tableFile = GetDataPath();
            string metaFile = GetMetaPath();

            if (!File.Exists(tableFile) || !File.Exists(metaFile))
                return $"Table '{Name}' does not exist!";

            ReadMeta(out int rowCount, out Column[] columns, out int freeCount, out int[] freeSlots, out int storedChecksum);

            int realChecksum = CalcDataChecksum(tableFile, GetDataStart());
            if (storedChecksum == 0)
                return "OK (no checksum stored yet).";

            if (realChecksum != storedChecksum)
                return $"CORRUPT: checksum mismatch (meta={storedChecksum}, real={realChecksum})";

            return "Integrity check passed.";
        }
    }
}
