using DynamicSQLQueryBuilder;

namespace DynamiSqlBuilderConsole
{
    public class JoinInfo
    {
        public string JoinedTableName { get; set; }
        public JoinType JoinType { get; set; }
        public string BaseColumnName { get; set; }
        public string JoinedColumnName { get; set; }

        public JoinInfo(string joinedTableName, JoinType joinType, string baseColumnName, string joinedColumnName)
        {
            JoinedTableName = joinedTableName;
            JoinType = joinType;
            BaseColumnName = baseColumnName;
            JoinedColumnName = joinedColumnName;
        }
    }
}
