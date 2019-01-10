# ArgentSea Shared Library

ArgentSea is a data access framework, built for simplicity, high-performance, and global scale.

This NuGet package contains most of the code for the ArgentSea framework. To be useful, however, it must be implemented by a platform-specific package. 

Currently, these are:

* __[ArgentSea.Sql](https://www.nuget.org/packages/ArgentSea.Sql/)__ - for Microsoft SQL Server
* __[ArgnetSea.Pg](https://www.nuget.org/packages/ArgentSea.Pg/)__ - for PostgreSQL

Because these other packages include a dependency upon ArgentSea, it is unlikely that you would need to include or download this package directly. 