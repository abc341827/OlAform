using System.Text;
using Newtonsoft.Json;

namespace OlAform
{
    internal sealed class ProjectStorage
    {
        private readonly string _projectsDirectory;

        public ProjectStorage(string baseDirectory)
        {
            _projectsDirectory = Path.Combine(baseDirectory, "Projects");
        }

        public void EnsureDirectory()
        {
            Directory.CreateDirectory(_projectsDirectory);
        }

        public IReadOnlyList<string> GetProjectNames()
        {
            EnsureDirectory();

            return Directory.GetFiles(_projectsDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void Save(string projectName, IEnumerable<WorkflowNode> workflowRoots)
        {
            EnsureDirectory();

            var project = new ProjectDefinition
            {
                Name = projectName,
                WorkflowRoots = workflowRoots.Select(ToDto).ToList()
            };

            var json = JsonConvert.SerializeObject(project, Formatting.Indented);
            File.WriteAllText(GetProjectFilePath(projectName), json, Encoding.UTF8);
        }

        public ProjectDefinition Load(string projectName)
        {
            var filePath = GetProjectFilePath(projectName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("项目文件不存在。", filePath);
            }

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<ProjectDefinition>(json)
                ?? throw new InvalidOperationException("项目文件解析失败。");
        }

        public void Delete(string projectName)
        {
            var filePath = GetProjectFilePath(projectName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public List<WorkflowNode> RestoreWorkflow(ProjectDefinition project)
        {
            return project.WorkflowRoots.Select(FromDto).ToList();
        }

        private string GetProjectFilePath(string projectName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(projectName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(safeName))
            {
                throw new InvalidOperationException("项目名称无效。");
            }

            return Path.Combine(_projectsDirectory, $"{safeName}.json");
        }

        private static WorkflowNodeDto ToDto(WorkflowNode node)
        {
            return new WorkflowNodeDto
            {
                Id = node.Id,
                Action = node.Action.Clone(),
                Children = node.Children.Select(ToDto).ToList()
            };
        }

        private static WorkflowNode FromDto(WorkflowNodeDto dto)
        {
            var node = new WorkflowNode
            {
                Id = string.IsNullOrWhiteSpace(dto.Id) ? Guid.NewGuid().ToString("N") : dto.Id,
                Action = dto.Action?.Clone() ?? new ScriptAction()
            };

            foreach (var child in dto.Children ?? new List<WorkflowNodeDto>())
            {
                node.Children.Add(FromDto(child));
            }

            return node;
        }
    }
}
