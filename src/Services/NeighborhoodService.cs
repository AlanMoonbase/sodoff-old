﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using sodoff.Configuration;
using sodoff.Model;
using sodoff.Schema;
using sodoff.Util;

namespace sodoff.Services
{
    public class NeighborhoodService
    {
        private readonly DBContext ctx;
        private readonly MMOCommunicationService mpCommunication;

        // default neighborhood slots (NPCs)
        Guid slot0 = new Guid("aaaaaaaa-0000-0000-0000-000000000000");
        Guid slot1 = new Guid("bbbbbbbb-0000-0000-0000-000000000000");
        Guid slot2 = new Guid("cccccccc-0000-0000-0000-000000000000");
        Guid slot3 = new Guid("dddddddd-0000-0000-0000-000000000000");
        Guid slot4 = new Guid("eeeeeeee-0000-0000-0000-000000000000");

        public NeighborhoodService(DBContext ctx, MMOCommunicationService mpCommunication)
        {
            this.ctx = ctx;
            this.mpCommunication = mpCommunication;
        }

        public Neighborhood SaveDefaultNeighbors(Viking viking)
        {
            Neighborhood newNeighborhood = new Neighborhood
            {
                VikingId = viking.Id,
                Slot0 = this.slot0,
                Slot1 = this.slot1,
                Slot2 = this.slot2,
                Slot3 = this.slot3,
                Slot4 = this.slot4
            };
            viking.Neighborhood = newNeighborhood;
            ctx.SaveChanges();
            return newNeighborhood;
        }

        public bool SaveNeighbors(Viking viking, string neighborUid, int slot, string apiToken) {
            Neighborhood? neighborhood = viking.Neighborhood;
            Dictionary<string, string> neighborVars = new Dictionary<string, string>();

            if (neighborhood == null) SaveDefaultNeighbors(viking);

            // couldn't find a better way to do this
            switch (slot) {
                case 0:
                    viking.Neighborhood.Slot0 = new Guid(neighborUid);
                    neighborVars.Add("S0", viking.Neighborhood.Slot0.ToString());
                    break;
                case 1:
                    viking.Neighborhood.Slot1 = new Guid(neighborUid);
                    neighborVars.Add("S1", viking.Neighborhood.Slot1.ToString());
                    break;
                case 2:
                    viking.Neighborhood.Slot2 = new Guid(neighborUid);
                    neighborVars.Add("S2", viking.Neighborhood.Slot2.ToString());
                    break;
                case 3:
                    viking.Neighborhood.Slot3 = new Guid(neighborUid);
                    neighborVars.Add("S3", viking.Neighborhood.Slot3.ToString());
                    break;
                case 4:
                    viking.Neighborhood.Slot4 = new Guid(neighborUid);
                    neighborVars.Add("S4", viking.Neighborhood.Slot4.ToString());
                    break;
            }

            // update room vars
            mpCommunication.UpdateRoomVarsInRoom(apiToken, "MyNeighborhood_" + viking.Uid.ToString(), neighborVars);

            ctx.SaveChanges();
            return true;
        }

        public NeighborData GetNeighbors(string userId) {
            Model.Viking? viking = ctx.Vikings.FirstOrDefault(e => e.Uid == new Guid(userId));
            bool isNull = viking == null || viking.Neighborhood == null; 

            Neighbor[] neighbors = {
                new Neighbor {
                    NeighborUserID = isNull ? this.slot0 : viking.Neighborhood.Slot0,
                    Slot = 0
                },
                new Neighbor {
                    NeighborUserID = isNull ? this.slot1 : viking.Neighborhood.Slot1,
                    Slot = 1
                },
                new Neighbor {
                    NeighborUserID = isNull ? this.slot2 : viking.Neighborhood.Slot2,
                    Slot = 2
                },
                new Neighbor {
                    NeighborUserID = isNull ? this.slot3 : viking.Neighborhood.Slot3,
                    Slot = 3
                },
                new Neighbor {
                    NeighborUserID = isNull ? this.slot4 : viking.Neighborhood.Slot4,
                    Slot = 4
                }
            };

            return new NeighborData {
                UserID = new Guid(userId),
                Neighbors = neighbors
            };
        }
    }
}
