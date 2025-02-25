using Xunit;
using System;
using System.Collections.Generic;
using ArgentSea;
using FluentAssertions;
using System.Text;
using System.Runtime;

namespace ArgentSea.Test
{
    public class ShardSerializationTests
    {
        [Fact]
        public void TestShardKeySerializationInts1()
        {
            var sk1 = new ShardKey<short>('a', 3, 4);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<short>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationInts3()
        {
            var sk1 = new ShardKey<short>('a', 3, 4);
            var array = sk1.ToArray();
            var sk2 = new ShardKey<short>(array);
            sk2.Should().Be(sk1, "because the serialized array creates an equivalent shardKey");
        }
        [Fact]
        public void TestOreleansShardKeySerializationInts4()
        {
            var sk1 = new ShardKey<Guid>('s', 0, Guid.NewGuid());
            //var span = new Span<byte>()
            ReadOnlyMemory<byte> rom = sk1.ToUtf8();
            var sk2 = ShardKey<Guid>.FromUtf8(rom.Span);
            sk2.Should().Be(sk1, "because the serialized array creates an equivalent shardKey");
            var str = Encoding.UTF8.GetString(rom.Span);
            var utf8 = Encoding.UTF8.GetBytes(str);
            //var sk3 = new ShardKey<Guid>(utf8);
            var sk3 = ShardKey<Guid>.FromUtf8(utf8);
            sk3.Should().Be(sk1, "because the serialized array creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationStr()
        {
            var sk1 = new ShardKey<string>('a', 0, "two");
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<string>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationFloat()
        {
            var sk1 = new ShardKey<double>('a', 0, 1.0f);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<double>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized float creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationDouble()
        {
            var sk1 = new ShardKey<double>('a', 0, 0.3);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<double>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized double creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationInts2()
        {
            var sk1 = new ShardKey<long>('a', 3, 4);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<long>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationMore1()
        {
            var sk1 = new ShardKey<decimal>('a', 0, 4);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<decimal>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized decimal creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationMore2()
        {
            var sk1 = new ShardKey<Guid>('a', 0, Guid.NewGuid());
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<Guid>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized Guid creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationMore3()
        {
            var sk1 = new ShardKey<DateTime>('a', 0, DateTime.UtcNow);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<DateTime>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized DateTime creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardChildSerializationInts()
        {
            var sc1 = new ShardKey<int, short>('a', 5, 6, 7);
            var str = sc1.ToExternalString();
            var sc2 = ShardKey<int, short>.FromExternalString(str);
            sc2.Should().Be(sc1, "because the serialized string creates an equivalent shardChild");
        }
    }
}
