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
            var sk1 = new ShardKey<short>('a', 3, 4);
            var str = sk1.ToExternalString();
            var sk2 = ShardKey<short>.FromExternalString(str);
            sk2.Should().Be(sk1, "because the serialized string creates an equivalent shardKey");
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
