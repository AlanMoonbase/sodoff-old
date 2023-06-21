﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sodoff.Attributes;
using sodoff.Model;
using sodoff.Schema;
using sodoff.Services;
using sodoff.Util;

namespace sodoff.Controllers.Common;
public class ContentController : Controller {

    private readonly DBContext ctx;
    private KeyValueService keyValueService;
    public ContentController(DBContext ctx, KeyValueService keyValueService) {
        this.ctx = ctx;
        this.keyValueService = keyValueService;
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("ContentWebService.asmx/GetDefaultNameSuggestion")]
    public IActionResult GetDefaultNameSuggestion() {
        // TODO:  generate random names, and ensure they aren't already taken
        string[] suggestions = new string[] { "dragon1", "dragon2", "dragon3" };
        return Ok(new DisplayNameUniqueResponse {
            Suggestions = new SuggestionResult {
                Suggestion = suggestions
            }
        });
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/ValidateName")]
    public IActionResult ValidateName([FromForm] string apiToken,[FromForm] string nameValidationRequest) {
        User? user = ctx.Sessions.FirstOrDefault(e => e.ApiToken == apiToken)?.User;
        if (user is null) {
            // TODO: better error handling than just replying not unique
            return Ok(new NameValidationResponse { Result = NameValidationResult.NotUnique });
        }

        // Check if name populated
        NameValidationRequest request = XmlUtil.DeserializeXml<NameValidationRequest>(nameValidationRequest);

        if (request.Category == NameCategory.Default) {
            // This is an avatar we are checking
            // Check if viking exists
            bool exists = ctx.Vikings.Count(e => e.Name == request.Name) > 0;
            NameValidationResult result = exists ? NameValidationResult.NotUnique : NameValidationResult.Ok;
            return Ok(new NameValidationResponse { Result = result});

        } else {
            // TODO: pets, groups, default
            return Ok();
        }
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("ContentWebService.asmx/GetKeyValuePair")]
    public Schema.PairData? GetKeyValuePair([FromForm] string apiToken, [FromForm] int pairId) {
        // Find session by apiToken
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        if (session is null)
            return null;

        // Get the pair
        Model.PairData? pair = keyValueService.GetPairData(session.UserId, session.VikingId, pairId);

        return keyValueService.ModelToSchema(pair);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("ContentWebService.asmx/SetKeyValuePair")]
    public IActionResult SetKeyValuePair([FromForm] string apiToken, [FromForm] int pairId, [FromForm] string contentXML) {
        Schema.PairData schemaData = XmlUtil.DeserializeXml<Schema.PairData>(contentXML);

        // Get the session
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        if (session is null)
            return Ok(false);

        bool result = keyValueService.SetPairData(session.UserId, session.VikingId, pairId, schemaData);

        return Ok(result);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("ContentWebService.asmx/GetKeyValuePairByUserID")]
    public Schema.PairData? GetKeyValuePairByUserID([FromForm] string apiToken, [FromForm] int pairId, [FromForm] string userId) {
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        if (session is null)
            return null;

        Model.PairData? pair = keyValueService.GetPairData(userId, null, pairId);

        return keyValueService.ModelToSchema(pair);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("ContentWebService.asmx/SetKeyValuePairByUserID")]
    public IActionResult SetKeyValuePairByUserID([FromForm] string apiToken, [FromForm] int pairId, [FromForm] string userId, [FromForm] string contentXML) {
        Schema.PairData schemaData = XmlUtil.DeserializeXml<Schema.PairData>(contentXML);

        // Get the session
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        if (session is null || string.IsNullOrEmpty(userId))
            return Ok(false);

        bool result = keyValueService.SetPairData(userId, null, pairId, schemaData);

        return Ok(result);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/GetCommonInventory")]
    [Route("ContentWebService.asmx/GetCommonInventory")]
    public IActionResult GetCommonInventory([FromForm] string apiToken) {
        // TODO, this is a placeholder
        User? user = ctx.Sessions.FirstOrDefault(e => e.ApiToken == apiToken)?.User;
        Viking? viking = ctx.Sessions.FirstOrDefault(e => e.ApiToken == apiToken)?.Viking;
        if (user is null && viking is null) {
            return Ok();
        }

        return Ok(new CommonInventoryData {
            UserID = Guid.Parse(user is not null ? user.Id : viking.Id),
            Item = new UserItemData[] {
                new UserItemData {
                    UserInventoryID = 1099730701,
                    ItemID = 8977,
                    Quantity = 1,
                    Uses = -1,
                    ModifiedDate = DateTime.Now,
                    Item = new ItemData {
                        AssetName = "DragonStableINTDO",
                        Cost = 100000,
                        CashCost = -1,
                        CreativePoints = 0,
                        Description = "Any dragon would be glad to make one of the two available nests home!",
                        IconName = "RS_DATA/DragonsStablesDO.unity3d/IcoDWDragonStableDefault",
                        InventoryMax = 1,
                        ItemID = 8977,
                        ItemName = "Dragon Stable",
                        Locked = false,
                        Stackable = false,
                        AllowStacking = false,
                        SaleFactor = 10,
                        Uses = -1,
                        Attribute = new ItemAttribute[] {
                            new ItemAttribute { Key = "2D", Value = "1" },
                            new ItemAttribute { Key = "NestCount", Value = "2" },
                            new ItemAttribute { Key = "StableType", Value = "All" }
                        },
                        Category = new ItemDataCategory[] {
                            new ItemDataCategory { CategoryId = 455, CategoryName = "Dragons Dragon Stable", IconName = "8977"}

                        }
                    }
                }
            }
        });
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("ContentWebService.asmx/GetAuthoritativeTime")]
    public IActionResult GetAuthoritativeTime() {
        return Ok(new DateTime(DateTime.Now.Ticks));
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/SetAvatar")]
    public IActionResult SetAvatar([FromForm] string apiToken, [FromForm] string contentXML) {
        Viking? viking = ctx.Sessions.FirstOrDefault(e => e.ApiToken == apiToken)?.Viking;
        if (viking is null) {
            return Ok(new SetAvatarResult {
                Success = false,
                StatusCode = AvatarValidationResult.Error
            });
        }

        viking.AvatarSerialized = contentXML;
        ctx.SaveChanges();

        return Ok(new SetAvatarResult {
			Success = true,
			DisplayName = viking.Name,
			StatusCode = AvatarValidationResult.Valid
		});
    }

    [HttpPost]
    //[Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/GetAllActivePetsByuserId")]
    public IActionResult GetAllActivePetsByuserId() {
        // TODO, this is a placeholder
        return Ok("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<ArrayOfRaisedPetData xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />");
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/GetUserUpcomingMissionState")]
    public IActionResult GetUserUpcomingMissionState([FromForm] string apiToken, [FromForm] string userId) {
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        UserMissionStateResult result = new UserMissionStateResult();

        if (session is null)
            return Ok(result);

        result.UserID = Guid.Parse(session.VikingId);
        return Ok(result); // TODO: placeholder, returns no upcoming missions
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/GetUserActiveMissionState")]
    public IActionResult GetUserActiveMissionState([FromForm] string apiToken, [FromForm] string userId) {
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        UserMissionStateResult result = new UserMissionStateResult { Missions = new List<Mission>()  };
        Mission tutorial = XmlUtil.DeserializeXml<Mission>(XmlUtil.ReadResourceXmlString("tutorialmission"));

        // Update the mission with completed tasks
        List<Model.TaskStatus> taskStatuses = ctx.TaskStatuses.Where(e => e.VikingId == userId && e.MissionId == tutorial.MissionID).ToList();
        foreach (var task in taskStatuses) {
            RuleItem? rule = tutorial.MissionRule.Criteria.RuleItems.Find(x => x.ID == task.Id);
            if (rule != null && task.Completed) rule.Complete = 1;

            Schema.Task? t = tutorial.Tasks.Find(x => x.TaskID == task.Id);
            if (t != null) {
                if (task.Completed) t.Completed = 1;
                t.Payload = task.Payload;
            }
        }

        result.Missions.Add(tutorial);

        if (session is null)
            return Ok("error");

        result.UserID = Guid.Parse(session.VikingId);
        return Ok(result); // TODO: placeholder, returns the tutorial
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/GetUserCompletedMissionState")]
    public IActionResult GetUserCompletedMissionState([FromForm] string apiToken, [FromForm] string userId) {
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        UserMissionStateResult result = new UserMissionStateResult();

        if (session is null)
            return Ok(result);

        result.UserID = Guid.Parse(session.VikingId);
        return Ok(result); // TODO: placeholder, returns no completed missions
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("V2/ContentWebService.asmx/SetTaskState")]
    public IActionResult SetTaskState([FromForm] string apiToken, [FromForm] string userId, [FromForm] int missionId, [FromForm] int taskId, [FromForm] bool completed, [FromForm] string xmlPayload) {
        Session? session = ctx.Sessions.FirstOrDefault(s => s.ApiToken == apiToken);
        if (session is null || session.VikingId != userId)
            return Ok(new SetTaskStateResult { Success = false, Status = SetTaskStateStatus.Unknown });

        Model.TaskStatus? status = ctx.TaskStatuses.FirstOrDefault(task => task.Id == taskId && task.MissionId == missionId && task.VikingId == userId);

        if (status is null) {
            status = new Model.TaskStatus {
                Id = taskId,
                MissionId = missionId,
                VikingId = userId,
                Payload = xmlPayload,
                Completed = completed
            };
            ctx.TaskStatuses.Add(status);
        } else {
            status.Payload = xmlPayload;
            status.Completed = completed;
        }
        ctx.SaveChanges();

        return Ok(new SetTaskStateResult { Success = true, Status = SetTaskStateStatus.TaskCanBeDone });
    }
}
