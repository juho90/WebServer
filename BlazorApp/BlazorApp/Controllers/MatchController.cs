using CommonLibrary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlazorApp.Controllers
{
    [ApiController]
    [Route("api/match")]
    [Authorize]
    public class MatchController(RoomMatchService roomMatchService) : ControllerBase
    {
        private readonly RoomMatchService roomMatchService = roomMatchService;

        [HttpPost("matching")]
        public async Task<IActionResult> Matching([FromQuery] string region, [FromQuery] int capacity, [FromQuery] int mmr)
        {
            var uid = User.Identity?.Name!;
            try
            {
                await roomMatchService.Enqueue(uid, region, capacity, mmr);
                return Ok(new
                {
                    enqueued = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpGet("matching-status")]
        public async Task<IActionResult> MatchingStatus()
        {
            var uid = User.Identity?.Name!;
            (var isMatching, var enqueuedAt) = await roomMatchService.IsMatching(uid);
            if (isMatching)
            {
                return Ok(new
                {
                    isMatching,
                    enqueuedAt
                });
            }
            else
            {
                return Ok(new
                {
                    isMatching = false
                });
            }
        }

        [HttpGet("room-id")]
        public async Task<IActionResult> GetRoomId()
        {
            var uid = User.Identity?.Name!;
            var roomId = await roomMatchService.GetRoomId(uid);
            return Ok(new
            {
                roomId
            });
        }
    }
}
