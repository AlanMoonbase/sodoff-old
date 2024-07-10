using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using sodoff.Configuration;
using sodoff.Model;
using sodoff.Schema;
using sodoff.Util;

namespace sodoff.Services
{
    public class BuddyService
    {
        private readonly DBContext ctx;
        private readonly IOptions<ApiServerConfig> config;
        private readonly MessageService messageService;
        private static readonly char[] BuddyCodeCharList = {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'
        };
        public BuddyService(DBContext ctx, IOptions<ApiServerConfig> config, MessageService messageService)
        {
            this.ctx = ctx;
            this.config = config;
            this.messageService = messageService;
        }

        public BuddyList GetBuddyList(Viking viking)
        {
            List<Schema.Buddy> buddies = new List<Schema.Buddy>();

            List<Model.Buddy> vikingBuddies = ctx.Buddies.Where(e => e.OwnerID == viking.Id).ToList(); // relations aren't right, getting around it

            foreach(Model.Buddy buddy in vikingBuddies)
            {
                buddies.Add(new Schema.Buddy
                {
                    BestBuddy = buddy.BestBuddy,
                    DisplayName = XmlUtil.DeserializeXml<AvatarData>(buddy.Viking.AvatarSerialized).DisplayName,
                    Online = buddy.Viking.Online ?? false,
                    Status = buddy.Status,
                    OnMobile = false,
                    UserID = buddy.Viking.Uid.ToString()
                });
            }

            return new BuddyList { Buddy = buddies.ToArray() };
        }

        public BuddyActionResult AddBuddy(Viking owner, Viking receiver, string apiToken, uint gameVersion)
        {
            // create two relations
            Model.Buddy relation = new Model.Buddy { OwnerID = owner.Id, BuddyID = receiver.Id, Status = BuddyStatus.PendingApprovalFromOther };
            Model.Buddy relation2 = new Model.Buddy { OwnerID = receiver.Id, BuddyID = owner.Id, Status = BuddyStatus.PendingApprovalFromSelf };

            // prevent receiver from receiving request if versions don't match
            if (receiver.GameVersion != owner.GameVersion) return new BuddyActionResult { Result = BuddyActionResultType.InvalidFriendCode };


            if (receiver == owner) return new BuddyActionResult { Result = BuddyActionResultType.CannotAddSelf };
            if (ctx.Buddies.Contains(relation) || ctx.Buddies.Contains(relation2)) return new BuddyActionResult { Result = BuddyActionResultType.AlreadyInList }; // DO NOT ADD IF ALREADY ADDED

            ctx.Buddies.Add(relation);
            ctx.Buddies.Add(relation2);
            ctx.SaveChanges();

            // post buddy request

            messageService.PostBuddyRequest(owner, receiver, apiToken);

            return new BuddyActionResult
            {
                BuddyUserID = receiver.Uid.ToString(),
                Result = BuddyActionResultType.Success,
                Status = relation.Status
            };
        }

        public bool RemoveBuddy(Viking owner, Viking receiver)
        {
            // get buddy relation 1
            Model.Buddy? relation = ctx.Buddies.Where(e => e.OwnerID == owner.Id)
                .FirstOrDefault(e => e.BuddyID == receiver.Id);

            // remove it
            if (relation == null) return false;
            else if (relation.Status == BuddyStatus.BlockedByBoth) relation.Status = BuddyStatus.BlockedByOther;
            else ctx.Buddies.Remove(relation);

            // get buddy relation 2

            Model.Buddy? relation2 = ctx.Buddies.Where(e => e.OwnerID == receiver.Id)
                .FirstOrDefault(e => e.BuddyID == owner.Id);

            // remove it
            if (relation2 == null) return false;
            else if (relation2.Status == BuddyStatus.BlockedByBoth) relation2.Status = BuddyStatus.BlockedBySelf;
            else ctx.Buddies.Remove(relation2);

            ctx.SaveChanges();

            return true;
        }

