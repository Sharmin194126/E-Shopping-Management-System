using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using System.Security.Claims;

namespace E_ShoppingManagement.Controllers
{
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> React(int reviewId, string reactionType)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return NotFound();

            var existingReaction = await _context.ReviewReactions
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

            if (existingReaction != null)
            {
                if (existingReaction.ReactionType == reactionType)
                {
                    _context.ReviewReactions.Remove(existingReaction);
                }
                else
                {
                    existingReaction.ReactionType = reactionType;
                    existingReaction.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                _context.ReviewReactions.Add(new ReviewReaction
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Reply(int reviewId, string replyText)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(replyText)) return BadRequest();

            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return NotFound();

            var reply = new ReviewReply
            {
                ReviewId = reviewId,
                UserId = userId,
                ReplyText = replyText,
                CreatedAt = DateTime.UtcNow,
                IsSeller = User.IsInRole("Admin") || User.IsInRole("Employee")
            };

            _context.ReviewReplies.Add(reply);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
