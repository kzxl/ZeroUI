using System;
using System.Collections.Generic;
using System.Text;
using ZeroUI.Core.Localization;

namespace ZeroUI.Core.Data
{
    public enum FilterGroupOperator
    {
        And,
        Or,
        NotAnd,
        NotOr
    }

    public enum FilterComparisonOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Between,
        IsNull,
        IsNotNull
    }

    public interface IFilterCriteriaNode
    {
        string ToDisplayString();
        string ToSqlWhere();
    }

    public class ConditionFilterNode : IFilterCriteriaNode
    {
        public string FieldName { get; set; } = string.Empty;
        public FilterComparisonOperator Operator { get; set; } = FilterComparisonOperator.Equals;
        public string Value { get; set; } = string.Empty;

        public ConditionFilterNode() { }

        public ConditionFilterNode(string fieldName, FilterComparisonOperator op, string value)
        {
            FieldName = fieldName;
            Operator = op;
            Value = value;
        }

        public string ToDisplayString()
        {
            return $"[{FieldName}] {Operator} '{Value}'";
        }

        public string ToSqlWhere()
        {
            return Operator switch
            {
                FilterComparisonOperator.Equals => $"[{FieldName}] = '{Value}'",
                FilterComparisonOperator.NotEquals => $"[{FieldName}] <> '{Value}'",
                FilterComparisonOperator.GreaterThan => $"[{FieldName}] > '{Value}'",
                FilterComparisonOperator.GreaterThanOrEqual => $"[{FieldName}] >= '{Value}'",
                FilterComparisonOperator.LessThan => $"[{FieldName}] < '{Value}'",
                FilterComparisonOperator.LessThanOrEqual => $"[{FieldName}] <= '{Value}'",
                FilterComparisonOperator.Contains => $"[{FieldName}] LIKE '%{Value}%'",
                FilterComparisonOperator.StartsWith => $"[{FieldName}] LIKE '{Value}%'",
                FilterComparisonOperator.EndsWith => $"[{FieldName}] LIKE '%{Value}'",
                FilterComparisonOperator.IsNull => $"[{FieldName}] IS NULL",
                FilterComparisonOperator.IsNotNull => $"[{FieldName}] IS NOT NULL",
                _ => $"[{FieldName}] = '{Value}'"
            };
        }
    }

    public class GroupFilterNode : IFilterCriteriaNode
    {
        public FilterGroupOperator Operator { get; set; } = FilterGroupOperator.And;
        public List<IFilterCriteriaNode> Children { get; } = new List<IFilterCriteriaNode>();

        public GroupFilterNode() { }

        public GroupFilterNode(FilterGroupOperator op)
        {
            Operator = op;
        }

        public void AddCondition(string fieldName, FilterComparisonOperator op, string value)
        {
            Children.Add(new ConditionFilterNode(fieldName, op, value));
        }

        public GroupFilterNode AddGroup(FilterGroupOperator op)
        {
            var grp = new GroupFilterNode(op);
            Children.Add(grp);
            return grp;
        }

        public string ToDisplayString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            string joiner = Operator == FilterGroupOperator.Or ? " OR " : " AND ";
            for (int i = 0; i < Children.Count; i++)
            {
                if (i > 0) sb.Append(joiner);
                sb.Append(Children[i].ToDisplayString());
            }
            sb.Append(")");
            return sb.ToString();
        }

        public string ToSqlWhere()
        {
            if (Children.Count == 0) return "1=1";
            var sb = new StringBuilder();
            sb.Append("(");
            string joiner = Operator == FilterGroupOperator.Or ? " OR " : " AND ";
            for (int i = 0; i < Children.Count; i++)
            {
                if (i > 0) sb.Append(joiner);
                sb.Append(Children[i].ToSqlWhere());
            }
            sb.Append(")");
            return sb.ToString();
        }
    }

    public static class FilterCriteriaExtensions
    {
        public static string GetLocalizedName(this FilterGroupOperator op)
        {
            return op switch
            {
                FilterGroupOperator.And => ZeroLocalizer.GetString(ZeroStringId.FilterOpAnd),
                FilterGroupOperator.Or => ZeroLocalizer.GetString(ZeroStringId.FilterOpOr),
                FilterGroupOperator.NotAnd => ZeroLocalizer.GetString(ZeroStringId.FilterOpNotAnd),
                FilterGroupOperator.NotOr => ZeroLocalizer.GetString(ZeroStringId.FilterOpNotOr),
                _ => op.ToString().ToUpperInvariant()
            };
        }

        public static string GetLocalizedName(this FilterComparisonOperator op)
        {
            return op switch
            {
                FilterComparisonOperator.Equals => ZeroLocalizer.GetString(ZeroStringId.FilterEquals),
                FilterComparisonOperator.NotEquals => ZeroLocalizer.GetString(ZeroStringId.FilterNotEquals),
                FilterComparisonOperator.Contains => ZeroLocalizer.GetString(ZeroStringId.FilterContains),
                FilterComparisonOperator.StartsWith => ZeroLocalizer.GetString(ZeroStringId.FilterStartsWith),
                FilterComparisonOperator.EndsWith => ZeroLocalizer.GetString(ZeroStringId.FilterEndsWith),
                FilterComparisonOperator.GreaterThan => ZeroLocalizer.GetString(ZeroStringId.FilterGreaterThan),
                FilterComparisonOperator.LessThan => ZeroLocalizer.GetString(ZeroStringId.FilterLessThan),
                FilterComparisonOperator.IsNull => ZeroLocalizer.GetString(ZeroStringId.FilterIsNull),
                FilterComparisonOperator.IsNotNull => ZeroLocalizer.GetString(ZeroStringId.FilterIsNotNull),
                _ => op.ToString()
            };
        }
    }
}
