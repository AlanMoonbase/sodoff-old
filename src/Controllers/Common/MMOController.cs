using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata;
using sodoff.Model;
using sodoff.Schema;
using sodoff.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sodoff.Controllers.Common
{
    public class MMOController : Controller
    {
        private readonly DBContext ctx;
        private readonly BuddyService buddyService;
        private readonly MessageService messageService;
        private readonly MMOCommunicationService mpCommunication;
        public MMOController(DBContext ctx, BuddyService buddyService, MessageService messageService, MMOCommunicationService mMOCommunicationService)
        {
            this.ctx = ctx;
            this.buddyService = buddyService;
            this.messageService = messageService;
            this.mpCommunication = mMOCommunicationService;
        }

        [HttpPost]
        [Produces("application/xml")]
        [Route("MMO/SetBuddyOnline")]
        public IActionResult SetBuddyOnline([FromForm] Guid token, [FromForm] string online)
        {
            Session session = ctx.Sessions.FirstOrDefault(e => e.ApiToken == token);
            Viking vikingToSet = null;

            if (session != null) vikingToSet = session.Viking;

            if (vikingToSet != null)
            {
                vikingToSet.Online = bool.Parse(online);
                ctx.SaveChanges();
                return Ok(true);
            }
            else return Ok(false);
        }

        [HttpPost]
        [Produces("application/xml")]
        [Route("MMO/SetBuddyLocation")]
        public IActionResult SetBuddyLocation([FromForm] Guid token, [FromForm] string roomId, [FromForm] string roomName, [FromForm] string isPrivate)
        {
            Session session = ctx.Sessions.FirstOrDefault(e => e.ApiToken == token);
            Viking vikingToSet = null;

            if (session != null) vikingToSet = session.Viking;

            if (vikingToSet != null)
            {
                if (!string.IsNullOrEmpty(roomId)) vikingToSet.CurrentRoomId = int.Parse(roomId);
                else vikingToSet.CurrentRoomId = 0;
                vikingToSet.CurrentRoomName = roomName;

                vikingToSet.IsCurrentRoomPrivate = bool.Parse(isPrivate);

                ctx.SaveChanges();
                return Ok(true);
            } else return Ok(false);
        }

        [HttpPost]
        [Produces("application/xml")]
        [Route("MMO/SendMessage")]
        public IActionResult SendMessage([FromForm] Guid token, [FromForm] Guid toUserId, [FromForm] string content, [FromForm] int level, [FromForm] int replyMsgId)
        {
            Session? session = ctx.Sessions.FirstOrDefault(e => e.ApiToken == token);
            Viking? viking = null;
            Viking? receivingViking = ctx.Vikings.FirstOrDefault(e => e.Uid == toUserId);

            if (session != null) viking = session.Viking;
            if (viking != null && receivingViking != null)
            {
                if (replyMsgId == 0)
                {
                    messageService.PostTextMessage(viking, receivingViking, content, MessageType.Post, (MessageLevel)level);
                    string newMessageCount = receivingViking.Messages.Where(e => e.IsNew == true && e.FromVikingId != receivingViking.Id && DateTime.UtcNow < e.CreatedAt.AddSeconds(10)).Count().ToString();
                    mpCommunication.SendPacketToPlayer(token.ToString(), receivingViking.Uid.ToString(), "NMP", new string[] { "NMP", "-1", "0", newMessageCount, "null" });

                }
                else 
                { 
                    messageService.ReplyToMessage(replyMsgId, viking, receivingViking, content, MessageType.Post, (MessageLevel)level);
                    string newMessageCount = receivingViking.Messages.Where(e => e.IsNew == true && e.FromVikingId != receivingViking.Id && DateTime.UtcNow < e.CreatedAt.AddSeconds(10)).Count().ToString();
                    mpCommunication.SendPacketToPlayer(token.ToString(), receivingViking.Uid.ToString(), "NMP", new string[] { "NMP", "-1", "0", newMessageCount, "null" });
                }
                return Ok(true);
            }

            return Ok(false);
        }

        [HttpPost]
        [Produces("application/xml")]
        [Route("MMOAdmin/ChangeUserFriendCode")]
        public IActionResult ChangeUserFriendCode([FromForm] Guid token, [FromForm] Guid userId, [FromForm] string code)
        {
            Session? session = ctx.Sessions.FirstOrDefault(e => e.ApiToken == token);
            Viking? viking = ctx.Vikings.FirstOrDefault(e => e.Uid == userId);

            if (session != null && viking != null)
            {
                Role? role = session.Viking.MMORoles.FirstOrDefault()?.Role;
                if (role != null)
                {
                    if(role == Role.Admin)
                    {
                        return Ok(buddyService.GetOrSetBuddyCode(viking, code));
                    } else
                    {
                        return Unauthorized();
                    }
                }
            }

            return BadRequest();
        }
    }
}
