using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

[SwaggerSchema("待辦事項模型", Description = "待辦事項的model，包含ID、名稱、完成狀態、創建時間和更新時間")]
public class TodoItem
{
    [Key]
    [SwaggerSchema("待辦事項 ID")]
    public int Id { get; set; }

    [SwaggerSchema("待辦事項")]
    public string? Title { get; set; }

    [SwaggerSchema("是否已完成")]
    public int IsDone { get; set; }

    [SwaggerSchema("創建時間")]
    public DateTime CreatedTime { get; set; }

    [SwaggerSchema("更新時間")]
    public DateTime UpdatedTime { get; set; }

    [SwaggerSchema("lock GUID")]
    public String GUID { get; set; }
}
