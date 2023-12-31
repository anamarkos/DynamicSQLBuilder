
using Microsoft.Data.SqlClient;

namespace DynamicSQLQueryBuilder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Example usage:
            var builder = new SqlQueryBuilder("baseTable");

            // Add column selections
            builder.AddColumnSelection(new ColumnSelection(TableName: "baseTable", ColumnName :"column1", Alias :"BaseColumn1"));
            builder.AddColumnSelection(new ColumnSelection(TableName: "joinTable1", ColumnName: "column2", Alias: "JoinedColumn2"));

            // Add string filter
            builder.AddFilter(new Filter(
                tableName: "joinTable1",
                columnName: "column1",
                columnType: ColumnType.String,
                filterType: FilterType.Contains,
                value: "test"
            ));

            // Add date filter
            builder.AddFilter(new Filter(
                tableName: "baseTable",
                columnName: "dateColumn",
                columnType: ColumnType.Date,
                filterType: FilterType.Greater,
                value: new DateTime(2023, 1, 1) // Example date: January 1, 2023
            ));

            // Add a 'greater or equal' date filter
            builder.AddFilter(new Filter(
                tableName: "baseTable",
                columnName: "dateColumn",
                columnType: ColumnType.Date,
                filterType: FilterType.GreaterOrEqual,
                value: new DateTime(2023, 1, 1)
            ));

            // Add a 'not equal' numeric filter
            builder.AddFilter(new Filter(
                tableName: "joinTable2",
                columnName: "numericColumn",
                columnType: ColumnType.Numeric,
                filterType: FilterType.NotEqual,
                value: 100
            ));

            builder.AddFilter(new Filter(
                tableName: "yourTable",
                columnName: "yourColumn",
                columnType: ColumnType.String,
                filterType: FilterType.Contains,
                value: "%searchString%"
            ));


            Dictionary<string, object> queryParams;
            string queryString = builder.BuildQuery(out queryParams);

            using (var connection = new SqlConnection("connectionString"))
            {
                using (var command = new SqlCommand(queryString, connection))
                {
                    foreach (var param in queryParams)
                    {
                        // Get the filter corresponding to the parameter
                        var filter = builder.Filters.FirstOrDefault(f => f.Placeholder == param.Key);

                        // Use the sanitized value for 'Contains' filters
                        var value = (filter != null && filter._filterType == FilterType.Contains)
                                    ? filter.SanitizeValueForLike()
                                    : param.Value;

                        command.Parameters.AddWithValue(param.Key, value);
                    }

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Process data
                        }
                    }
                }
            }
        }
    }
}