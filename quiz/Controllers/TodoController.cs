

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Annotations;

namespace quiz.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly RedisCacheService _redisService;
        private readonly TodoContext _todoContext;

        private readonly IDatabase _redis;


        public TodoController(IConnectionMultiplexer connectionMultiplexer, RedisCacheService redisService, TodoContext todoContext)
        {
            _redisService = redisService;
            _todoContext = todoContext;
            _redis = connectionMultiplexer.GetDatabase();
        }

        [HttpGet("{id}")]
        [SwaggerOperation("查詢指定id的待辦事項內容")]
        [SwaggerResponse(200, Description = "查詢成功")]
        [SwaggerResponse(404, Description = "查無此id的待辦事項")]
        public async Task<IActionResult> GetData(int id)
        {
            var cachedData = await _redisService.GetAsync(id.ToString());
            if (cachedData != null)
            {
                return Ok(new { source = "cache hit", data = JsonConvert.DeserializeObject<TodoItem>(cachedData) });
            }

            var dataFromDb = await _todoContext.Set<TodoItem>().FindAsync(id);
            if (dataFromDb == null) return NotFound("查無此id的待辦事項");

            var json = JsonConvert.SerializeObject(dataFromDb);
            await _redisService.SetAsync(id.ToString(), json); // 重新設定 TTL = 3 秒

            return Ok(new { source = "cache missing", data = dataFromDb });
        }

        [HttpPost]
        [SwaggerOperation("新增待辦事項")]
        [SwaggerResponse(201, Description = "新增成功")]
        [SwaggerResponse(400, "data被鎖住了")]
        public async Task<IActionResult> CreateData([FromBody, SwaggerParameter("要新增的代辦事項內容")] TodoItem newItem)
        {
            string guid = Guid.NewGuid().ToString();
            var available = await _redis.StringSetAsync("lock_" + guid, true, TimeSpan.FromSeconds(10), When.NotExists);
            if (available)
            {
                newItem.CreatedTime = DateTime.Now;
                newItem.UpdatedTime = DateTime.Now;
                newItem.GUID = guid;
                _todoContext.Add(newItem);
                int newItemId = await _todoContext.SaveChangesAsync();

                var json = JsonConvert.SerializeObject(newItem);
                await _redisService.SetAsync(newItemId.ToString(), json); // 設定 TTL = 3 秒
                return CreatedAtAction(nameof(GetData), new { id = newItem.Id }, newItem);
            }
            else
            {
                return BadRequest($"data:{guid}被鎖住了");
            }

        }

        [HttpPut]
        [SwaggerOperation("更新指定id的待辦事項內容")]
        [SwaggerResponse(200, Description = "更新成功")]
        [SwaggerResponse(404, Description = "查無此id的待辦事項")]
        public async Task<IActionResult> UpdateData([FromBody] TodoItem updatedData)
        {
            var data = await _todoContext.Set<TodoItem>().FindAsync(updatedData.Id);
            if (data == null) return NotFound("查無此id的待辦事項");
            var available = await _redis.StringSetAsync("lock_" + data.GUID, true, TimeSpan.FromSeconds(10), When.NotExists);
            if (available)
            {
                // 更新資料
                data.Title = updatedData.Title;
                data.IsDone = updatedData.IsDone;
                data.UpdatedTime = DateTime.Now;
                await _todoContext.SaveChangesAsync();

                // 同步更新 Redis
                var json = JsonConvert.SerializeObject(data);
                await _redisService.SetAsync(updatedData.Id.ToString(), json); // 重新設定 TTL = 3 秒

                return Ok(new { returnMsg = "更新成功", data = data });
            }
            else
            {
                return BadRequest("data:" + data.GUID + "被鎖住了");
            }
        }
    }

}