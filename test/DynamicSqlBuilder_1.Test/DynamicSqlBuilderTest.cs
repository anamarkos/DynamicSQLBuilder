using DynamicSQLQueryBuilder;
using NSubstitute;
using System.Data;
using System.Globalization;
using Xunit;

namespace DynamicSqlBuilder_1.Test
{
    public class DynamicSqlBuilderTest
    {
        public DynamicSqlBuilderTest()
        {
            Filter.ResetGlobalFilterIndex(); // Add this method to the Filter class to reset the counter
        }

        [Fact]
        public void BuildAndExecuteQuery_WithStringFilter_CommandExecutedWithCorrectQuery()
        {
            // Arrange
            var mockConnection = Substitute.For<IDbConnection>();
            var mockCommand = Substitute.For<IDbCommand>();
            var commandText = "";

            mockConnection.CreateCommand().Returns(mockCommand);
            mockCommand.When(cmd => cmd.CommandText = Arg.Any<string>())
                       .Do(callInfo => commandText = callInfo.Arg<string>());

            var builder = new SqlQueryBuilder("baseTable");
            builder.AddFilter(new Filter(
                filterType: FilterType.Equals,
                tableName: "baseTable",
                columnName: "Name",
                columnType: ColumnType.String,
                value: "TestName"
            ));


            var expectedQuery = "SELECT * FROM baseTable WHERE baseTable.Name = @param0"; // The first filter will have the placeholder @param0

            // Act
            var query = builder.BuildQuery(out var queryParams);
            ExecuteQuery(query, mockConnection, queryParams);

            // Assert
            mockCommand.Received(1).ExecuteNonQuery();
            Assert.Equal(expectedQuery, commandText); // Verify if the query matches the expected one
        }

        [Fact]
        public void BuildQuery_MultipleTypesOfFilters_GeneratesCorrectSql()
        {
            // Arrange
            var mockConnection = Substitute.For<IDbConnection>();
            var mockCommand = Substitute.For<IDbCommand>();
            var commandText = "";

            mockConnection.CreateCommand().Returns(mockCommand);
            mockCommand.When(cmd => cmd.CommandText = Arg.Any<string>())
                       .Do(callInfo => commandText = callInfo.Arg<string>());

            var builder = new SqlQueryBuilder("baseTable");
            builder.AddFilter(new Filter(
                tableName: "baseTable",
                columnName: "Name",
                columnType: ColumnType.String,
                filterType: FilterType.Equals,
                value: "TestName"
            ));

            builder.AddFilter(new Filter(
                tableName: "baseTable",
                columnName: "Age",
                columnType: ColumnType.Numeric,
                filterType: FilterType.Greater,
                value: 30
            ));

            builder.AddFilter(new Filter(
                tableName: "baseTable",
                columnName: "StartDate",
                columnType: ColumnType.Date,
                filterType: FilterType.LesserOrEqual,
                value: new DateTime(2023, 1, 1)
            ));


            var expectedQuery = "SELECT * FROM baseTable WHERE baseTable.Name = @param0 AND baseTable.Age > @param1 AND baseTable.StartDate <= @param2";

            // Act
            var query = builder.BuildQuery(out var queryParams);
            ExecuteQuery(query, mockConnection, queryParams);

            // Assert
            Assert.Equal(expectedQuery, query);
        }

        [Fact]
        public void AddFilter_WithInvalidStringFilterOnDateColumn_ThrowsException()
        {
            var builder = new SqlQueryBuilder("baseTable");

            // Assert that an exception is thrown for an invalid filter combination
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddFilter(new Filter(
                    tableName: "baseTable",
                    columnName: "dateColumn",
                    columnType: ColumnType.Date,
                    filterType: FilterType.Contains, // Invalid for Date type
                    value: "2023-01-01"
                ))
            );
        }

