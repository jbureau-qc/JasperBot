using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using JasperBot.Core.Data;

namespace JasperBot.Core.Commands
{
    public class CommandsHandling : ModuleBase<SocketCommandContext>
    {        
        [Command("request"), Summary("Request crafting order")]
        public async Task RequestCMD(params string[] @params)
        {
            List<CraftingRequest> requests = new List<CraftingRequest>();
            EmbedBuilder Embed = new EmbedBuilder();
            if (@params.Length < 3)
            {
                Embed.WithColor(Color.Red);
                Embed.WithTitle($"{Context.User.Username}'{(Context.User.Username.EndsWith("s") ? "" : "s")} order not accepted");
                Embed.WithAuthor(Context.User.Username, Context.User.GetAvatarUrl());
                Embed.Description = $"Invalid number of params provided ({@params.Length}) for request when at least 3 is needed.\n\nValid format is: @crafterRole quantity item1 item2 item3 etc.";
            } else {
                Embed.WithColor(Color.Green);
                Embed.WithTitle($"{Context.User.Username}'{(Context.User.Username.EndsWith("s") ? "" : "s")} order accepted");
                Embed.WithAuthor(Context.User.Username, Context.User.GetAvatarUrl());
                string crafter = @params[0];
                string quantity = @params[1];
                
                List<string> itemList = new List<string>();
                for (int i = 2; i < @params.Length; i++)
                {
                    itemList.Add(@params[i]);
                    requests.Add(new CraftingRequest(Program.Requests.Count+1, Utilities.RoleMapper(crafter), int.Parse(quantity), @params[i], Context.User.Username));
                }

                string itemListing = "";
                foreach (string item in itemList)
                    itemListing += $"{quantity}x {item}\n";

                Embed.AddField("Item Requested", itemListing);
                Embed.AddField("Crafter Role", crafter);
            }

            Embed.WithFooter($"posted at {DateTime.Now}");

            Utilities.DeleteCMD(Context.Channel);

            var post = await Context.Channel.SendMessageAsync("", false, Embed.Build());
            foreach (var req in requests)
            {
                req.messageId = post.Id;
                Program.Requests.Add(req);
            }

            Utilities.UpdateListing(Context.Channel);
        }

        [Command("take"), Summary("Take crafting request")]
        public async Task TakeCMD(params string[] @params)
        {
            Utilities.DeleteCMD(Context.Channel);

            if (@params.Length == 1)
            {
                var requests = Program.Requests.FindAll(r => r.id == int.Parse(@params[0]));
                foreach (var request in requests)
                {
                    request.status = OrderStatus.Assigned;
                    request.assignedCrafter = Context.User.Username;
                }
            }

            Utilities.UpdateListing(Context.Channel);
        }
        
        [Command("update"), Summary("Update crafting order status")]
        public async Task UpdateCMD(params string[] @params)
        {
            Utilities.DeleteCMD(Context.Channel);

            if (@params.Length == 2)
            {
                string value = @params[1];

                if (!"InProgress,Ready,Completed".Contains(value)) return;

                var requests = Program.Requests.FindAll(r => r.id == int.Parse(@params[0]));
                foreach (var request in requests)
                {
                    if (request.assignedCrafter != Context.User.Username) return;

                    switch (value)
                    {
                        case "In Progress":
                            request.status = OrderStatus.InProgress;
                            break;
                        case "Ready":
                            request.status = OrderStatus.Ready;
                            break;
                        case "Completed":
                            request.status = OrderStatus.Completed;
                            break;
                    }                    
                }
            }

            Utilities.UpdateListing(Context.Channel);
        }

        [Command("list"), Summary("List crafting requests/orders for current user")]
        public async Task ListCND()
        {
            Utilities.DeleteCMD(Context.Channel);

            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithColor(Color.Green);
            Embed.WithDescription($"List of crafting requests & crafting orders");
            Embed.WithTitle($"Crafting Requests & Orders");

            var requests = Program.Requests.FindAll(r => r.Requester == Context.User.Username);
            string res = "";
            foreach (var req in requests)
                res += Utilities.FormatRequest(req);
            res = (res == "" ? "None" : res);
            Embed.AddField("Pending Crafting Requests", res);

            res = "";
            foreach (var req in requests)
                res += Utilities.FormatRequest(req);
            requests = Program.Requests.FindAll(r => r.assignedCrafter == Context.User.Username);
            res = (res == "" ? "None" : res);
            Embed.AddField("Crafting Order to Complete", res);

            var u = Context.Message.Author;
            await UserExtensions.SendMessageAsync(u, "", false, Embed.Build());
        }

