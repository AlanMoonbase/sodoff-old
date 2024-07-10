using sodoff.Model;
using sodoff.Schema;
using sodoff.Util;
using System.Security.Authentication.ExtendedProtection;

namespace sodoff.Services
{
    public class MessageService
    {
        private readonly DBContext ctx;
        private readonly MMOCommunicationService mpCommunication;
        public MessageService(DBContext ctx, MMOCommunicationService mpCommunication)
        {
            this.ctx = ctx;
            this.mpCommunication = mpCommunication;
        }

        public CombinedListMessage[] GetCombinedMessageArray(Viking viking)
        {
            // get messages from database for user
            List<Model.Message> messages = ctx.Messages.Where(e => e.VikingId == viking.Id)
                .ToList();

            List<CombinedListMessage> messagesResponse = new List<CombinedListMessage>();

            foreach (Model.Message message in messages)
            {
                if(message.IsDeleted || DateTime.UtcNow >= message.CreatedAt.AddDays(7))
                {
                    ctx.Messages.Remove(message);
                    ctx.SaveChanges();
                    continue;
                }

                switch(message.MessageTypeID)
                {
                    case MessageTypeID.Messaging:
                    {

                            // construct a CombinedListMessage based on the database model
                            CombinedListMessage msgRes = new CombinedListMessage
                            {
                                MessageType = (int)message.MessageType,
                                MessageBody = XmlUtil.SerializeXml(new Schema.Message //TODO: Add code to also show photo messages.
                                {
                                    MessageID = message.Id,
                                    ConversationID = message.ConversationID ?? 0,
                                    ReplyToMessageID = message.ReplyToMessageID,
                                    Creator = ctx.Vikings.FirstOrDefault(e => e.Id == message.FromVikingId).Uid.ToString() ?? "NotFound",
                                    CreateTime = message.CreatedAt,
                                    UpdateDate = message.LastUpdatedAt,
                                    MessageType = message.MessageType.Value,
                                    MessageLevel = message.MessageLevel,
                                    Content = message.Data ?? "No Data In This Message. This Is Probably An Error.",
                                    DisplayAttribute = "C=White" // always have it be white :)
                                }),
                                MessageDate = message.CreatedAt
                            };

                            // get ALL replies
                            List<Model.Message> repliesToMsg = ctx.Messages.Where(e => e.ReplyToMessageID == message.Id).ToList();

                            foreach(Model.Message msg in repliesToMsg)
                            {
                                if(msg.Viking == viking) continue;

                                CombinedListMessage replyRes = new CombinedListMessage
                                {
                                    MessageType = (int)msg.MessageType,
                                    MessageBody = XmlUtil.SerializeXml(new Schema.Message //TODO: Add code to also show photo messages.
                                    {
                                        MessageID = msg.Id,
                                        ConversationID = msg.ConversationID ?? 0,
                                        ReplyToMessageID = msg.ReplyToMessageID,
                                        Creator = ctx.Vikings.FirstOrDefault(e => e.Id == msg.FromVikingId).Uid.ToString() ?? "NotFound",
                                        CreateTime = msg.CreatedAt,
                                        UpdateDate = msg.LastUpdatedAt,
                                        MessageType = msg.MessageType.Value,
                                        MessageLevel = msg.MessageLevel,
                                        Content = msg.Data ?? "No Data In This Message. This Is Probably An Error.",
                                        DisplayAttribute = "C=White" // always have it be white :)
                                    }),
                                    MessageDate = msg.CreatedAt
                                };

                                messagesResponse.Add(replyRes);
                            }

                            messagesResponse.Add(msgRes);

                            break;
                    }
                    case MessageTypeID.Photo:
                    case MessageTypeID.GreetingCard:
                    {
                            ArrayOfKeyValuePairOfStringString arrayOfKVP = XmlUtil.DeserializeXml<ArrayOfKeyValuePairOfStringString>(message.Data);
                            string data = "";
                            List<KeyValuePairOfStringString> pairList = new List<KeyValuePairOfStringString>();

                            for(int i = 0; i < arrayOfKVP.KeyValuePairOfStringString.Length; i++)
                            {
                                pairList.Add(arrayOfKVP.KeyValuePairOfStringString[i]);
                            }

                            foreach(KeyValuePairOfStringString pair in pairList)
                            {
                                data = data + $"[[{pair.Key}]]=[[{pair.Value}]]";
                            }

                            CombinedListMessage msgRes = new CombinedListMessage
                            {
                                MessageType = (int)message.MessageType,
                                MessageBody = XmlUtil.SerializeXml(new MessageInfo
                                {
                                    UserMessageQueueID = message.QueueID,
                                    MessageID = message.Id,
                                    MessageTypeID = (int)message.MessageTypeID,
                                    FromUserID = ctx.Vikings.FirstOrDefault(e => e.Id == message.FromVikingId).Uid.ToString() ?? "NotFound",
                                    MemberMessage = message.MemberMessage,
                                    NonMemberMessage = message.NonMemberMessage,
                                    Data = data ?? "NoData",
                                    CreateDate = message.CreatedAt
                                }),
                                MessageDate = message.CreatedAt
                            };

                            messagesResponse.Add(msgRes);

                            break;
                    }
                    case MessageTypeID.Achievement:
                    {
                            CombinedListMessage msgRes = new CombinedListMessage
                            {
                                MessageType = (int)message.MessageType,
                                MessageBody = XmlUtil.SerializeXml(new MessageInfo
                                {
                                    UserMessageQueueID = message.QueueID,
                                    MessageID = message.Id,
                                    MessageTypeID = (int)message.MessageTypeID,
                                    FromUserID = ctx.Vikings.FirstOrDefault(e => e.Id == message.FromVikingId).Uid.ToString() ?? "NotFound",
                                    MemberMessage = message.MemberMessage,
                                    NonMemberMessage = message.NonMemberMessage,
                                    Data = message.Data ?? "NoData",
                                    CreateDate = message.CreatedAt
                                }),
                                MessageDate = message.CreatedAt
                            };

                            messagesResponse.Add(msgRes);

                            break;
                    }
                }
            }

            // always add announcements

            List<Model.Message> annMsgs = ctx.Messages.Where(e => e.MessageType == MessageType.Announcement).ToList();

            foreach(Model.Message annMsg in annMsgs)
            {
                if(DateTime.UtcNow > annMsg.CreatedAt.AddDays(1)) // only keep announcements for one day
                {
                    ctx.Messages.Remove(annMsg);
                    ctx.SaveChanges();
                    continue;
                }

                CombinedListMessage annMsgRes = new CombinedListMessage
                {
                    MessageType = (int)annMsg.MessageType,
                    MessageBody = XmlUtil.SerializeXml(new Announcement
                    {
                        AnnouncementID = annMsg.Id,
                        AnnouncementText = annMsg.Data ?? "[[Message]]=[[No Announcement Data]]",
                        Description = "Important Message From The ReStarted Team"
                    }),
                    MessageDate = annMsg.CreatedAt
                };

                messagesResponse.Add(annMsgRes);
            }

            // sort messages by newest first
            List<CombinedListMessage> sortedList = messagesResponse.OrderBy(e => e.MessageDate).ToList();
            sortedList.Reverse();

            return sortedList.ToArray();
        }