        [Fact]
        public void AddFilter_WithInvalidBetweenFilterOnStringColumn_ThrowsException()
        {
            var builder = new SqlQueryBuilder("baseTable");

            // Assert that an exception is thrown for an invalid 'Between' filter on a string column
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddFilter(new Filter(
                    tableName: "baseTable",
                    columnName: "stringColumn",
                    columnType: ColumnType.String,
                    filterType: FilterType.Between, // Invalid for String type
                    value: "start",
                    secondValue: "end"
                ))
            );
        }

        [Theory]
        [InlineData(ColumnType.Date, FilterType.Contains)] // Invalid: 'Contains' not valid for 'Date'
        [InlineData(ColumnType.String, FilterType.Greater)] // Invalid: 'Greater' not valid for 'String'
        [InlineData(ColumnType.String, FilterType.Lesser)] // Invalid: 'Lesser' not valid for 'String'
        [InlineData(ColumnType.Numeric, FilterType.StartsWith)] // Invalid: 'StartsWith' not valid for 'Numeric'
        public void AddFilter_WithInvalidFilterCombination_ThrowsException(ColumnType columnType, FilterType filterType)
        {
            var builder = new SqlQueryBuilder("baseTable");

            // Assert that an exception is thrown for an invalid filter combination
            Assert.Throws<InvalidOperationException>(() =>
                builder.AddFilter(new Filter(
                    tableName: "baseTable",
                    columnName: "testColumn",
                    columnType: columnType,
                    filterType: filterType,
                    value: "testValue" // The value can be a generic test value
                ))
            );
        }
        [Theory]
        // String ColumnType Tests
        [InlineData(ColumnType.String, FilterType.Equals, "TestValue", "SELECT * FROM baseTable WHERE baseTable.TestColumn = @param0")]
        [InlineData(ColumnType.String, FilterType.NotEqual, "TestValue", "SELECT * FROM baseTable WHERE baseTable.TestColumn <> @param0")]
        [InlineData(ColumnType.String, FilterType.Contains, "Test", "SELECT * FROM baseTable WHERE baseTable.TestColumn LIKE @param0")]
        [InlineData(ColumnType.String, FilterType.StartsWith, "Test", "SELECT * FROM baseTable WHERE baseTable.TestColumn LIKE @param0")]
        [InlineData(ColumnType.String, FilterType.EndsWith, "Test", "SELECT * FROM baseTable WHERE baseTable.TestColumn LIKE @param0")]

        // Numeric ColumnType Tests
        [InlineData(ColumnType.Numeric, FilterType.Equals, 100, "SELECT * FROM baseTable WHERE baseTable.TestColumn = @param0")]
        [InlineData(ColumnType.Numeric, FilterType.NotEqual, 100, "SELECT * FROM baseTable WHERE baseTable.TestColumn <> @param0")]
        [InlineData(ColumnType.Numeric, FilterType.Greater, 100, "SELECT * FROM baseTable WHERE baseTable.TestColumn > @param0")]
        [InlineData(ColumnType.Numeric, FilterType.Lesser, 100, "SELECT * FROM baseTable WHERE baseTable.TestColumn < @param0")]
        [InlineData(ColumnType.Numeric, FilterType.GreaterOrEqual, 100, "SELECT * FROM baseTable WHERE baseTable.TestColumn >= @param0")]
        [InlineData(ColumnType.Numeric, FilterType.LesserOrEqual, 100, "SELECT * FROM baseTable WHERE baseTable.TestColumn <= @param0")]       
        public void BuildQuery_WithVariousFilters_GeneratesCorrectSql(ColumnType columnType, FilterType filterType, object value, string expectedQuery)
        {
            // Arrange
            var mockConnection = Substitute.For<IDbConnection>();
            var mockCommand = Substitute.For<IDbCommand>();
            var commandText = "";

            mockConnection.CreateCommand().Returns(mockCommand);
            mockCommand.When(cmd => cmd.CommandText = Arg.Any<string>())
                       .Do(callInfo => commandText = callInfo.Arg<string>());
            var builder = CreateQueryBuilderWithFilter(columnType, filterType, value);

            // Act
            var query = builder.BuildQuery(out var queryParams);
            ExecuteQuery(query, mockConnection, queryParams);

            // Assert
            Assert.Equal(expectedQuery, query);
        }

        // Date ColumnType Tests
        [InlineData(ColumnType.Date, FilterType.Equals, "2023-01-01", "SELECT * FROM baseTable WHERE baseTable.TestColumn = @param0")]
        [InlineData(ColumnType.Date, FilterType.NotEqual, "2023-01-01", "SELECT * FROM baseTable WHERE baseTable.TestColumn <> @param0")]
        [InlineData(ColumnType.Date, FilterType.Greater, "2023-01-01", "SELECT * FROM baseTable WHERE baseTable.TestColumn > @param0")]
        [InlineData(ColumnType.Date, FilterType.Lesser, "2023-01-01", "SELECT * FROM baseTable WHERE baseTable.TestColumn < @param0")]
        [InlineData(ColumnType.Date, FilterType.GreaterOrEqual, "2023-01-01", "SELECT * FROM baseTable WHERE baseTable.TestColumn >= @param0")]
        [InlineData(ColumnType.Date, FilterType.LesserOrEqual, "2023-01-01", "SELECT * FROM baseTable WHERE baseTable.TestColumn <= @param0")]
        public void BuildQuery_WithVariousDateFilters_GeneratesCorrectSql(ColumnType columnType, FilterType filterType, string value, string expectedQuery)
        {
            // Arrange
            var mockConnection = Substitute.For<IDbConnection>();
            var mockCommand = Substitute.For<IDbCommand>();
            var commandText = "";
            DateTime dateValue = DateTime.ParseExact(value, "yyyy-MM-dd", provider: CultureInfo.InvariantCulture);

            mockConnection.CreateCommand().Returns(mockCommand);
            mockCommand.When(cmd => cmd.CommandText = Arg.Any<string>())
                       .Do(callInfo => commandText = callInfo.Arg<string>());
            var builder = CreateQueryBuilderWithFilter(columnType, filterType, dateValue);

            // Act
            var query = builder.BuildQuery(out var queryParams);
            ExecuteQuery(query, mockConnection, queryParams);

            // Assert
            Assert.Equal(expectedQuery, query);
        }

        [Theory]
        [InlineData("10", "20", "SELECT * FROM baseTable WHERE baseTable.TestColumn BETWEEN @param0 AND @param1")]
        public void BuildQuery_WithNumericBetweenFilter_GeneratesCorrectSql(string startValue, string endValue, string expectedQuery)
        {
            // Arrange
            var mockConnection = Substitute.For<IDbConnection>();
            var mockCommand = Substitute.For<IDbCommand>();
            var commandText = "";

            mockConnection.CreateCommand().Returns(mockCommand);
            mockCommand.When(cmd => cmd.CommandText = Arg.Any<string>())
                       .Do(callInfo => commandText = callInfo.Arg<string>());

            var builder = new SqlQueryBuilder("baseTable");
            AddBetweenFilter(builder, ColumnType.Numeric, startValue, endValue);

            var query = builder.BuildQuery(out var queryParams);
            ExecuteQuery(query, mockConnection, queryParams);

            Assert.Equal(expectedQuery, query);
        }

        [Theory]
        [InlineData("2023-01-01", "2023-12-31", "SELECT * FROM baseTable WHERE baseTable.TestColumn BETWEEN @param0 AND @param1")]
        public void BuildQuery_WithDateBetweenFilter_GeneratesCorrectSql(string startValue, string endValue, string expectedQuery)
        {
            // Arrange
            var mockConnection = Substitute.For<IDbConnection>();
            var mockCommand = Substitute.For<IDbCommand>();
            var commandText = "";

            mockConnection.CreateCommand().Returns(mockCommand);
            mockCommand.When(cmd => cmd.CommandText = Arg.Any<string>())
                       .Do(callInfo => commandText = callInfo.Arg<string>());

            DateTime dateStartValue = DateTime.ParseExact(startValue, "yyyy-MM-dd", provider: CultureInfo.InvariantCulture);
            DateTime dateEndValue = DateTime.ParseExact(endValue, "yyyy-MM-dd", provider: CultureInfo.InvariantCulture);

            var builder = new SqlQueryBuilder("baseTable");
            AddBetweenFilter(builder, ColumnType.Date, dateStartValue, dateEndValue);

            var query = builder.BuildQuery(out var queryParams);
            ExecuteQuery(query, mockConnection, queryParams);

            Assert.Equal(expectedQuery, query);
        }

        [Fact]
        public void BuildQuery_WithMultipleTablesAndFilters_GeneratesCorrectSql()
        {
            // Arrange
            var mockConnection = Substitute.For<IDbConnection>();
            var mockCommand = Substitute.For<IDbCommand>();
            var mockParameters = new List<IDbDataParameter>();
            var commandText = "";

            mockConnection.CreateCommand().Returns(mockCommand);
            mockCommand.When(cmd => cmd.CommandText = Arg.Any<string>())
                       .Do(callInfo => commandText = callInfo.Arg<string>());

            mockCommand.CreateParameter().Returns(callInfo =>
            {
                var parameter = Substitute.For<IDbDataParameter>();
                parameter.When(p => p.Value = Arg.Any<object>())
                        .Do(ci => mockParameters.Add(parameter));
                return parameter;
            });

            var builder = new SqlQueryBuilder("baseTable");

            // Adding joins with different tables
            builder.AddColumnSelection(new ColumnSelection(TableName: "joinTable1", ColumnName: "baseTableId", Alias: "joinTable1Id"));
            builder.AddColumnSelection(new ColumnSelection(TableName: "joinTable2", ColumnName: "baseTableId", Alias: "joinTable2Id"));
            builder.AddColumnSelection(new ColumnSelection(TableName: "joinTable3", ColumnName: "baseTableId", Alias: "joinTable3Id"));

            // Adding various filters
            builder.AddFilter(new Filter(
                tableName: "baseTable",
                columnName: "Name",
                columnType: ColumnType.String,
                filterType: FilterType.Equals,
                value: "TestName"
            ));
            builder.AddFilter(new Filter(
                tableName: "joinTable1",
                columnName: "Age",
                columnType: ColumnType.Numeric,
                filterType: FilterType.Greater,
                value: 30
            ));
            builder.AddFilter(new Filter(
                tableName: "joinTable2",
                columnName: "StartDate",
                columnType: ColumnType.Date,
                filterType: FilterType.LesserOrEqual,
                value: new DateTime(2023, 1, 1)
            ));

            builder.AddJoin("joinTable1", JoinType.Inner, "baseTableId", "baseTableId");
            builder.AddJoin("joinTable2", JoinType.Left, "baseTableId", "baseTableId");
            builder.AddJoin("joinTable3", JoinType.Right, "baseTableId", "baseTableId");

            // Construct the expected SQL query string
            var expectedQuery = "SELECT " +
                                "joinTable1.baseTableId AS joinTable1Id, " +
                                "joinTable2.baseTableId AS joinTable2Id, " +
                                "joinTable3.baseTableId AS joinTable3Id " +
                                "FROM baseTable " +
                                "INNER JOIN joinTable1 ON baseTable.baseTableId = joinTable1.baseTableId " +
                                "LEFT JOIN joinTable2 ON baseTable.baseTableId = joinTable2.baseTableId " +
                                "RIGHT JOIN joinTable3 ON baseTable.baseTableId = joinTable3.baseTableId " +
                                "WHERE baseTable.Name = @param0 AND joinTable1.Age > @param1 AND joinTable2.StartDate <= @param2";

            var expectedQueryParams = new Dictionary<string, object>
                {
                    // Populate with expected parameters and their values
                    { "@param0", "TestName" },
                    { "@param1", 30 },
                    { "@param2", new DateTime(2023, 1, 1) }
                };

            // Act
            var query = builder.BuildQuery(out var queryParams);
            ExecuteQuery(query, mockConnection, queryParams);

            // Assert
            Assert.Equal(expectedQuery, query);

            // Assert that the captured parameters match the expected parameters
            foreach (var expectedParam in queryParams)
            {
                var param = mockParameters.FirstOrDefault(p => p.ParameterName == expectedParam.Key);
                Assert.NotNull(param);
                Assert.Equal(expectedParam.Value, param.Value);
            }
        }

        [Fact]
        public void BuildQuery_WithoutJoins_OmitsTableNameInSelect()
        {
            // Arrange
            var builder = new SqlQueryBuilder("baseTable");
            builder.AddColumnSelection(new ColumnSelection("baseTable", "ColumnName1", null));
            builder.AddColumnSelection(new ColumnSelection("baseTable", "ColumnName2", null));
            // ... Add filters or other clauses as needed ...

            var expectedQuery = "SELECT ColumnName1, ColumnName2 FROM baseTable "; // No table name prefix

            // Act
            var query = builder.BuildQuery(out var queryParams);

            // Assert
            Assert.Equal(expectedQuery, query);
        }

        [Fact]
        public void BuildQuery_WithJoins_IncludesTableNameInSelect()
        {
            // Arrange
            var builder = new SqlQueryBuilder("baseTable");
            builder.AddJoin("joinTable", JoinType.Inner, "baseTableId", "joinTableId");
            builder.AddColumnSelection(new ColumnSelection("baseTable", "ColumnName1", null));
            builder.AddColumnSelection(new ColumnSelection("joinTable", "ColumnName2", null));

            var expectedQuery = "SELECT baseTable.ColumnName1, joinTable.ColumnName2 FROM baseTable INNER JOIN joinTable ON baseTable.baseTableId = joinTable.joinTableId ";

            // Act
            var query = builder.BuildQuery(out var queryParams);

            // Assert
            Assert.Equal(expectedQuery, query);
        }


        private void AddBetweenFilter(SqlQueryBuilder builder, ColumnType columnType, object startValue, object endValue)
        {
            /*
            var filter = new Filter
            {
                SecondValue = ConvertValue(columnType, endValue),
                Value = ConvertValue(columnType, startValue),
                TableName = "baseTable",
                ColumnName = "TestColumn",
                ColumnType = columnType,
                FilterType = FilterType.Between,
            };
            */
            var filter = new Filter(
                secondValue: ConvertValue(columnType, endValue),
                value: ConvertValue(columnType, startValue),
                tableName: "baseTable",
                columnName: "TestColumn",
                columnType: columnType,
                filterType: FilterType.Between);
            
            builder.AddFilter(filter);
        }
        
        private object ConvertValue(ColumnType columnType, object value)
        {
            return columnType switch
            {
                ColumnType.Date => ConvertToDate(value),
                ColumnType.Numeric => Convert.ToDecimal(value),
                _ => throw new ArgumentException("Invalid column type for Between filter.")
            };
        }

        private DateTime ConvertToDate(object value)
        {
            if (value is string stringValue)
            {
                return DateTime.Parse(stringValue); // Assumes the date is in a correct and parseable format
            }
            else if (value is DateTime dateTimeValue)
            {
                return dateTimeValue;
            }

            throw new ArgumentException("Invalid value type for Date column.");
        }

        private SqlQueryBuilder CreateQueryBuilderWithFilter(ColumnType columnType, FilterType filterType, object value)
        {
            var builder = new SqlQueryBuilder("baseTable");
            builder.AddFilter(new Filter(
                tableName: "baseTable",
                columnName: "TestColumn",
                columnType: columnType,
                filterType: filterType,
                value: value
            ));

            return builder;
        }

        private void ExecuteQuery(string query, IDbConnection connection, Dictionary<string, object> queryParams)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                foreach (var param in queryParams)
                {
                    var dbParameter = command.CreateParameter();
                    dbParameter.ParameterName = param.Key;
                    dbParameter.Value = param.Value;
                    command.Parameters.Add(dbParameter);
                }

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
