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
        public TF2Value AmountAdded;

        public SuperScraperUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }


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

        public override void OnFriendRemove() { }


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
            return true;
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
            SendTradeMessage("Hurray, great success! Please put up your items.");
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }




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
                SendTradeMessage("Scrap: {0}", AmountAdded.ScrapTotal);
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
            AmountAdded = TF2Value.Zero;

            List<string> errors = new List<string>();

            foreach (TradeUserAssets asset in Trade.OtherOfferedItems)
            {
                var item = Trade.OtherInventory.GetItem(asset.assetid);
                if (item.Defindex == 5000)
                    AmountAdded += TF2Value.Scrap;
                else if (item.Defindex == 5001)
                    AmountAdded += TF2Value.Reclaimed;
                else if (item.Defindex == 5002)
                    AmountAdded += TF2Value.Refined;
                else
                {
                    var schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
                    errors.Add("Item " + schemaItem.Name + " is not a metal.");
                }
            }

            if (AmountAdded == TF2Value.Zero)
            {
                errors.Add("You must put up at least 1 scrap.");
            }

            // send the errors
            if (errors.Count != 0)
                SendTradeMessage("There were errors in your trade: ");
            foreach (string error in errors)
            {
                SendTradeMessage(error);
            }

            return errors.Count == 0;
        }

    }

}

