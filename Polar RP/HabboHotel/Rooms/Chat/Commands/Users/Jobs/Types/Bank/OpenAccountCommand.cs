using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Bank
{
    class OpenAccountCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_jobs_open_account"; }
        }

        public string Parameters
        {
            get { return "%user% %type%"; }
        }

        public string Description
        {
            get { return "Se ofrece para abrir el tipo de cuenta bancaria al usuario"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor ingrese un tipo de cuenta. (chequings, savings)");
                return;
            }

            GameClient Target = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (Target == null)
            {
                Session.SendWhisper("¡Uy, no pudo encontrar ese usuario!");
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Target.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "openaccount") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
            {
                Session.SendWhisper("Lo siento, no trabajas en la corporación del banco", 1);
                return;
            }

            #endregion

            #region Execute

            if (Params.Length < 3)
            {
                if (Target.GetRoleplay().BankAccount > 0)
                {
                    Session.SendWhisper("Lo sentimos, pero esta persona ya tiene una cuenta corriente", 1);
                    return;
                }

                string offer = "corriente";
                bool HasOffer = false;
                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                {
                    if (Offer.Type.ToLower() == offer.ToLower())
                        HasOffer = true;
                }
                if (!HasOffer)
                {
                    Session.Shout("*Ofertas para abrir una cuenta corriente para " + Target.GetHabbo().Username + " ¡GRATIS!*", 4);
                    Target.GetRoleplay().OfferManager.CreateOffer("corriente", Session.GetHabbo().Id, 0);
                    Target.SendWhisper("Acaba de recibir una Cuenta corriente gratis DIGA ':aceptar corriente' para activarla!", 1);
                    return;
                }
                else
                {
                    Session.SendWhisper("A este usuario ya se le ha ofrecido una cuenta de corriente", 1);
                    return;
                }
            }
            else
            {
                switch (Params[2].ToLower())
                {
                    case "corriente":
                    case "chequings":
                    case "checkings":
                        {
                            if (Target.GetRoleplay().BankAccount > 0)
                            {
                                Session.SendWhisper("Lo siento, pero esta persona ya tiene una cuenta corriente!", 1);
                                break;
                            }

                            Params[2] = "corriente";
                            bool HasOffer = false;
                            foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                            {
                                if (Offer.Type.ToLower() == Params[2].ToLower())
                                    HasOffer = true;
                            }
                            if (!HasOffer)
                            {
                                Session.Shout("*Ofertas para abrir una cuenta corriente para " + Target.GetHabbo().Username + " ¡GRATIS!*", 4);
                                Target.GetRoleplay().OfferManager.CreateOffer("corriente", Session.GetHabbo().Id, 0);
                                Target.SendWhisper("Acaba de recibir una Cuenta corriente gratis DIGA ':aceptar corriente' para activarla", 1);
                                break;
                            }
                            else
                            {
                                Session.SendWhisper("A este usuario ya se le ha ofrecido una cuenta de corriente", 1);
                                break;
                            }
                        }
                    case "ahorro":
                    case "savings":
                        {
                            int Cost = 2500;
                            if (Target.GetRoleplay().BankAccount > 1)
                            {
                                Session.SendWhisper("Lo sentimos, pero esta persona ya tiene una cuenta de ahorro", 1);
                                break;
                            }

                            bool HasOffer = false;
                            if (Target.GetHabbo().Credits >= Cost)
                            {
                                foreach (var Offer in Target.GetRoleplay().OfferManager.ActiveOffers.Values)
                                {
                                    if (Offer.Type.ToLower() == Params[2].ToLower())
                                        HasOffer = true;
                                }
                                if (!HasOffer)
                                {
                                    Session.Shout("*Ofertas para abrir una Cuenta de Ahorro para " + Target.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", 4);
                                    Target.GetRoleplay().OfferManager.CreateOffer("ahorro", Session.GetHabbo().Id, Cost);
                                    Target.SendWhisper("Recién se le ha ofrecido una cuenta de ahorros por $" + String.Format("{0:N0}", Cost) + " DIGA ':aceptar ahorro' para activarla!", 1);
                                    break;
                                }
                                else
                                {
                                    Session.SendWhisper("A este usuario ya se le ha ofrecido una cuenta de ahorro", 1);
                                    break;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("¡Este ciudadano no puede pagar una Cuenta de Ahorros!", 1);
                                break;
                            }
                        }
                    default:
                        {
                            Session.SendWhisper("Ingrese un tipo de cuenta bancaria válido: (corriente o ahorro)");
                            break;
                        }
                }
            }

            #endregion
        }
    }
}