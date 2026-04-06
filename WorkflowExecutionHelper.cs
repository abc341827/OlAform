using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OlAform
{
    internal static class WorkflowExecutionHelper
    {
        public static Dictionary<string, int> BuildStepIndex(IReadOnlyList<ScriptAction> actions)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < actions.Count; index++)
            {
                var key = GetStepKey(actions[index]);
                if (!string.IsNullOrWhiteSpace(key) && !result.ContainsKey(key))
                {
                    result[key] = index;
                }
            }

            return result;
        }

        public static string ResolveValue(string? template, IReadOnlyDictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return Regex.Replace(template, "\\$\\{(?<name>[^}]+)\\}", match =>
            {
                var name = match.Groups["name"].Value;
                return variables.TryGetValue(name, out var value) ? value : string.Empty;
            });
        }

        public static void SetVariable(IDictionary<string, string> variables, string? name, string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            variables[name] = value ?? string.Empty;
        }

        public static void SetPointVariables(IDictionary<string, string> variables, string? name, Point point)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            variables[name] = $"{point.X},{point.Y}";
            variables[$"{name}.X"] = point.X.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Y"] = point.Y.ToString(CultureInfo.InvariantCulture);
        }

        public static void SetMatchResultVariables(IDictionary<string, string> variables, string? name, MatchResult result)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            variables[name] = result.MatchState ? $"{result.X},{result.Y},{result.Width},{result.Height}" : string.Empty;
            variables[$"{name}.MatchState"] = result.MatchState.ToString();
            variables[$"{name}.X"] = result.X.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Y"] = result.Y.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Width"] = result.Width.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Height"] = result.Height.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Score"] = result.MatchVal.ToString(CultureInfo.InvariantCulture);
        }

        public static bool EvaluateCondition(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var left = ResolveValue(action.ConditionLeft, variables);
            var right = ResolveValue(action.ConditionRight, variables);

            return action.ConditionOperator switch
            {
                ConditionOperatorType.Equals => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.NotEquals => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.Contains => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.NotContains => !left.Contains(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.StartsWith => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.EndsWith => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.GreaterThan => CompareAsNumber(left, right) > 0,
                ConditionOperatorType.GreaterThanOrEqual => CompareAsNumber(left, right) >= 0,
                ConditionOperatorType.LessThan => CompareAsNumber(left, right) < 0,
                ConditionOperatorType.LessThanOrEqual => CompareAsNumber(left, right) <= 0,
                ConditionOperatorType.IsEmpty => string.IsNullOrWhiteSpace(left),
                ConditionOperatorType.IsNotEmpty => !string.IsNullOrWhiteSpace(left),
                _ => false
            };
        }

        public static int ResolveTargetIndex(ScriptAction action, IReadOnlyDictionary<string, string> variables, IReadOnlyDictionary<string, int> stepIndex)
        {
            var target = ResolveValue(action.TargetStep, variables);
            if (string.IsNullOrWhiteSpace(target) || !stepIndex.TryGetValue(target, out var index))
            {
                throw new InvalidOperationException($"未找到目标步骤: {action.TargetStep}");
            }

            return index;
        }

        public static long ParseWindowHandle(string input)
        {
            var value = input.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("请输入外部窗口句柄。支持十进制或 0x 开头的十六进制。");
            }

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                {
                    return hexValue;
                }
            }

            if (long.TryParse(value, out var decimalValue))
            {
                return decimalValue;
            }

            if (long.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexWithoutPrefix))
            {
                return hexWithoutPrefix;
            }

            throw new InvalidOperationException("窗口句柄格式不正确，请输入十进制或十六进制句柄值。");
        }

        public static bool RequiresBoundWindow(ActionType actionType)
        {
            return actionType is
                ActionType.BindWindow or
                ActionType.MouseMove or
                ActionType.LeftClick or
                ActionType.LeftDoubleClick or
                ActionType.LeftDown or
                ActionType.LeftUp or
                ActionType.MouseDrag or
                ActionType.RightClick or
                ActionType.RightDown or
                ActionType.RightUp or
                ActionType.MiddleClick or
                ActionType.WheelUp or
                ActionType.WheelDown or
                ActionType.KeyPress or
                ActionType.InputText or
                ActionType.SendPaste or
                ActionType.OCR or
                ActionType.FindImage or
                ActionType.ClickImage or
                ActionType.WaitImage or
                ActionType.FindColor or
                ActionType.ClickColor or
                ActionType.WaitColor or
                ActionType.Capture or
                ActionType.WindowActivate or
                ActionType.WindowHide or
                ActionType.WindowShow or
                ActionType.WindowSetSize;
        }

        public static int FindElseOrEndIfIndex(IReadOnlyList<ScriptAction> actions, int ifIndex)
        {
            var depth = 0;
            for (var index = ifIndex + 1; index < actions.Count; index++)
            {
                switch (actions[index].ActionType)
                {
                    case ActionType.If:
                        depth++;
                        break;
                    case ActionType.EndIf when depth == 0:
                    case ActionType.Else when depth == 0:
                        return index;
                    case ActionType.EndIf:
                        depth--;
                        break;
                }
            }

            throw new InvalidOperationException("If 步骤缺少匹配的 Else 或 EndIf。");
        }

        public static int FindEndIfIndex(IReadOnlyList<ScriptAction> actions, int elseIndex)
        {
            var depth = 0;
            for (var index = elseIndex + 1; index < actions.Count; index++)
            {
                switch (actions[index].ActionType)
                {
                    case ActionType.If:
                        depth++;
                        break;
                    case ActionType.EndIf when depth == 0:
                        return index;
                    case ActionType.EndIf:
                        depth--;
                        break;
                }
            }

            throw new InvalidOperationException("Else 步骤缺少匹配的 EndIf。");
        }

        public static int FindMatchingEndLoopIndex(IReadOnlyList<ScriptAction> actions, int loopStartIndex)
        {
            var depth = 0;
            for (var index = loopStartIndex + 1; index < actions.Count; index++)
            {
                switch (actions[index].ActionType)
                {
                    case ActionType.LoopStart:
                        depth++;
                        break;
                    case ActionType.EndLoop when depth == 0:
                        return index;
                    case ActionType.EndLoop:
                        depth--;
                        break;
                }
            }

            throw new InvalidOperationException("Loop Start 步骤缺少匹配的 End Loop。");
        }

        private static string GetStepKey(ScriptAction action)
        {
            return string.IsNullOrWhiteSpace(action.StepId) ? action.Name : action.StepId!;
        }

        private static int CompareAsNumber(string left, string right)
        {
            var leftIsNumber = double.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out var leftNumber);
            var rightIsNumber = double.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rightNumber);

            return leftIsNumber && rightIsNumber
                ? leftNumber.CompareTo(rightNumber)
                : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