        public MessageInfo[] GetUserMessageInfoArray(Viking viking, bool showOldMessages, bool showDeletedMessages)
        {
            List<Model.Message> recentMessages = ctx.Messages.Where(e => e.VikingId == viking.Id)
                .OrderBy(e => e.QueueID)
                .ToList();

            List<MessageInfo> messagesResponse = new List<MessageInfo>();

            foreach(Model.Message message in recentMessages)
            {
                if (!showOldMessages && message.IsDeleted) continue;
                if (!message.IsNew) continue;
                if (message.MessageType == MessageType.Announcement) continue; // do not add announcements due to missing from viking
                if (DateTime.UtcNow >= message.CreatedAt.AddMinutes(30) && message.IsNew) continue;
                messagesResponse.Add(new MessageInfo
                {
                    MessageID = message.Id,
                    UserMessageQueueID = message.QueueID,
                    FromUserID = ctx.Vikings.FirstOrDefault(e => e.Id == message.FromVikingId).Uid.ToString() ?? "NotFound",
                    MessageTypeID = (int)message.MessageTypeID,
                    CreateDate = message.CreatedAt,
                    Data = message.Data,
                    MemberMessage = message.MemberMessage ?? "NoMessage",
                    NonMemberMessage = message.MemberMessage ?? "NoMessage"
                });
            }

            return messagesResponse.ToArray();
        }

