using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items
{
    class CallDeliveryCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_call_delivery"; }
        }

        public string Parameters
        {
            get { return "%item%"; }
        }

        public string Description
        {
            get { return "Pide al repartidor que entregue un determinado artículo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, introduzca el artículo que desea entregar.", 1);
                return;
            }

            if (RoleplayManager.CalledDelivery)
            {
                Session.SendWhisper("El hombre de entrega está demasiado ocupado ahora mismo! Por favor, inténtelo de nuevo más tarde.", 1);
                return;
            }

            bool DeliveryCame = false;

            string Item = Params[1];

            switch (Item.ToLower())
            {
                #region Weapons
                case "glock":
                case "magnum":
                case "mp5":
                case "cuchillo":
                case "martillo":
                case "ak47oro":
                    {
                        if (!Room.DeliveryEnabled)
                        {
                            Session.SendWhisper("El hombre de entrega no entrega a esta habitación", 1);
                            break;
                        }

                        if (!Session.GetRoleplay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("Usted debe estar trabajando para llamar al hombre de entrega", 1);
                            break;
                        }

                        if (!GroupManager.HasJobCommand(Session, "weapon") && !Session.GetHabbo().GetPermissions().HasRight("offer_anything"))
                        {
                            Session.SendWhisper("¡No trabajas en la tienda de armas!", 1);
                            break;
                        }

                        var Weapon = WeaponManager.getWeapon(Item.ToLower());

                        if (Weapon == null)
                        {
                            Session.SendWhisper("Por alguna razón, esta arma no se pudo encontrar.", 1);
                            break;
                        }

                        if (Weapon.Stock > 0)
                        {
                            Session.SendWhisper("Por favor espere " + Weapon.PublicName + "s Están fuera de stock antes de llamar al hombre de entrega", 1);
                            break;
                        }

                        RoleplayBot Bot = RoleplayBotManager.GetCachedBotByAI(RoleplayBotAIType.DELIVERY);

                        if (Bot == null)
                        {
                            Session.SendWhisper("No se encontró bot de entrega, por favor contacte a un miembro del personal", 1);
                            break;
                        }

                        RoleplayManager.UserWhoCalledDelivery = Session.GetHabbo().Id;
                        RoleplayManager.CalledDelivery = true;
                        RoleplayManager.DeliveryWeapon = Weapon;

                        new Thread(() =>
                        {
                            if (Session.GetRoomUser() != null)
                            {
                                Session.Shout("*Agarra su teléfono y llama al hombre de entrega, ordenando un nuevo stock de " + Weapon.PublicName + "s*", 4);
                                Session.GetRoomUser().ApplyEffect(EffectsList.CellPhone);
                            }

                            Thread.Sleep(3000);

                            if (Session.GetRoomUser() != null)
                                Session.GetRoomUser().ApplyEffect(0);
                        }).Start();

                        var BotUser = RoleplayBotManager.GetDeployedBotById(Bot.Id);
                        new Thread(() =>
                        {
                            Thread.Sleep(15000);

                            
                            RoleplayBot DeliverrBot = RoleplayBotManager.GetCachedBotByAI(RoleplayBotAIType.DELIVERY);

                            if (!DeliveryCame)
                            {
                                if (DeliverrBot == null)
                                {
                                    Session.SendWhisper("No se puede obtener el bot de entrega. Vuelve a intentarlo más tarde.", 1);
                                    Thread.CurrentThread.Abort();
                                    return;
                                }
                                else
                                {
                                    RoleplayBotManager.DeployBotByAI(RoleplayBotAIType.DELIVERY, "default", Room.Id);
                                    DeliveryCame = true;
                                }
                            }

                            while (Room != null && Room.GetRoomItemHandler() != null && Room.GetRoomItemHandler().GetFloor.Where(x => x.GetBaseItem().InteractionType == HabboHotel.Items.InteractionType.DELIVERY_BOX).ToList().Count <= 0)
                            {
                                Thread.Sleep(10);
                            }

                            Thread.Sleep(2000);
                            RoleplayManager.CalledDelivery = false;
                        }).Start();
                        break;
                    }
                #endregion

                #region Default
                default:
                    {
                        Session.SendWhisper("¡Eso no es un artículo entregable!", 1);
                        break;
                    }
                    #endregion
            }
        }
    }
}