﻿using Microsoft.AspNetCore.Mvc;
using sodoff.Schema;

namespace sodoff.Controllers.Common;
public class MessagingController : Controller {

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/GetUserMessageQueue")]
    public ArrayOfMessageInfo? GetUserMessageQueue() {
        // TODO: this is a placeholder
        return null;
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SendMessage")]
    public IActionResult SendMessage() {
        // TODO: this is a placeholder
        return Ok(false);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessagingWebService.asmx/SaveMessage")]
    public IActionResult SaveMessage() {
        // TODO: this is a placeholder
        return Ok(false);
    }

    [HttpPost]
    [Produces("application/xml")]
    [Route("MessageWebService.asmx/GetCombinedListMessage")]
    public IActionResult GetCombinedListMessage()
    {
        // TODO - placeholder
        return Ok(new ArrayOfMessageInfo
        {
            MessageInfo = new List<MessageInfo>()
            {
                new MessageInfo
                {
                    MessageID = 1,
                    MessageTypeID = 1,
                    UserMessageQueueID = 1,
                    FromUserID = "e1521dff-1d7c-4c50-9a58-ad24f41e91d8",
                    MemberMessage = "Test"
                }
            }.ToArray()
        });
    }
}
