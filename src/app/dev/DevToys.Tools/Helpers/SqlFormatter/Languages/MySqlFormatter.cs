﻿using DevToys.Tools.Helpers.SqlFormatter.Core;

namespace DevToys.Tools.Helpers.SqlFormatter.Languages;

internal sealed class MySqlFormatter : Formatter
{
    private static readonly string[] ReservedWords
        =
        {
            "ACCESSIBLE",
            "ADD",
            "ALL",
            "ALTER",
            "ANALYZE",
            "AND",
            "AS",
            "ASC",
            "ASENSITIVE",
            "BEFORE",
            "BETWEEN",
            "BIGINT",
            "BINARY",
            "BLOB",
            "BOTH",
            "BY",
            "CALL",
            "CASCADE",
            "CASE",
            "CHANGE",
            "CHAR",
            "CHARACTER",
            "CHECK",
            "COLLATE",
            "COLUMN",
            "CONDITION",
            "CONSTRAINT",
            "CONTINUE",
            "CONVERT",
            "CREATE",
            "CROSS",
            "CUBE",
            "CUME_DIST",
            "CURRENT_DATE",
            "CURRENT_TIME",
            "CURRENT_TIMESTAMP",
            "CURRENT_USER",
            "CURSOR",
            "DATABASE",
            "DATABASES",
            "DAY_HOUR",
            "DAY_MICROSECOND",
            "DAY_MINUTE",
            "DAY_SECOND",
            "DEC",
            "DECIMAL",
            "DECLARE",
            "DEFAULT",
            "DELAYED",
            "DELETE",
            "DENSE_RANK",
            "DESC",
            "DESCRIBE",
            "DETERMINISTIC",
            "DISTINCT",
            "DISTINCTROW",
            "DIV",
            "DOUBLE",
            "DROP",
            "DUAL",
            "EACH",
            "ELSE",
            "ELSEIF",
            "EMPTY",
            "ENCLOSED",
            "ESCAPED",
            "EXCEPT",
            "EXISTS",
            "EXIT",
            "EXPLAIN",
            "FALSE",
            "FETCH",
            "FIRST_VALUE",
            "FLOAT",
            "FLOAT4",
            "FLOAT8",
            "FOR",
            "FORCE",
            "FOREIGN",
            "FROM",
            "FULLTEXT",
            "FUNCTION",
            "GENERATED",
            "GET",
            "GRANT",
            "GROUP",
            "GROUPING",
            "GROUPS",
            "HAVING",
            "HIGH_PRIORITY",
            "HOUR_MICROSECOND",
            "HOUR_MINUTE",
            "HOUR_SECOND",
            "IF",
            "IGNORE",
            "IN",
            "INDEX",
            "INFILE",
            "INNER",
            "INOUT",
            "INSENSITIVE",
            "INSERT",
            "INT",
            "INT1",
            "INT2",
            "INT3",
            "INT4",
            "INT8",
            "INTEGER",
            "INTERVAL",
            "INTO",
            "IO_AFTER_GTIDS",
            "IO_BEFORE_GTIDS",
            "IS",
            "ITERATE",
            "JOIN",
            "JSON_TABLE",
            "KEY",
            "KEYS",
            "KILL",
            "LAG",
            "LAST_VALUE",
            "LATERAL",
            "LEAD",
            "LEADING",
            "LEAVE",
            "LEFT",
            "LIKE",
            "LIMIT",
            "LINEAR",
            "LINES",
            "LOAD",
            "LOCALTIME",
            "LOCALTIMESTAMP",
            "LOCK",
            "LONG",
            "LONGBLOB",
            "LONGTEXT",
            "LOOP",
            "LOW_PRIORITY",
            "MASTER_BIND",
            "MASTER_SSL_VERIFY_SERVER_CERT",
            "MATCH",
            "MAXVALUE",
            "MEDIUMBLOB",
            "MEDIUMINT",
            "MEDIUMTEXT",
            "MIDDLEINT",
            "MINUTE_MICROSECOND",
            "MINUTE_SECOND",
            "MOD",
            "MODIFIES",
            "NATURAL",
            "NOT",
            "NO_WRITE_TO_BINLOG",
            "NTH_VALUE",
            "NTILE",
            "NULL",
            "NUMERIC",
            "OF",
            "ON",
            "OPTIMIZE",
            "OPTIMIZER_COSTS",
            "OPTION",
            "OPTIONALLY",
            "OR",
            "ORDER",
            "OUT",
            "OUTER",
            "OUTFILE",
            "OVER",
            "PARTITION",
            "PERCENT_RANK",
            "PRECISION",
            "PRIMARY",
            "PROCEDURE",
            "PURGE",
            "RANGE",
            "RANK",
            "READ",
            "READS",
            "READ_WRITE",
            "REAL",
            "RECURSIVE",
            "REFERENCES",
            "REGEXP",
            "RELEASE",
            "RENAME",
            "REPEAT",
            "REPLACE",
            "REQUIRE",
            "RESIGNAL",
            "RESTRICT",
            "RETURN",
            "REVOKE",
            "RIGHT",
            "RLIKE",
            "ROW",
            "ROWS",
            "ROW_NUMBER",
            "SCHEMA",
            "SCHEMAS",
            "SECOND_MICROSECOND",
            "SELECT",
            "SENSITIVE",
            "SEPARATOR",
            "SET",
            "SHOW",
            "SIGNAL",
            "SMALLINT",
            "SPATIAL",
            "SPECIFIC",
            "SQL",
            "SQLEXCEPTION",
            "SQLSTATE",
            "SQLWARNING",
            "SQL_BIG_RESULT",
            "SQL_CALC_FOUND_ROWS",
            "SQL_SMALL_RESULT",
            "SSL",
            "STARTING",
            "STORED",
            "STRAIGHT_JOIN",
            "SYSTEM",
            "TABLE",
            "TERMINATED",
            "THEN",
            "TINYBLOB",
            "TINYINT",
            "TINYTEXT",
            "TO",
            "TRAILING",
            "TRIGGER",
            "TRUE",
            "UNDO",
            "UNION",
            "UNIQUE",
            "UNLOCK",
            "UNSIGNED",
            "UPDATE",
            "USAGE",
            "USE",
            "USING",
            "UTC_DATE",
            "UTC_TIME",
            "UTC_TIMESTAMP",
            "VALUES",
            "VARBINARY",
            "VARCHAR",
            "VARCHARACTER",
            "VARYING",
            "VIRTUAL",
            "WHEN",
            "WHERE",
            "WHILE",
            "WINDOW",
            "WITH",
            "WRITE",
            "XOR",
            "YEAR_MONTH",
            "ZEROFILL",
        };

