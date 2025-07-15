# ArgentSea 

ArgentSea is a powerful data management library built for high-performance and global scale. 

The library supports the sharding of a single dataset across many servers, or the use of discrete tenant DBs behind a hosted solution.

Version 2 of this library has been adjusted to accomodate Microsoft Orleans. 

* Upgraded the code and dependencies to support .net 9 (dropped support for .NET standard)
* Changed the processing to support property setting of existing objects, rather than always “new-ing” a model result.
* Improved shard-key serialization performancee and enabled serialization to/from UTF8.
* Enables a genuinely 3rd-normal-form persistance layer for Microsoft Orleans.

This framework has been used in production for several years and has performed flawlessly.

## Core Library

This library is a set of abstractions that must be implemented by a database provider. You do not typically need to download this package directly.

Currently, ArgentSea has two implementations: one for SQL Server and one for PostgrSQL. 

The amount of code required for a particular platform implementation is relatively minor; most of the actual logic lives within this project.

## Contributions

Contributions are very welcome.

## License

[MIT.](https://opensource.org/licenses/MIT)


