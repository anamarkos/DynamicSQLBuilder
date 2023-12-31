using DynamiSqlBuilderConsole;
using System.Text;

namespace DynamicSQLQueryBuilder
{
    public class SqlQueryBuilder
    {
        private string baseTable;
        private List<ColumnSelection> columnSelections;
        private List<JoinInfo> joins;
        public List<Filter> Filters;
        private bool hasJoins = false;

        public SqlQueryBuilder(string baseTable)
        {
            this.baseTable = baseTable;
            this.columnSelections = new List<ColumnSelection>();
            this.joins = new List<JoinInfo>();
            this.Filters = new List<Filter>();

        }
        
        public void AddJoin(string joinedTableName, JoinType joinType, string baseColumnName, string joinedColumnName)
        {
            joins.Add(new JoinInfo(joinedTableName, joinType, baseColumnName, joinedColumnName));
            hasJoins = true;
        }

        public void AddColumnSelection(ColumnSelection selection)
        {
            columnSelections.Add(selection);
        }

        public void AddFilter(Filter filter)
        {
            Filters.Add(filter);
        }

        public string BuildQuery(out Dictionary<string, object> queryParams)
        {
            StringBuilder queryBuilder = new StringBuilder();
            string selectClause = columnSelections.Any()
                ? string.Join(", ", columnSelections.Select(c => c.GetSelection(hasJoins: hasJoins, baseTable: baseTable)))
                : "*";

            queryBuilder.Append($"SELECT {selectClause} FROM {baseTable} ");

            foreach (var join in joins)
            {
                string joinTypeString = join.JoinType.ToString().ToUpper();
                queryBuilder.Append($"{joinTypeString} JOIN {join.JoinedTableName} ON {baseTable}.{join.BaseColumnName} = {join.JoinedTableName}.{join.JoinedColumnName} ");
            }

            if (Filters.Any())
            {
                queryBuilder.Append("WHERE ");
                queryBuilder.Append(String.Join(" AND ", Filters.Select(f => f.GetSqlCondition())));
            }

            queryParams = new Dictionary<string, object>();

            foreach (var filter in Filters)
            {
                var formattedValue = filter.FormatValueBasedOnType();
                queryParams.Add(filter.Placeholder, formattedValue);
            }

            return queryBuilder.ToString();
        }
    }

}
