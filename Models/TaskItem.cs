namespace PersonalAssistantAI.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsCompleted { get; set; }
}