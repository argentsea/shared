// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Data;
using System.Reflection;

namespace ArgentSea
{
    /// <summary>
    /// This implementation of Query loads the SQL from a relative file.
    /// </summary>
    public class QueryStatement : Query
    {
        private static readonly Lazy<string> appPath = new Lazy<string>(() => 
            {
                var assembly = typeof(QueryStatement).GetTypeInfo().Assembly;
                return Path.GetDirectoryName(assembly.Location);
            });

        private QueryStatement()
            : base(null, null, null)
        {
            //hide default constructor
        }

        private QueryStatement(string name, string sql, string[] parameterNames)
            : base(sql, name, parameterNames)
        {
            //
        }
        public override CommandType Type { get => CommandType.Text; }

        public static string Folder { get; set; } = "SQL";

        public static string Extension { get; set; } = "sql";

        private static QueryStatement QueryStatementFactory(string name, string filepath, string[] parameterNames)
        {
            var sql = File.ReadAllText(filepath);
            return new QueryStatement(name, sql, parameterNames);
        }

        public static Lazy<QueryStatement> Create(string name)
        {
            return new Lazy<QueryStatement>(() =>
            {
                return QueryStatementFactory(name, $"{appPath.Value}\\{QueryStatement.Folder}\\{name}.{QueryStatement.Extension}", null);
            });
        }
        public static Lazy<QueryStatement> Create(string name, string[] parameterNames)
        {
            return new Lazy<QueryStatement>(() =>
            {
                return QueryStatementFactory(name, $"{appPath.Value}\\{QueryStatement.Folder}\\{name}.{QueryStatement.Extension}", parameterNames);
            });
        }
        public static Lazy<QueryStatement> Create(string name, string[] parameterNames, string folderName, string extension)
        {
            return new Lazy<QueryStatement>(() =>
            {
                return QueryStatementFactory(name, $"{appPath.Value}\\{folderName}\\{name}.{extension}", parameterNames);
            });
        }
        public static Lazy<QueryStatement> Create(string name, string[] parameterNames, string fullFilePath)
        {
            return new Lazy<QueryStatement>(() =>
            {
                return QueryStatementFactory(name, fullFilePath, parameterNames);
            });
        }
    }
}