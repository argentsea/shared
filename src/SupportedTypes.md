# Shard Supported Types
Formally, the interface definition for the ShardKey and ShardChild generic objects 
allow any type that implements *IComparable*. However, because the 
serialization/deserialization functions and Expression Tree mappings
work from a list of known types, the options are actually more limited.
 
The type also needs to be supported by the corresponding database engine; 
for example, there is no obvious way to save uint, ushort, ulong, and 
sbyte types in SQL Server or PostgreSQL.

The ShardKey and ShardChild generics support all “built in” simple C# value types 
(except bool). They also support the corresponding Nullable types and Enum types.

### Type List
To make this explicit, this is an exhausive list of supported data types:
* byte/byte?/Enum:byte/Nullable<Enum: byte>
* char/char?
* DateTime/DateTime?
* DateTimeOffset/DateTimeOffset?
* decimal/decimal?
* double/double?
* float/float?
* Guid/Guid?
* int/int?/Enum:int/Nullable<Enum: int>
* long/long?/Enum:long/Nullable<Enum: long>
* sbyte/sbyte?Enum:sbyte/Nullable<Enum: sbyte>
* short/short?/Enum:short/Nullable<Enum: short>
* string
* TimeSpan/TimeSpan?
* uint/uint?/Enum: uint/Nullable<Enum: uint>
* ulong/ulong?/Enum: ulong/Nullable<Enum: ulong>
* ushort/ushort?/Enum: short/Nullable<Enum: short>

