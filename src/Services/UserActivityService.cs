using sodoff.Model;
using sodoff.Schema;

namespace sodoff.Services
{
    public class UserActivityService
    {
        public readonly DBContext ctx;
        public UserActivityService(DBContext ctx)
        {
            this.ctx = ctx;
        }

        public List<Schema.UserActivity> GetUserActivities(Viking viking)
        {
            List<Model.UserActivity> userActivities = ctx.UserActivities.Where(e => e.VikingId == viking.Id).ToList();
            List<Schema.UserActivity> response = new List<Schema.UserActivity>();

            foreach(var activity in userActivities)
            {
                response.Add(new Schema.UserActivity
                {
                    UserID = viking.Uid,
                    RelatedUserID = activity.RelatedVikingUid,
                    UserActivityID = activity.Id,
                    UserActivityTypeID = activity.VikingActivityTypeId,
                    LastActivityDate = activity.LastActivityAt
                });
            }

            return response;
        }

        public Model.UserActivity AddUserActivity(Schema.UserActivity deserializedUserActivity)
        {
            Viking? viking = ctx.Vikings.FirstOrDefault(e => e.Uid == deserializedUserActivity.UserID);
            Viking? relatedViking = ctx.Vikings.FirstOrDefault(e => e.Uid == deserializedUserActivity.RelatedUserID);

            if (viking != null && relatedViking != null) // i do not know how to handle the other types, game seems to be treating all type id's the same
            {
                Model.UserActivity userActivity = new Model.UserActivity
                {
                    LastActivityAt = DateTime.UtcNow,
                    VikingActivityTypeId = deserializedUserActivity.UserActivityTypeID ?? 0,
                    RelatedVikingUid = relatedViking.Uid
                };

                viking.UserActivities.Add(userActivity);
                ctx.SaveChanges();
                return userActivity;
            }
            else return new Model.UserActivity();
        }
    }
}