        public Model.Message PostTextMessage(Viking viking, Viking toViking, string content, MessageType type, MessageLevel level)
        {
            Random rnd = new Random(); // for generating conversationid and queueid

            // create a new message
            Model.Message newMsg = new Model.Message
            {
                VikingId = toViking.Id,
                FromVikingId = viking.Id,
                ConversationID = rnd.Next(1000, 9999),
                QueueID = rnd.Next(1000, 9999),
                CreatedAt = DateTime.UtcNow,
                MessageType = type,
                MessageLevel = level,
                Data = content,
                MessageTypeID = MessageTypeID.Messaging,
                IsNew = true
            };

            // add it to other vikings messages
            toViking.Messages.Add(newMsg);
            ctx.SaveChanges();

            return newMsg;
        }

        public Model.Message PostDataMessage(Viking viking, Viking toViking, string apiToken, string data, MessageType type, MessageLevel level, MessageTypeID messageTypeID, string memberMessage = "", string nonMemberMessage = "")
        {
            Random rnd = new Random(); // for generating conversationid and queueid

            // create a new message
            Model.Message newMsg = new Model.Message
            {
                VikingId = toViking.Id,
                FromVikingId = viking.Id,
                ConversationID = rnd.Next(1000, 9999),
                QueueID = rnd.Next(1000, 9999),
                CreatedAt = DateTime.UtcNow,
                MessageType = type,
                MessageLevel = level,
                MemberMessage = memberMessage,
                NonMemberMessage = nonMemberMessage,
                Data = data,
                MessageTypeID = messageTypeID,
                IsNew = true
            };

            // add it to other vikings messages
            toViking.Messages.Add(newMsg);
            ctx.SaveChanges();

            // send SPMN update packet to viking
            mpCommunication.SendPacketToPlayer(apiToken, toViking.Uid.ToString(), "SPMN", new string[] { "SPMN" });

            return newMsg;
        }

        public Model.Message PostBuddyRequest(Viking viking, Viking toViking, string apiToken)
        {
            Random rnd = new Random();

            // create a new message
            Model.Message newMsg = new Model.Message
            {
                VikingId = toViking.Id,
                FromVikingId = viking.Id,
                ConversationID = rnd.Next(1000, 9999),
                QueueID = rnd.Next(1000, 9999),
                CreatedAt = DateTime.UtcNow,
                MessageType = MessageType.Post,
                MessageLevel = MessageLevel.WhiteList,
                MemberMessage = "[[Line1]]=[[{{BuddyUserName}} wants to be added to your buddy list. Is that okay?]]",
                NonMemberMessage = "[[Line1]]=[[{{BuddyUserName}} wants to be added to your buddy list. Is that okay?]]",
                MessageTypeID = MessageTypeID.BuddyList,
                IsNew = true
            };

            // add it to other vikings messages
            toViking.Messages.Add(newMsg);
            ctx.SaveChanges();

            // buddy request updates are handled by SBE packet, do not send SPMN here

            return newMsg;
        }

        public Model.Message ReplyToMessage(int messageId, Viking viking, Viking toViking, string content, MessageType type, MessageLevel level)
        {
            Random rnd = new Random();

            Model.Message? message = ctx.Messages.FirstOrDefault(e => e.Id == messageId);

            if (message != null)
            {
                // create a new reply
                Model.Message newMsg = new Model.Message
                {
                    VikingId = toViking.Id,
                    FromVikingId = viking.Id,
                    ConversationID = message.ConversationID,
                    QueueID = rnd.Next(1000, 9999),
                    ReplyToMessageID = message.Id,
                    CreatedAt = DateTime.UtcNow,
                    MessageType = type,
                    MessageLevel = level,
                    Data = content,
                    MessageTypeID = MessageTypeID.Messaging
                };

                // add it to other vikings messages
                toViking.Messages.Add(newMsg);
                ctx.SaveChanges();

                return newMsg;
            }

            return new Model.Message();
        }

        public bool RemoveMessage(int id)
        {
            Model.Message messageToRemove = ctx.Messages.FirstOrDefault(e => e.Id == id);
            if (messageToRemove != null)
            {
                List<Model.Message> messageReplies = ctx.Messages.Where(e => e.ReplyToMessageID == messageToRemove.Id).ToList();

                foreach(Model.Message message in messageReplies)
                {
                    ctx.Messages.Remove(message);
                }

                ctx.Messages.Remove(messageToRemove);

                ctx.SaveChanges();
                return true;
            }

            return false;
        }
    }
}