        public bool IgnoreBuddy(Viking owner, Viking receiver)
        {
            Model.Buddy? existingRelation1 = ctx.Buddies.Where(e => e.OwnerID == owner.Id)
                .FirstOrDefault(e => e.BuddyID == receiver.Id);
            Model.Buddy? existingRelation2 = ctx.Buddies.Where(e => e.OwnerID == receiver.Id)
                .FirstOrDefault(e => e.BuddyID == owner.Id);

            // check if relation exists and if relation1 has relation2 blocked and if relation2 has relation1 blocked
            if (existingRelation1 != null && existingRelation2 != null && existingRelation1.Status == BuddyStatus.BlockedBySelf && existingRelation2.Status == BuddyStatus.BlockedByOther)
            {
                // set both status' to blocked by both
                existingRelation1.Status = BuddyStatus.BlockedByBoth;
                existingRelation2.Status = BuddyStatus.BlockedByBoth;

                ctx.SaveChanges();
                return true;
            }

            // check if relation exists and if relation2 has relation1 blocked and if relation1 has relation2 blocked
            if(existingRelation1 != null && existingRelation2 != null && existingRelation1.Status == BuddyStatus.BlockedByOther && existingRelation2.Status == BuddyStatus.BlockedBySelf)
            {
                // set both status' to blocked by both
                existingRelation1.Status = BuddyStatus.BlockedByBoth;
                existingRelation2.Status = BuddyStatus.BlockedByBoth;

                ctx.SaveChanges();
                return true;
            }

            if (existingRelation1 != null) existingRelation1.Status = BuddyStatus.BlockedBySelf;
            else
            {
                // create relation for block
                Model.Buddy relation = new Model.Buddy { OwnerID = owner.Id, BuddyID = receiver.Id, Status = BuddyStatus.BlockedBySelf };
                ctx.Buddies.Add(relation);
            }

            if (existingRelation2 != null) existingRelation2.Status = BuddyStatus.BlockedByOther;
            else
            {
                Model.Buddy relation = new Model.Buddy { OwnerID = receiver.Id, BuddyID = owner.Id, Status = BuddyStatus.BlockedByOther };
                ctx.Buddies.Add(relation);
            }

            ctx.SaveChanges();
            return true;
        }

        public bool AcceptBuddyRequest(Viking owner, Viking receiver)
        {
            // get buddy relation 1
            Model.Buddy? relation = ctx.Buddies.Where(e => e.OwnerID == owner.Id)
                .FirstOrDefault(e => e.BuddyID == receiver.Id);

            // change status to approved
            if (relation != null) relation.Status = BuddyStatus.Approved;
            else return false;

            // get buddy relation 2

            Model.Buddy? relation2 = ctx.Buddies.Where(e => e.OwnerID == receiver.Id)
                .FirstOrDefault(e => e.BuddyID == owner.Id);

            // change status to approved
            if (relation2 != null) relation2.Status = BuddyStatus.Approved;
            else return false;

            ctx.SaveChanges();

            return true;
        }

        public bool SetBuddyAsBest(Viking viking, Viking vikingToSet, bool best)
        {
            Model.Buddy? buddyToSet = ctx.Buddies.Where(e => e.OwnerID == viking.Id)
                .FirstOrDefault(e => e.BuddyID == vikingToSet.Id);

            if (buddyToSet == null) return false;

            buddyToSet.BestBuddy = best;
            ctx.SaveChanges();

            return true;
        }

        public BuddyLocation GetBuddyLocation(Viking viking, uint gameVersion)
        {
            Model.Buddy? buddy = ctx.Buddies.FirstOrDefault(e => e.BuddyID == viking.Id);
            var location = new BuddyLocation();

            if (viking.CurrentRoomId != null && buddy != null) location = new BuddyLocation
            {
                Server = config.Value.MMOAdress,
                ServerPort = config.Value.MMOPort,
                ServerVersion = "S2X", // always use SmartFox 2X Protocol
                Zone = viking.CurrentRoomName ?? "ZoneNotSet",
                Room = viking.CurrentRoomId.ToString() ?? "RoomNotSet",
                MultiplayerID = 0, // not sure what this is, most likely for minigames
                UserID = viking.Uid.ToString()
            };
            else return new BuddyLocation();

            switch(gameVersion)
            {
                case ClientVersion.WoJS:
                    {
                        location.AppName = "JSMain";
                        break;
                    }
                case ClientVersion.WoJS_NewAvatar:
                    {
                        location.AppName = "JSMain";
                        break;
                    }
                case ClientVersion.WoJS_AdvLand:
                    {
                        location.AppName = "JSAdventureland";
                        break;
                    }
                case ClientVersion.MB:
                    {
                        location.AppName = "MBMain";
                        break;
                    }
            }

            return location;
        }

        public string GetOrSetBuddyCode(Viking viking, string codeOverride = "")
        {
            Random rnd = new Random();

            if (!string.IsNullOrEmpty(codeOverride) && ctx.Vikings.FirstOrDefault(e => e.BuddyCode == codeOverride) == null) viking.BuddyCode = codeOverride;

            if (viking.BuddyCode == null)
            {
                string generatedCode = "";
                do // keep generating codes until a unique one is generated
                {
                    for (var i = 0; i < 5; i++)
                    {
                        generatedCode = generatedCode + BuddyCodeCharList[rnd.Next(0, BuddyCodeCharList.Length)];
                    }
                } while (ctx.Vikings.FirstOrDefault(e => e.BuddyCode == generatedCode) != null);
                viking.BuddyCode = generatedCode;
            }

            ctx.SaveChanges();

            return viking.BuddyCode;
        }
    }
}
