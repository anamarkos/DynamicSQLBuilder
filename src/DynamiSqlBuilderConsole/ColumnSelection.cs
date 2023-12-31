namespace DynamicSQLQueryBuilder
{
    public class ColumnSelection
    {

        public ColumnSelection(string TableName, string ColumnName, string Alias)
        {
            this.TableName = TableName;
            this.ColumnName = ColumnName;
            this.Alias = Alias;
        }

        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string Alias { get; set; }

        public string GetSelection(bool hasJoins, string baseTable)
        {
            //return string.IsNullOrEmpty(Alias)
            //    ? $"{TableName}.{ColumnName}"
            //    : $"{TableName}.{ColumnName} AS {Alias}";

            string fullColumnName = hasJoins || TableName != baseTable
                                    ? $"{TableName}.{ColumnName}"
                                    : ColumnName;

            if (!string.IsNullOrEmpty(Alias))
            {
                fullColumnName += $" AS {Alias}";
            }

            return fullColumnName;
        }
    }
}