    private static readonly string[] ReservedTopLevelWords
        =
        {
            "ADD",
            "ALTER COLUMN",
            "ALTER TABLE",
            "DELETE FROM",
            "EXCEPT",
            "FROM",
            "GROUP BY",
            "HAVING",
            "INSERT INTO",
            "INSERT",
            "LIMIT",
            "ORDER BY",
            "SELECT",
            "SET",
            "UPDATE",
            "VALUES",
            "WHERE",
        };

    private static readonly string[] ReservedTopLevelWordsNoIndent = new[] { "INTERSECT", "INTERSECT ALL", "UNION", "UNION ALL" };

    private static readonly string[] ReservedNewlineWords
        =
        {
            "AND",
            "ELSE",
            "OR",
            "WHEN",
            // joins
            "JOIN",
            "INNER JOIN",
            "LEFT JOIN",
            "LEFT OUTER JOIN",
            "RIGHT JOIN",
            "RIGHT OUTER JOIN",
            "CROSS JOIN",
            "NATURAL JOIN",
            // non-standard joins
            "STRAIGHT_JOIN",
            "NATURAL LEFT JOIN",
            "NATURAL LEFT OUTER JOIN",
            "NATURAL RIGHT JOIN",
            "NATURAL RIGHT OUTER JOIN",
        };

    protected override Tokenizer GetTokenizer()
    {
        return
            new Tokenizer(
                ReservedWords,
                ReservedTopLevelWords,
                ReservedNewlineWords,
                ReservedTopLevelWordsNoIndent,
                stringTypes: new[] { "\"\"", "''", "``" },
                openParens: new[] { "(", "CASE" },
                closeParens: new[] { ")", "END" },
                indexedPlaceholderTypes: new[] { '?' },
                namedPlaceholderTypes: Array.Empty<char>(),
                lineCommentTypes: new[] { "#", "--" },
                specialWordChars: new[] { "@" },
                operators: new[] { ":=", "<<", ">>", "!=", "<>", "<=>", "&&", "||", "->", "->>" });
    }
}
