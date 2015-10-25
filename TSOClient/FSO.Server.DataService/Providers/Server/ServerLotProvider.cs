﻿using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Protocol.CitySelector;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Providers.Server
{
    public class ServerLotProvider : LazyDataServiceProvider<uint, Lot>
    {
        private IShardRealestateDomain Realestate;
        private int ShardId;

        public ServerLotProvider([Named("ShardId")] int shardId, IRealestateDomain realestate){
            ShardId = shardId;
            Realestate = realestate.GetByShard(shardId);
        }


        protected override Lot LazyLoad(uint key)
        {
            var location = MapCoordinates.Unpack(key);

            var lot = new Lot {
                Lot_Name = "My Lot",
                Lot_IsOnline = false,
                Lot_Location = new Location { Location_X = location.X, Location_Y = location.Y },
                //Lot_Price = 0,
                Lot_Price = (uint)Realestate.GetPurchasePrice(location.X, location.Y),
                Lot_OwnerVec = new List<uint>() { },
                Lot_RoommateVec = new List<uint>() { },
                Lot_NumOccupants = 1
            };
            return lot;
        }
    }
}