        [Command("cancel"), Summary("Cancel crafting request")]
        public async Task CancelCMD(params string[] @params)
        {
            Utilities.DeleteCMD(Context.Channel);

            //TODO Post cancelled order?

            if (@params.Length == 1)
            {                
                Program.Requests.RemoveAll(r => r.id == int.Parse(@params[0]));             
            }

            Utilities.UpdateListing(Context.Channel);
        }

        [Command("setrequester"), Summary("Update crafting request's requester")]
        public async Task SetRequesterCMD(params string[] @params)
        {
            Utilities.DeleteCMD(Context.Channel);
            
            if (@params.Length == 2)
            {
                var requests = Program.Requests.FindAll(r => r.id == int.Parse(@params[0]));
                foreach (var request in requests)
                    request.Requester = @params[1];
            }

            Utilities.UpdateListing(Context.Channel);
        }

        [Command("meet"), Summary("Setup meeting with crafting request's requester")]
        public async Task MeetCMD(params string[] @params)
        {
            Utilities.DeleteCMD(Context.Channel);

            if (@params.Length == 2)
            {
                IUser Requester = null;
                var requests = Program.Requests.FindAll(r => r.id == long.Parse(@params[0]));
                if (requests.Count == 0) return;

                var res = Context.Channel.GetUsersAsync();
                var users = await res.Flatten();
                foreach (var user in users)
                {
                    if (user.Username == requests[0].Requester)
                    {
                        Requester = user;
                    }
                }

                if (Requester == null) return;

                EmbedBuilder Embed = new EmbedBuilder();
                Embed.WithColor(Color.Green);
                Embed.WithTitle($"Your order is ready for pickup!");
                Embed.WithDescription($"Your order was fulfilled by crafter: {requests[0].assignedCrafter} and he would like to meetup so you can grab your items!");

                string items = "";
                foreach (var request in requests)
                {
                    items += $"{request.quantity}x {request.itemName} {Environment.NewLine}";
                }

                Embed.AddField("Items ready", items);
                Embed.AddField("Location to Meet", @params[1]);
                Embed.AddField("Crafter", requests[0].assignedCrafter);

                await UserExtensions.SendMessageAsync(Requester, "", false, Embed.Build());
            }
        }

        [Command("help"), Summary("List available commands")]
        public async Task HelpCMD(params string[] @params)
        {
            Utilities.DeleteCMD(Context.Channel);

            EmbedBuilder Embed = new EmbedBuilder();
            Embed.WithColor(Color.Green);
            Embed.WithDescription($"List of available commands related to crafting requests, each commands must start with ! or tag Jasper");
            Embed.WithTitle($"Jasper Crafting Requests Commands");
            Embed.AddField("!help", "List available commands");
            Embed.AddField("!request", "Request crafting order, need to provide 3 or more arguments. Crafter role @ (example @Scribe), quantity, item1, item2, item3");
            Embed.AddField("!take", "Take crafting request, need to provide request's id");
            Embed.AddField("!update", "Update crafting order status, need to provide request's id and status. Available status; \"In Progress\", Ready, Completed");
            Embed.AddField("!cancel", "Cancel crafting request, need to provide request's id");
            Embed.AddField("!meet", "Setup meeting with crafting request's requester, need to provide request's id and place to meet (surround with double quotes if you want to put spaces)");
            Embed.AddField("!setrequester", "Update crafting request's requester, need to provide request's id and username");
            Embed.AddField("!list", "List crafting requests/orders for current user");
            
            var u = Context.Message.Author;  
            await UserExtensions.SendMessageAsync(u, "", false, Embed.Build());
        }
  
    }
}
