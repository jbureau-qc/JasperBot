using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;

namespace JasperBot.Core.Data
{
    public class CraftingRequest
    {
        public long id { get; }
        public CrafterRole role { get; set; }
        public int quantity { get; set; }
        public string itemName { get; set; }
        public string assignedCrafter { get; set; } = "Unassigned";
        public OrderStatus status { get; set; } = OrderStatus.Unassigned;
        public ulong messageId { get; set; }
        public string Requester { get; set; }

        public CraftingRequest(long id) { this.id = id; }

        public CraftingRequest(long id, CrafterRole role, int quantity, string itemName, string requester) {
            this.id = id;            
            this.quantity = quantity;
            this.itemName = itemName;
            this.role = role;
            this.Requester = requester;
        }
    }
}
