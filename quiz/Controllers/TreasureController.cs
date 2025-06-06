using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Annotations;

[ApiController]
[Route("[controller]")]
public class TreasureController : ControllerBase
{
    private readonly IDatabase _redis;

    public TreasureController(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    [HttpPost("add")]
    [SwaggerOperation(Summary = "更新treasure值", Description = "Treasure值加一")]
    [SwaggerResponse(200, "成功更新treasure")]
    [SwaggerResponse(400, "treasure被鎖住了")]
    [SwaggerResponse(404, "找不到treasure")]

    public async Task<IActionResult> AddTreasure()
    {
        var lockKey = "treasure_lock";

        var available = await _redis.StringSetAsync(lockKey, true, TimeSpan.FromSeconds(10), When.NotExists);

        if (available)
        {
            var treasure = await _redis.StringGetAsync("treasure");
            int newValue = int.Parse(treasure) + 1;
            await _redis.StringSetAsync("treasure", newValue);

            return Ok(new
            {
                resultMsg = "add成功",
                currentTreasure = newValue
            });
        }
        else
        {
            return BadRequest("Treasure被鎖住了");
        }


    }
}
