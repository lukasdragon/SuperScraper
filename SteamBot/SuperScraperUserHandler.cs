using System;
using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeOffer;
using SteamTrade.TradeWebAPI;

namespace SteamBot
{
    public class SuperScraperUserHandler : UserHandler
    {
        public TF2Value UnverifiedAmount;

        public SuperScraperUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }


        public static int InviteTimerInterval = 2000;


        public int userWepAdded = 0;

        public int invalidItem = 0;
        public bool errorMsgRun = false;





        #region overrides
        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd()
        {
            return true;
        }


        public override void OnLoginCompleted()
        {
        }

        public override void OnFriendRemove()
        {
            Log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + "removed me!");

        }


        #endregion

        #region chatLogic
        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            SendChatMessage(Response(message));

        }
        public override void OnTradeMessage(string message)
        {
            SendTradeMessage(Response(message));
        }

        public string Response(string message)
        {
            message = message.ToLower();
            switch (message)
            {
                case "thanks":
                    return "You're Welcome <3";

                case "help":
                    return "Just send a trade request";

                default:
                    return Bot.ChatResponse;
            }
        }

        #endregion

        #region tradeLogic
        public override bool OnTradeRequest()
        {
            if (IsAdmin)
                return true;

            return false;
        }

        public override void OnTradeError(string error)
        {
            SendChatMessage("my bad, there was an error: {0}.", error);
            Log.Warn(error);
        }

        public override void OnTradeTimeout()
        {
            SendChatMessage("Sorry bud, but you were AFK and the trade was canceled.");
            Log.Info("User was kicked because he was AFK.");
        }

        public override void OnTradeInit()
        {
            UnverifiedAmount = TF2Value.Zero;
            SendTradeMessage("Hurray, great success! I'm ready to trade!");
            SendTradeMessage("Please note that I will give you a single scrap for each item you add ");
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            Log.Success("User added: " + schemaItem.ItemName);
            if (invalidItem >= 4)
            {
                Trade.CancelTrade();
                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "Stop messing around. This bot is used for scrapbanking, and will only accept craftable weapons.");
            }
            else if ((item.CraftClass == "weapon" || item.CraftMaterialType == "weapon") && !inventoryItem.IsNotCraftable)
            {
                userWepAdded++;
                UnverifiedAmount += TF2Value.Scrap * 0.5;
            }
            else if (item.Defindex == 5000)
                UnverifiedAmount += TF2Value.Scrap;
            else if (item.Defindex == 5001)
                UnverifiedAmount += TF2Value.Reclaimed;
            else if (item.Defindex == 5002)
                UnverifiedAmount += TF2Value.Refined;
            else
            {
                Trade.SendMessage(schemaItem.ItemName + " is not a valid item! Please remove it from the trade.");
                invalidItem++;
            }
            SendTradeMessage("I now owe you: {0} ref, {1} rec, and {2} scrap", UnverifiedAmount.RefinedPart, UnverifiedAmount.ReclaimedPart, UnverifiedAmount.ScrapPart);
        }


        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            var item = Trade.CurrentSchema.GetItem(schemaItem.Defindex);
            Log.Success("User removed: " + schemaItem.ItemName);
            if ((item.CraftClass == "weapon" || item.CraftMaterialType == "weapon") && !inventoryItem.IsNotCraftable)
            {
                userWepAdded--;
                UnverifiedAmount -= TF2Value.Scrap * 0.5;
            }
            else if (item.Defindex == 5000)
                UnverifiedAmount -= TF2Value.Scrap;
            else if (item.Defindex == 5001)
                UnverifiedAmount -= TF2Value.Reclaimed;
            else if (item.Defindex == 5002)
                UnverifiedAmount -= TF2Value.Refined;
            else
            {
                invalidItem--;
            }
            SendTradeMessage("I now owe you: {0} ref, {1} rec, and {2} scrap", UnverifiedAmount.RefinedPart, UnverifiedAmount.ReclaimedPart, UnverifiedAmount.ScrapPart);
        }





        public override void OnTradeReady(bool ready)
        {
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                if (Validate())
                {
                    Trade.SetReady(true);
                }
                SendTradeMessage("Scrap: {0}", UnverifiedAmount.ScrapTotal);
            }
        }

        public override void OnTradeAwaitingConfirmation(long tradeOfferID)
        {
            Log.Warn("Trade ended awaiting confirmation");
            SendChatMessage("Hey Bud, please complete the confirmation to finish the trade");
        }

        public override void OnTradeOfferUpdated(TradeOffer offer)
        {
            switch (offer.OfferState)
            {
                case TradeOfferState.TradeOfferStateAccepted:
                    Log.Info(String.Format("Trade offer {0} has been completed!", offer.TradeOfferId));
                    SendChatMessage("Trade completed, have an awesome day!");
                    break;
                case TradeOfferState.TradeOfferStateActive:
                case TradeOfferState.TradeOfferStateNeedsConfirmation:
                case TradeOfferState.TradeOfferStateInEscrow:
                    //Trade is still active but incomplete
                    break;
                case TradeOfferState.TradeOfferStateCountered:
                    Log.Info(String.Format("Trade offer {0} was countered", offer.TradeOfferId));
                    break;
                default:
                    Log.Info(String.Format("Trade offer {0} failed", offer.TradeOfferId));
                    break;
            }
        }

        public override void OnTradeAccept()
        {
            if (Validate() || IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try
                {
                    if (Trade.AcceptTrade())
                        Log.Success("Trade Accepted!");
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
                }
            }
        }

        #endregion

        public bool Validate()
        {
            TF2Value verifiedValue = TF2Value.Zero;            
            List<string> errors = new List<string>();

            if (invalidItem > 0)
            {
                errors.Add("You have given me invalid items! Please remove them!");
                Log.Warn("User has invalid items put up!");
            }

            foreach (TradeUserAssets asset in Trade.OtherOfferedItems)
            {
                var item = Trade.CurrentSchema.GetItem(Trade.OtherInventory.GetItem(asset.assetid).Defindex);

                if ((item.CraftClass == "weapon" || item.CraftMaterialType == "weapon"))
                    verifiedValue += TF2Value.Scrap * 0.5;
                else if (item.Defindex == 5000)
                    verifiedValue += TF2Value.Scrap;
                else if (item.Defindex == 5001)
                    verifiedValue += TF2Value.Reclaimed;
                else if (item.Defindex == 5002)
                    verifiedValue += TF2Value.Refined;
            }

            if (UnverifiedAmount != verifiedValue)
            {
                errors.Add("The previous payout estimate was incorrect... I actually owe you " + verifiedValue.RefinedTotal + " ref");
            }

            Trade.RemoveAllItems();

            while (verifiedValue.RefinedPart > 0)
            {
                verifiedValue -= TF2Value.Refined;
                Trade.AddItem(5002);                
            }
            while (verifiedValue.ReclaimedPart > 0)
            {
                verifiedValue -= TF2Value.Reclaimed;
                Trade.AddItem(5001);
            }
            while (verifiedValue.ScrapPart > 0)
            {
                verifiedValue -= TF2Value.Scrap;
                Trade.AddItem(5000);
            }
                        
            // send the errors
            if (errors.Count != 0)
                Trade.SendMessage("There were errors in your trade: ");

            foreach (string error in errors)
            {
                Trade.SendMessage(error);
            }

            return errors.Count == 0;
        }

    }

}

