﻿using System;
using System.Linq;
using System.Text;
using Sparrow.Binary;
using Voron;
using Voron.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace FastTests.Voron.Tables
{
    public class Compression : StorageTest
    {
        public Compression(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public unsafe void Can_get_better_compression_rate_after_training()
        {
            using (var tx = Env.WriteTransaction())
            {
                Slice.From(tx.Allocator, "Compression", out var etagIndexName);
                var fixedSizedIndex = new TableSchema.FixedSizeSchemaIndexDef
                {
                    Name = etagIndexName,
                    IsGlobal = true,
                    StartIndex = 1,
                };

                var tableSchema = new TableSchema()
                    .CompressValues(fixedSizedIndex, true)
                    .DefineFixedSizeIndex(fixedSizedIndex)
                    .DefineKey(new TableSchema.SchemaIndexDef
                    {
                        StartIndex = 0,
                        Count = 1,
                    });

                tableSchema.Create(tx, "Items", 16);
                var itemsTable = tx.OpenTable(tableSchema, "Items");
                long number = 1L;
                var random = new Random(49941); 
                var data = string.Join(", ", Enumerable.Range(0, 100).Select(_ =>
                {
                    var bytes = new byte[16];
                    random.NextBytes(bytes);
                    return new Guid(bytes).ToString();
                }));
                
                int firstAllocatedSize;
                
                using (itemsTable.Allocate(out TableValueBuilder builder))
                using (Slice.From(tx.Allocator, "val", out var key))
                using (Slice.From(tx.Allocator, data, out var val ))
                {
                    builder.Add(key);
                    builder.Add(Bits.SwapBytes(number));
                    builder.Add(val);
                    long id = itemsTable.Insert(builder);
                    firstAllocatedSize = itemsTable.GetAllocatedSize(id);
                }

                var minAllocatedSize = 0;
                for (int i = 0; i < 100; i++)
                {
                    using (itemsTable.Allocate(out TableValueBuilder builder))
                    using (Slice.From(tx.Allocator, "val" + i, out var key))
                    using (Slice.From(tx.Allocator, data, out var val ))
                    {
                        builder.Add(key);
                        builder.Add(Bits.SwapBytes(++number));
                        builder.Add(val);
                        long id = itemsTable.Insert(builder);
                        var allocatedSize = itemsTable.GetAllocatedSize(id);
                        minAllocatedSize = Math.Min(minAllocatedSize, allocatedSize);
                    }
                }

                Assert.True(minAllocatedSize < firstAllocatedSize);
            }
        }

        [Fact]
        public unsafe void Can_define_compressed_table_and_read_write_small()
        {
            using (var tx = Env.WriteTransaction())
            {
                Slice.From(tx.Allocator, "Compression", out var etagIndexName);
                var fixedSizedIndex = new TableSchema.FixedSizeSchemaIndexDef
                {
                    Name = etagIndexName,
                    IsGlobal = true,
                    StartIndex = 1,
                };

                var tableSchema = new TableSchema()
                    .CompressValues(fixedSizedIndex, true)
                    .DefineFixedSizeIndex(fixedSizedIndex)
                    .DefineKey(new TableSchema.SchemaIndexDef
                    {
                        StartIndex = 0,
                        Count = 1,
                    });

                tableSchema.Create(tx, "Items", 16);
                var itemsTable = tx.OpenTable(tableSchema, "Items");
                const long number = 1L;

                using (itemsTable.Allocate(out TableValueBuilder builder))
                using (Slice.From(tx.Allocator, "val1", out var key))
                using (Slice.From(tx.Allocator, new string('a', 1024*16), out var val ))
                {
                    builder.Add(key);
                    builder.Add(Bits.SwapBytes(number));
                    builder.Add(val);
                    long id = itemsTable.Insert(builder);
                    var allocatedSize = itemsTable.GetAllocatedSize(id);
                    Assert.True(allocatedSize < 128);
                }

                using (Slice.From(tx.Allocator, "val1", out var key))
                {
                    Assert.True(itemsTable.ReadByKey(key, out var reader));
                    var ptr = reader.Read(2, out var size);
                    using var _ = Slice.External(tx.Allocator, ptr, size, out var slice);
                    Assert.Equal(new string('a', 1024*16), slice.ToString());
                }
                
                using (Slice.From(tx.Allocator, "val1", out var key))
                {
                    Assert.True(itemsTable.ReadByKey(key, out var reader));
                    
                    Assert.True(itemsTable.ReadByKey(key, out var reader2));
                    

                    Assert.Equal(new IntPtr(reader.Pointer), new IntPtr(reader2.Pointer));
                }
            }
        }

        [Fact]
        public void Can_update_compressed_value()
        {
            using (var tx = Env.WriteTransaction())
            {
                Slice.From(tx.Allocator, "Compression", out var etagIndexName);
                var fixedSizedIndex = new TableSchema.FixedSizeSchemaIndexDef
                {
                    Name = etagIndexName,
                    IsGlobal = true,
                    StartIndex = 1,
                };

                var tableSchema = new TableSchema()
                    .CompressValues(fixedSizedIndex, true)
                    .DefineFixedSizeIndex(fixedSizedIndex)
                    .DefineKey(new TableSchema.SchemaIndexDef
                    {
                        StartIndex = 0,
                        Count = 1,
                    });

                tableSchema.Create(tx, "Items", 16);
                var itemsTable = tx.OpenTable(tableSchema, "Items");
                const long number = 1L;

                using (itemsTable.Allocate(out TableValueBuilder builder))
                using (Slice.From(tx.Allocator, "val1", out var key))
                using (Slice.From(tx.Allocator, new string('a', 1024 * 16), out var val))
                {
                    builder.Add(key);
                    builder.Add(Bits.SwapBytes(number));
                    builder.Add(val);
                    long id = itemsTable.Insert(builder);
                    var allocatedSize = itemsTable.GetAllocatedSize(id);
                    Assert.True(allocatedSize < 128);
                }

                using (itemsTable.Allocate(out TableValueBuilder builder))
                using (Slice.From(tx.Allocator, "val1", out var key))
                using (Slice.From(tx.Allocator, new string('a', 1024 * 32), out var val))
                {
                    builder.Add(key);
                    builder.Add(Bits.SwapBytes(number));
                    builder.Add(val);
                    itemsTable.Set(builder);
                }

                using (Slice.From(tx.Allocator, "val1", out var key))
                {
                    Assert.True(itemsTable.ReadByKey(key, out var reader));
                    Assert.True(itemsTable.GetAllocatedSize(reader.Id) < 128);

                }
            }
        }

        [Fact]
        public unsafe void Can_define_compressed_table_and_read_write_large()
        {
            using (var tx = Env.WriteTransaction())
            {
                Slice.From(tx.Allocator, "Compression", out var etagIndexName);
                var fixedSizedIndex = new TableSchema.FixedSizeSchemaIndexDef
                {
                    Name = etagIndexName,
                    IsGlobal = true,
                    StartIndex = 1,
                };

                var tableSchema = new TableSchema()
                    .CompressValues(fixedSizedIndex, true)
                    .DefineFixedSizeIndex(fixedSizedIndex)
                    .DefineKey(new TableSchema.SchemaIndexDef
                    {
                        StartIndex = 0,
                        Count = 1,
                    });

                tableSchema.Create(tx, "Items", 16);
                var itemsTable = tx.OpenTable(tableSchema, "Items");
                const long number = 1L;

                var str = Enumerable.Range(0,10_000)
                    .Aggregate(new StringBuilder(), (sb, i) => sb.Append(i) )
                    .ToString();
                
                using (itemsTable.Allocate(out TableValueBuilder builder))
                using (Slice.From(tx.Allocator, "val1", out var key))
                using (Slice.From(tx.Allocator, str, out var val ))
                {
                    builder.Add(key);
                    builder.Add(Bits.SwapBytes(number));
                    builder.Add(val);
                    long id = itemsTable.Insert(builder);
                    long allocatedSize = itemsTable.GetAllocatedSize(id);
                    Assert.True(allocatedSize > 8192 && allocatedSize < str.Length);
                }

                using (Slice.From(tx.Allocator, "val1", out var key))
                {
                    Assert.True(itemsTable.ReadByKey(key, out var reader));
                    var ptr = reader.Read(2, out var size);
                    using var _ = Slice.External(tx.Allocator, ptr, size, out var slice);
                    Assert.Equal(str, slice.ToString());
                }
                
                using (Slice.From(tx.Allocator, "val1", out var key))
                {
                    Assert.True(itemsTable.ReadByKey(key, out var reader));
                    
                    Assert.True(itemsTable.ReadByKey(key, out var reader2));
                    

                    Assert.Equal(new IntPtr(reader.Pointer), new IntPtr(reader2.Pointer));
                }
            }
        }

        public static string RandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        [Fact]
        public void Can_force_small_value_to_compress_to_large()
        {
            var random = new Random(222);
            using (var tx = Env.WriteTransaction())
            {
                Slice.From(tx.Allocator, "Compression", out var etagIndexName);
                var fixedSizedIndex = new TableSchema.FixedSizeSchemaIndexDef
                {
                    Name = etagIndexName,
                    IsGlobal = true,
                    StartIndex = 1,
                };

                var tableSchema = new TableSchema()
                    .CompressValues(fixedSizedIndex, true)
                    .DefineFixedSizeIndex(fixedSizedIndex)
                    .DefineKey(new TableSchema.SchemaIndexDef
                    {
                        StartIndex = 0,
                        Count = 1,
                    });

                tableSchema.Create(tx, "Items", 16);
                var itemsTable = tx.OpenTable(tableSchema, "Items");
                 long number = 1L;

                 var rnd = Enumerable.Range(1, 32)
                     .Select(i => RandomString(random, 1024))
                     .ToList();

                for (int i = 0; i < 16*1024; i++)
                {
                    using (itemsTable.Allocate(out TableValueBuilder builder))
                    using (Slice.From(tx.Allocator, "val" + i, out var key))
                    using (Slice.From(tx.Allocator, rnd[i%rnd.Count], out var val))
                    {
                        builder.Add(key);
                        builder.Add(Bits.SwapBytes(++number));
                        builder.Add(val);
                        itemsTable.Insert(builder);
                    }
                }

                for (int i = 512; i < 768; i++)
                {
                    using (Slice.From(tx.Allocator, "val" + i, out var key))
                    {
                        itemsTable.DeleteByKey(key);
                    }
                }

                var str = Enumerable.Range(0, 10_000)
                    .Aggregate(new StringBuilder(), (sb, i) => sb.Append((char)(i%26 + 'A')))
                    .ToString();

                for (int i = 0; i < 256; i++)
                {
                    using (itemsTable.Allocate(out TableValueBuilder builder))
                    using (Slice.From(tx.Allocator, "val" + i, out var key))
                    using (Slice.From(tx.Allocator, str, out var val))
                    {
                        builder.Add(key);
                        builder.Add(Bits.SwapBytes(++number));
                        builder.Add(val);
                        itemsTable.Set(builder);
                    }
                }
            }
        }


    }
}
