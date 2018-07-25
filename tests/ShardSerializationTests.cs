using Xunit;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel.Resolution;
using ArgentSea;
using FluentAssertions;

namespace ArgentSea.Test
{
    public class ShardSerializationTests
    {
        [Fact]
        public void TestShardKeySerializationInts1()
        {
            var sk1 = new ShardKey<byte, short>('a', 3, 4);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<byte, short>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationStr()
        {
            var sk1 = new ShardKey<string, string>('a', "one", "two");
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<string, string>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationFloat()
        {
            var sk1 = new ShardKey<float, double>('a', 1.0f, 0.3);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<float, double>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationInts2()
        {
            var sk1 = new ShardKey<int, long>('a', 3, 4);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<int, long>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationMore1()
        {
            var sk1 = new ShardKey<char, decimal>('a', 'x', 4);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<char, decimal>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardKeySerializationMore2()
        {
            var sk1 = new ShardKey<DateTime, Guid>('a', DateTime.UtcNow, Guid.NewGuid());
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<DateTime, Guid>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
        }
        [Fact]
        public void TestShardChildSerializationInts()
        {
            var sc1 = new ShardChild<byte, int, short>('a', 5, 6, 7);
            var str = sc1.ToExternalString();
            var sc2 = ShardChild<byte, int, short>.FromExternalString(str);
            sc2.Should().Be(sc1, "because the serialized string creates an equivalent shardChild");
        }
    }
}
