using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class DataFile
    {
        public enum Column
        {
            Int32 = 0,
            String = 1,
            ByteArray = 2,
            Int64 =3
        }

        private string[]? columns;
        private Column[]? types;
        private List<object> values = new List<object>();
        private int offset = 0;
        private readonly string filename;

        public DataFile(string filename)
        {
            this.filename = filename;
        }

        public void CreateFile(string[] columns, Column[] types)
        {
            this.columns = columns;
            this.types = types;

            SaveFile();
        }

        public bool ReadFile()
        {
            if (File.Exists(filename))
            {
                using (FileStream stream = File.OpenRead(filename))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int numCols = reader.ReadInt32();
                        columns = new string[numCols];
                        types = new Column[numCols];
                        for (int i = 0; i < numCols; i++)
                        {
                            columns[i] = reader.ReadString();
                            types[i] = (Column)reader.ReadInt32();
                        }

                        int numRows = reader.ReadInt32();
                        for (int i = 0; i < numRows; i++)
                        {
                            for (int j = 0; j < numCols; j++)
                            {
                                switch (types[j])
                                {
                                    case Column.Int32:
                                        values.Add(reader.ReadInt32());
                                        break;
                                    case Column.Int64:
                                        values.Add(reader.ReadInt64());
                                        break;
                                    case Column.String:
                                        values.Add(reader.ReadString());
                                        break;
                                    case Column.ByteArray:
                                        int numBytes = reader.ReadInt32();
                                        values.Add(reader.ReadBytes(numBytes));
                                        break;
                                    default:
                                        Logger.Instance.Log("[{0}] Unexpected data type '{1}' encountered.", filename, types[i]);
                                        break;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private void CreateDataDir()
        {
            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }
        }

        public void SaveFile()
        {
            CreateDataDir();

            if (columns == null || types == null)
            {
                return;
            }

            using (FileStream stream = File.OpenWrite(filename))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(columns.Length);
                    for (int i = 0; i < columns.Length; i++)
                    {
                        writer.Write(columns[i]);
                        writer.Write((int)types[i]);
                    }

                    int numRows = values.Count / columns.Length;
                    writer.Write(numRows);
                    
                    for (int i = 0; i < numRows; i++)
                    {
                        for (int j = 0; j < columns.Length; j++)
                        {
                            int idx = i * columns.Length + j;
                            switch (types[j])
                            {
                                case Column.Int32:
                                    writer.Write((int)values[idx]);
                                    break;
                                case Column.Int64:
                                    writer.Write((long)values[idx]);
                                    break;
                                case Column.String:
                                    writer.Write((string)values[idx]);
                                    break;
                                case Column.ByteArray:
                                    byte[] bytes = (byte[])values[idx];
                                    writer.Write(bytes.Length);
                                    writer.Write(bytes);
                                    break;
                                default:
                                    Logger.Instance.Log("[{0}] Unexpected data type '{1}' encountered.", filename, types[i]);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            this.SeekBegin();
            this.values.Clear();
        }

        public void SeekBegin()
        {
            this.offset = 0;
        }

        public void SeekEnd()
        {
            this.offset = this.values.Count;
        }

        public void NewRow()
        {
            if (columns != null && types != null)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    switch (types[i])
                    {
                        case Column.ByteArray:
                            this.values.Add(new byte[0]);
                            break;
                        case Column.Int32:
                            this.values.Add(0);
                            break;
                        case Column.Int64:
                            this.values.Add(0L);
                            break;
                        case Column.String:
                            this.values.Add(string.Empty);
                            break;
                    }
                }
            }
        }

        public bool NextRow()
        {
            if (this.columns != null)
            {
                this.offset += columns.Length;
                return this.offset < this.values.Count;
            }
            return false;
        }

        public int RowID()
        {
            if (this.columns == null)
            {
                return -1;
            }

            return this.offset / this.columns.Length;
        }

        public void Seek(int row)
        {
            if (this.columns == null)
            {
                return;
            }

            this.offset = row * this.columns.Length;
        }

        private int GetColumnId(string columnName)
        {
            if (columns != null)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    if (columns[i].Equals(columnName))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public string? GetString(string id)
        {
            int col = GetColumnId(id);
            if (col == -1) {  return null; }
            return this.values[offset + col] as string;
        }

        public int? GetInt32(string id)
        {
            int col = GetColumnId(id);
            if (col == -1) {  return null; }
            return this.values[offset + col] as int?;
        }

        public long? GetInt64(string id)
        {
            int col = GetColumnId(id);
            if (col == -1) { return null; }
            return this.values[offset + col] as long?;
        }

        public byte[]? GetByteArray(string id)
        {
            int col = GetColumnId(id);
            if (col == -1) { return null; }
            return this.values[offset + col] as byte[];
        }

        public void SetData(string id, object value)
        {
            int col = GetColumnId(id);
            if (col != -1)
            {
                this.values[offset + col] = value;
            }
        }
    }
}
