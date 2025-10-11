using System.ComponentModel;
using Microsoft.SemanticKernel;
using PersonalAssistantAI.Models;
using PersonalAssistantAI.Services;

namespace PersonalAssistantAI.Plugins;

public class TaskPlugin
{
    [KernelFunction]
    [Description("Create a new task with the given description")]
    public async Task<string> AddTask(string description)
    {
        var task = new TaskItem
        {
            Description = description,
            CreatedDate = DateTime.Now,
            IsCompleted = false
        };

        // Load existing tasks
        var tasks = FileService.LoadFromFile<List<TaskItem>>("Data/tasks.json") ?? new List<TaskItem>();

        // Add new task
        tasks.Add(task);

        // Save back to file
        FileService.SaveToFile("Data/tasks.json", tasks);

        return $"Task added: {description}";
    }

    [KernelFunction]
    [Description("Get all saved tasks from storage")]
    public async Task<string> GetTask()
    {
        var tasks = FileService.LoadFromFile<IEnumerable<TaskItem>>("Data/tasks.json");
        if (!tasks.Any())
            return "No tasks found.";

        return string.Join("\n", tasks.Select(t => $"- {t.Description} (Created: {t.CreatedDate})"));
    }

    [KernelFunction]
    [Description("Mark a specific task as completed by its ID")]
    public async Task<string> CompleteTask(int taskId)
    {
        var tasks = FileService.LoadFromFile<List<TaskItem>>("Data/tasks.json") ?? new List<TaskItem>();

        var task = tasks.FirstOrDefault(a => a.Id == taskId);
        if (task == null)
            return $"Task with ID {taskId} not found.";

        task.IsCompleted = true;
        FileService.SaveToFile("Data/tasks.json", tasks);

        return $"Task completed: {task.Description}";
    }
}