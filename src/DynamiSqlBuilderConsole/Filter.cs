namespace DynamicSQLQueryBuilder
{
    public class Filter
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public ColumnType _columnType { get; set; }
        public FilterType _filterType { get; set; }
        public object Value { get; set; }
        public object SecondValue { get; set; } // For 'Between' filter type
        public string Placeholder { get; private set; }
        public string SecondPlaceholder { get; private set; }
        
        private static int globalFilterIndex = 0;

        public ColumnType ColumnType
        {
            get => _columnType;
            set
            {
                _columnType = value;
            }
        }

        public FilterType FilterType
        {
            get => _filterType;
            set
            {
                _filterType = value;
                Placeholder = $"@param{globalFilterIndex++}";
                if (_filterType == FilterType.Between)
                {
                    SecondPlaceholder = $"@param{globalFilterIndex++}";
                }
            }
        }
        private void ValidateFilterTypeCompatibility()
        {
            bool isInvalidCombination;

            if (FilterType == FilterType.Between)
            {
                if ((ColumnType != ColumnType.Numeric && ColumnType != ColumnType.Date) || SecondValue == null)
                {
                    throw new InvalidOperationException("The 'Between' filter type requires two numeric or date values.");
                }
            }

            switch (ColumnType)
            {
                case ColumnType.String:
                    // For String, only Equals, NotEqual, Contains, StartsWith, and EndsWith are valid
                    isInvalidCombination = !(_filterType == FilterType.Equals ||
                                             _filterType == FilterType.NotEqual ||
                                             _filterType == FilterType.Contains ||
                                             _filterType == FilterType.StartsWith ||
                                             _filterType == FilterType.EndsWith);
                    break;

                case ColumnType.Numeric:
                    // For Numeric, Between is valid along with all comparison operators
                    isInvalidCombination = !(_filterType == FilterType.Equals ||
                                             _filterType == FilterType.NotEqual ||
                                             _filterType == FilterType.Greater ||
                                             _filterType == FilterType.Lesser ||
                                             _filterType == FilterType.GreaterOrEqual ||
                                             _filterType == FilterType.LesserOrEqual ||
                                             _filterType == FilterType.Between);
                    break;

                case ColumnType.Date:
                    // For Date, Between is valid along with all comparison operators
                    isInvalidCombination = !(_filterType == FilterType.Equals ||
                                             _filterType == FilterType.NotEqual ||
                                             _filterType == FilterType.Greater ||
                                             _filterType == FilterType.Lesser ||
                                             _filterType == FilterType.GreaterOrEqual ||
                                             _filterType == FilterType.LesserOrEqual ||
                                             _filterType == FilterType.Between);
                    break;

                default:
                    throw new NotImplementedException("Unsupported column type.");
            }

            if (isInvalidCombination)
            {
                throw new InvalidOperationException($"Invalid combination of column type {_columnType} and filter type {_filterType}.");
            }
        }

        public Filter(string tableName, string columnName, ColumnType columnType, FilterType filterType, object value, object secondValue = null)
        {
            TableName = tableName;
            FilterType = filterType; // This will initialize placeholders correctly
            ColumnName = columnName;
            ColumnType = columnType;
            Value = value;
            SecondValue = secondValue;
            ValidateFilterTypeCompatibility();
        }
        
        public string GetSqlCondition()
        {
            switch (FilterType)
            {
                case FilterType.Equals:
                    return $"{TableName}.{ColumnName} = {Placeholder}";
                case FilterType.NotEqual:
                    return $"{TableName}.{ColumnName} <> {Placeholder}";
                case FilterType.Contains:
                    // Assumes '%value%' will be set in the Value property itself
                    return $"{TableName}.{ColumnName} LIKE {Placeholder}";
                case FilterType.StartsWith:
                    // Assumes 'value%' will be set in the Value property itself
                    return $"{TableName}.{ColumnName} LIKE {Placeholder}";
                case FilterType.EndsWith:
                    // Assumes '%value' will be set in the Value property itself
                    return $"{TableName}.{ColumnName} LIKE {Placeholder}";
                case FilterType.Greater:
                    return $"{TableName}.{ColumnName} > {Placeholder}";
                case FilterType.Lesser:
                    return $"{TableName}.{ColumnName} < {Placeholder}";
                case FilterType.GreaterOrEqual:
                    return $"{TableName}.{ColumnName} >= {Placeholder}";
                case FilterType.LesserOrEqual:
                    return $"{TableName}.{ColumnName} <= {Placeholder}";
                case FilterType.Between:
                    if (ColumnType == ColumnType.Date || ColumnType == ColumnType.Numeric)
                    {
                        return $"{TableName}.{ColumnName} BETWEEN {Placeholder} AND {SecondPlaceholder}";
                    }
                    throw new InvalidOperationException("Between filter is only valid for Date and Numeric types.");

                default:
                    throw new NotImplementedException("Filter type not supported.");
            }
        }

        public string FormatValueBasedOnType()
        {
            switch (_columnType)
            {
                case ColumnType.String:
                    var formattedString = Value.ToString();
                    // Sanitize for LIKE operator if it's a Contains filter
                    if (_filterType == FilterType.Contains)
                    {
                        formattedString = SanitizeValueForLike();
                    }
                    // Enclose strings in single quotes
                    return $"'{formattedString}'";

                case ColumnType.Date:
                    // Format dates in a standard format enclosed in single quotes
                    return $"'{((DateTime)Value).ToString("yyyy-MM-dd")}'";

                case ColumnType.Numeric:
                    return Value.ToString();

                default:
                    throw new NotImplementedException("Unsupported column type.");
            }
        }

        private string GetSqlOperator(FilterType filterType)
        {
            return filterType switch
            {
                FilterType.Greater => ">",
                FilterType.Lesser => "<",
                FilterType.Equals => "=",
                FilterType.GreaterOrEqual => ">=",
                FilterType.LesserOrEqual => "<=",
                FilterType.NotEqual => "<>",
                _ => throw new ArgumentException("Invalid filter type")
            };
        }

        public string SanitizeValueForLike()
        {
            if (_filterType == FilterType.Contains && Value is string stringValue)
            {
                // Escaping '%' and '_' characters in the string
                return stringValue.Replace("%", "[%]").Replace("_", "[_]");
            }

            return Value.ToString();
        }

        public static void ResetGlobalFilterIndex()
        {
            globalFilterIndex = 0;
        }
    }

    public enum JoinType
    {
        Inner,
        Left,
        Right,
        Full
    }

    public enum ColumnType
    {
        String,
        Date,
        Numeric
    }

    public enum FilterType
    {
        Greater,
        Lesser,
        Contains,
        Equals,
        GreaterOrEqual,
        LesserOrEqual,
        NotEqual,
        Between,
        StartsWith,
        EndsWith,
    }

}
