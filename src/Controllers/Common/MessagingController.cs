using Microsoft.AspNetCore.Mvc;
using sodoff.Attributes;
using sodoff.Model;
using sodoff.Schema;
using sodoff.Services;
using sodoff.Util;

namespace sodoff.Controllers.Common;
public class MessagingController : Controller {
    private readonly DBContext ctx;
    private readonly MessageService messageService;
    public MessagingController(MessageService messageService, DBContext ctx)
    {
        this.messageService = messageService;
        this.ctx = ctx;
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/GetUserMessageQueue")]
    [VikingSession]
    public ArrayOfMessageInfo? GetUserMessageQueue(Viking viking, [FromForm] bool showOldMessages, [FromForm] bool showDeletedMessages, [FromForm] string apiKey) {
        return new ArrayOfMessageInfo { MessageInfo = messageService.GetUserMessageInfoArray(viking, showOldMessages, showDeletedMessages) };
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SendMessage")]
    [VikingSession]
    public IActionResult SendMessage(Viking viking, [FromForm] Guid toUser, [FromForm] int messageID, [FromForm] string data, [FromForm] string apiKey, [FromForm] string apiToken) {
        Viking? toViking = ctx.Vikings.FirstOrDefault(e => e.Uid == toUser);
        ArrayOfKeyValuePairOfStringString arrayOfKVP = XmlUtil.DeserializeXml<ArrayOfKeyValuePairOfStringString>(data);
        List<KeyValuePairOfStringString> pairList = new List<KeyValuePairOfStringString>();

        for (int i = 0; i < arrayOfKVP.KeyValuePairOfStringString.Length; i++)
        {
            pairList.Add(arrayOfKVP.KeyValuePairOfStringString[i]);
        }

        MessageTypeID typeId = MessageTypeID.Unknown;
        string typeText = "";

        switch(pairList.FirstOrDefault()?.Value)
        {
            case "Drawing":
                typeId = MessageTypeID.GreetingCard;
                typeText = "Greeting Card";
                break;
            case "Photo":
                typeId = MessageTypeID.Photo;
                typeText = "Photo";
                break;
        }

        if (toViking == null) return Ok(false);
        else
        {
            Model.Message msg = messageService.PostDataMessage(viking, toViking, apiToken, data, MessageType.Data, MessageLevel.WhiteList, typeId, "[[Line1]]=[[{{BuddyUserName}} has sent you a " + typeText + "]]",
                "[[Line1]]=[[{{BuddyUserName}} has sent you a " + typeText + "]]"); // hardcoding level for now
            if (msg != null) return Ok(true);
            else return Ok(false);
        }
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SaveMessage")]
    public IActionResult SaveMessage([FromForm]int userMessageQueueId, [FromForm] bool isNew, [FromForm] bool isDeleted, [FromForm] string apiKey) {
        Model.Message? messageFromQueueId = ctx.Messages.FirstOrDefault(e => e.QueueID == userMessageQueueId);

        if (messageFromQueueId != null)
        {
            messageFromQueueId.IsDeleted = isDeleted;
            messageFromQueueId.IsNew = isNew;
            messageFromQueueId.LastUpdatedAt = DateTime.UtcNow;
            ctx.SaveChanges();
            return Ok(true);
        }

        return Ok(false);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessageWebService.asmx/GetCombinedListMessage")]
    public ArrayOfCombinedListMessage? GetCombinedListMessage([FromForm] Guid userId, [FromForm] string apiKey)
    {
        Viking? viking = ctx.Vikings.FirstOrDefault(e => e.Uid == userId);

        if (viking == null) return new ArrayOfCombinedListMessage();

        return new ArrayOfCombinedListMessage { CombinedListMessage = messageService.GetCombinedMessageArray(viking) };
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessageWebService.asmx/RemoveMessageFromBoard")]
    public IActionResult RemoveMessageFromBoard([FromForm] int messageID, [FromForm] string apiKey)
    {
        return Ok(messageService.RemoveMessage(messageID));
    }
}
