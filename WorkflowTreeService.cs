namespace OlAform
{
    internal static class WorkflowTreeService
    {
        public static bool CanContainChildren(ActionType actionType)
        {
            return actionType is ActionType.If or ActionType.Else or ActionType.LoopStart;
        }

        public static void InsertWorkflowNode(List<WorkflowNode> workflowRoots, WorkflowNode node, TreeNode? targetNode, bool addAsChild)
        {
            if (targetNode?.Tag is not WorkflowNode targetWorkflowNode)
            {
                workflowRoots.Add(node);
                return;
            }

            if (addAsChild && CanContainChildren(targetWorkflowNode.Action.ActionType))
            {
                if (node.Action.ActionType == ActionType.Else && targetWorkflowNode.Action.ActionType != ActionType.If)
                {
                    throw new InvalidOperationException("Else 只能添加到 If 步骤下。");
                }

                if (node.Action.ActionType == ActionType.Else && targetWorkflowNode.Children.Any(c => c.Action.ActionType == ActionType.Else))
                {
                    throw new InvalidOperationException("每个 If 步骤只能包含一个 Else 分支。");
                }

                targetWorkflowNode.Children.Add(node);
                return;
            }

            var siblings = GetSiblingList(workflowRoots, targetNode);
            var index = siblings.IndexOf(targetWorkflowNode);
            siblings.Insert(index + 1, node);
        }

        public static List<WorkflowNode> GetSiblingList(List<WorkflowNode> workflowRoots, TreeNode treeNode)
        {
            return treeNode.Parent?.Tag is WorkflowNode parentNode ? parentNode.Children : workflowRoots;
        }

        public static void MoveWorkflowNode(List<WorkflowNode> workflowRoots, WorkflowNode sourceNode, TreeNode? targetTreeNode)
        {
            if (targetTreeNode?.Tag is WorkflowNode targetNode)
            {
                if (ReferenceEquals(sourceNode, targetNode) || ContainsNode(sourceNode, targetNode))
                {
                    return;
                }
            }

            RemoveWorkflowNode(workflowRoots, sourceNode);

            if (targetTreeNode?.Tag is not WorkflowNode targetWorkflowNode)
            {
                workflowRoots.Add(sourceNode);
                return;
            }

            if (CanContainChildren(targetWorkflowNode.Action.ActionType))
            {
                if (sourceNode.Action.ActionType == ActionType.Else && targetWorkflowNode.Action.ActionType != ActionType.If)
                {
                    throw new InvalidOperationException("Else 只能移动到 If 步骤下。");
                }

                targetWorkflowNode.Children.Add(sourceNode);
                return;
            }

            var siblings = GetSiblingList(workflowRoots, targetTreeNode);
            var targetIndex = siblings.IndexOf(targetWorkflowNode);
            siblings.Insert(targetIndex + 1, sourceNode);
        }

        public static bool RemoveWorkflowNode(ICollection<WorkflowNode> nodes, WorkflowNode target)
        {
            foreach (var node in nodes.ToList())
            {
                if (ReferenceEquals(node, target))
                {
                    nodes.Remove(node);
                    return true;
                }

                if (RemoveWorkflowNode(node.Children, target))
                {
                    return true;
                }
            }

            return false;
        }

        public static TreeNode CreateTreeNode(WorkflowNode node)
        {
            var text = string.IsNullOrWhiteSpace(node.Action.StepId)
                ? node.Action.ToString()
                : $"[{node.Action.StepId}] {node.Action}";

            var treeNode = new TreeNode(text) { Tag = node };
            foreach (var child in node.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            return treeNode;
        }

        public static TreeNode? FindTreeNodeByWorkflowId(TreeNodeCollection nodes, string workflowId)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is WorkflowNode workflowNode && string.Equals(workflowNode.Id, workflowId, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                var childResult = FindTreeNodeByWorkflowId(node.Nodes, workflowId);
                if (childResult is not null)
                {
                    return childResult;
                }
            }

            return null;
        }

        public static List<ScriptAction> FlattenWorkflow(IEnumerable<WorkflowNode> nodes)
        {
            var result = new List<ScriptAction>();
            foreach (var node in nodes)
            {
                FlattenWorkflowNode(node, result);
            }

            return result;
        }

        private static void FlattenWorkflowNode(WorkflowNode node, ICollection<ScriptAction> result)
        {
            switch (node.Action.ActionType)
            {
                case ActionType.If:
                    result.Add(node.Action.Clone());

                    var elseNode = node.Children.FirstOrDefault(c => c.Action.ActionType == ActionType.Else);
                    foreach (var child in node.Children)
                    {
                        if (ReferenceEquals(child, elseNode))
                        {
                            break;
                        }

                        FlattenWorkflowNode(child, result);
                    }

                    if (elseNode is not null)
                    {
                        result.Add(elseNode.Action.Clone());
                        foreach (var child in elseNode.Children)
                        {
                            FlattenWorkflowNode(child, result);
                        }
                    }

                    result.Add(new ScriptAction { Name = "End If", ActionType = ActionType.EndIf, Description = "Auto generated by tree workflow." });
                    break;

                case ActionType.LoopStart:
                    result.Add(node.Action.Clone());
                    foreach (var child in node.Children)
                    {
                        FlattenWorkflowNode(child, result);
                    }

                    result.Add(new ScriptAction { Name = "End Loop", ActionType = ActionType.EndLoop, Description = "Auto generated by tree workflow." });
                    break;

                case ActionType.Else:
                    result.Add(node.Action.Clone());
                    foreach (var child in node.Children)
                    {
                        FlattenWorkflowNode(child, result);
                    }
                    break;

                default:
                    result.Add(node.Action.Clone());
                    break;
            }
        }

        private static bool ContainsNode(WorkflowNode parent, WorkflowNode target)
        {
            foreach (var child in parent.Children)
            {
                if (ReferenceEquals(child, target) || ContainsNode(child, target))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
