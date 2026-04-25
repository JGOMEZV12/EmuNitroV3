using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.HabboHotel.Items.Wired
{
    static class WiredBoxTypeUtility
    {
        public static WiredBoxType FromWiredId(int Id)
        {
            switch (Id)
            {
                default:
                    return WiredBoxType.None;
                case 1:
                    return WiredBoxType.TriggerUserSays;
                case 2:
                    return WiredBoxType.TriggerStateChanges;
                case 3:
                    return WiredBoxType.TriggerRepeat;
                case 4:
                    return WiredBoxType.TriggerRoomEnter;
                case 8:
                    return WiredBoxType.TriggerWalkOnFurni;
                case 9:
                    return WiredBoxType.TriggerWalkOffFurni;
                case 5:
                    return WiredBoxType.EffectShowMessage;
                case 6:
                    return WiredBoxType.EffectTeleportToFurni;
                case 7:
                    return WiredBoxType.EffectToggleFurniState;
                case 10:
                    return WiredBoxType.EffectKickUser;
                case 11:
                    return WiredBoxType.ConditionFurniHasUsers;
                case 12:
                    return WiredBoxType.ConditionFurniHasFurni;
                case 13:
                    return WiredBoxType.ConditionTriggererOnFurni;
                case 14:
                    return WiredBoxType.EffectMatchPosition;
                case 21:
                    return WiredBoxType.ConditionIsGroupMember;
                case 22:
                    return WiredBoxType.ConditionIsNotGroupMember;
                case 23:
                    return WiredBoxType.ConditionTriggererNotOnFurni;
                case 24:
                    return WiredBoxType.ConditionFurniHasNoUsers;
                case 25:
                    return WiredBoxType.ConditionIsWearingBadge;
                case 26:
                    return WiredBoxType.ConditionIsWearingFX;
                case 27:
                    return WiredBoxType.ConditionIsNotWearingBadge;
                case 28:
                    return WiredBoxType.ConditionIsNotWearingFX;
                case 29:
                    return WiredBoxType.ConditionMatchStateAndPosition;
                case 30:
                    return WiredBoxType.ConditionUserCountInRoom;
                case 31:
                    return WiredBoxType.ConditionUserCountDoesntInRoom;
                case 32:
                    return WiredBoxType.EffectMoveAndRotate;
                case 33:
                    return WiredBoxType.ConditionDontMatchStateAndPosition;
                case 34:
                    return WiredBoxType.ConditionFurniTypeMatches;
                case 35:
                    return WiredBoxType.ConditionFurniTypeDoesntMatch;
                case 36:
                    return WiredBoxType.ConditionFurniHasNoFurni;
                case 37:
                    return WiredBoxType.EffectMoveFurniToNearestUser;
                case 38:
                    return WiredBoxType.EffectMoveFurniFromNearestUser;
                case 39:
                    return WiredBoxType.EffectMuteTriggerer;
                case 40:
                    return WiredBoxType.EffectGiveReward;
                case 41:
                    return WiredBoxType.AddonRandomEffect;
                case 42:
                    return WiredBoxType.TriggerGameStarts;
                case 43:
                    return WiredBoxType.TriggerGameEnds;
                case 44:
                    return WiredBoxType.TriggerUserFurniCollision;
                case 46:
                    return WiredBoxType.EffectExecuteWiredStacks;
                case 47:
                    return WiredBoxType.EffectTeleportBotToFurniBox;
                case 48:
                    return WiredBoxType.EffectBotChangesClothesBox;
                case 49:
                    return WiredBoxType.EffectBotMovesToFurniBox;
                case 50:
                    return WiredBoxType.EffectBotCommunicatesToAllBox;
                case 51:
                    return WiredBoxType.EffectBotCommunicatesToUserBox;
                case 52:
                    return WiredBoxType.EffectBotFollowsUserBox;
                case 53:
                    return WiredBoxType.EffectBotGivesHanditemBox;
                case 54:
                    return WiredBoxType.ConditionActorHasHandItemBox;
                case 55:
                    return WiredBoxType.ConditionActorIsInTeamBox;
                case 56:
                    return WiredBoxType.EffectAddActorToTeam;
                case 57:
                    return WiredBoxType.EffectRemoveActorFromTeam;
                case 58:
                    return WiredBoxType.TriggerUserSaysCommand;
                case 59:
                    return WiredBoxType.EffectSetRollerSpeed;
                case 60:
                    return WiredBoxType.EffectRegenerateMaps;
                case 61:
                    return WiredBoxType.EffectGiveUserBadge;
                case 62:
                    return WiredBoxType.EffectAddScore;
                case 63:
                    return WiredBoxType.EffectGiveCurrency;
                case 64:
                    return WiredBoxType.ConditionHasJob;
                case 65:
                    return WiredBoxType.ConditionIsNight;
                case 66:
                    return WiredBoxType.ConditionIsDay;
                case 67:
                    return WiredBoxType.EffectExecuteCommand;
                case 68:
                    return WiredBoxType.EffectGiveExperience;
                case 69:
                    return WiredBoxType.ConditionIsJailed;
                case 70:
                    return WiredBoxType.ConditionIsDead;
                case 71:
                    return WiredBoxType.EffectApplyEffect;
                case 72:
                    return WiredBoxType.ConditionIsDriving;
                case 73:
                    return WiredBoxType.EffectDamageUser;
                case 74:
                    return WiredBoxType.EffectHealUser;
                case 75:
                    return WiredBoxType.ConditionHasVip;
                case 76:
                    return WiredBoxType.EffectSetMotto;
                case 77:
                    return WiredBoxType.EffectFreezeUser;
                case 78:
                    return WiredBoxType.EffectUnfreezeUser;
                case 79:
                    return WiredBoxType.ConditionIsSitting;
                case 80:
                    return WiredBoxType.EffectGiveHuntPoints;
                case 81:
                    return WiredBoxType.EffectGiveEnergy;
                case 82:
                    return WiredBoxType.EffectGiveArmor;
                case 83:
                    return WiredBoxType.ConditionHasWeapon;
                case 84:
                    return WiredBoxType.EffectGiveRPItem;
                case 85:
                    return WiredBoxType.ConditionHasRPItem;
                case 86:
                    return WiredBoxType.EffectSetRotation;
                case 87:
                    return WiredBoxType.ConditionIsIdle;
                case 88:
                    return WiredBoxType.ConditionIsDancing;
                case 89:
                    return WiredBoxType.EffectSetVariable;
                case 90:
                    return WiredBoxType.EffectVariableAdd;
                case 91:
                    return WiredBoxType.ConditionVariableIsEqual;
                case 92:
                    return WiredBoxType.EffectVariableSubtract;
                case 93:
                    return WiredBoxType.ConditionVariableIsGreaterThan;
                case 94:
                    return WiredBoxType.ConditionVariableIsLessThan;
                case 95:
                    return WiredBoxType.EffectMoveFurniXYZ;
                case 96:
                    return WiredBoxType.EffectGiveHanditem;
                case 97:
                    return WiredBoxType.EffectTeleportToRoom;
            }
        }

        public static WiredBoxType FromInteractionType(string type)
        {
            switch (type.ToLower())
            {
                case "wf_trg_enter_room": return WiredBoxType.TriggerRoomEnter;
                case "wf_trg_leave_room":
                case "wf_trg_exit_room":
                    return WiredBoxType.TriggerRoomEnter;
                case "wf_trg_says_something":
                case "wf_trg_says_something_contains":
                case "wf_trg_says_something_equal":
                case "wf_trg_says_smth_show":
                case "wf_trg_says_not_something":
                case "wf_trg_not_says_thing":
                    return WiredBoxType.TriggerUserSays;
                case "wf_trg_periodically":
                case "wf_trg_period_long":
                case "wf_trg_repeat_short":
                case "wf_trg_idk":
                case "wf_trg_idk_seg":
                case "wf_trg_anti_afk":
                case "wf_trg_afkkkdormeur":
                case "wf_trg_at_given_time":
                case "wf_trg_at_time_long":
                case "wf_trg_clock_counter":
                    return WiredBoxType.TriggerRepeat;
                case "wf_trg_state_changed":
                case "wf_trg_double_click_furni":
                case "wf_trg_stuff_state":
                    return WiredBoxType.TriggerStateChanges;
                case "wf_trg_walks_on_furni": return WiredBoxType.TriggerWalkOnFurni;
                case "wf_trg_walks_off_furni": return WiredBoxType.TriggerWalkOffFurni;
                case "wf_trg_game_starts": return WiredBoxType.TriggerGameStarts;
                case "wf_trg_game_ends": return WiredBoxType.TriggerGameEnds;
                case "wf_trg_collision":
                case "wf_trg_colision_user_other":
                case "wf_trg_colision_other_user":
                case "wf_trg_colision_team_user_other":
                case "wf_trg_colision_team_other_user":
                    return WiredBoxType.TriggerUserFurniCollision;
                case "wf_trg_chat_cmd_user":
                case "wf_trg_chat_cmd_other":
                    return WiredBoxType.TriggerUserSaysCommand;

                case "wf_act_show_message":
                case "wf_act_show_message_room":
                case "wf_act_deux_message":
                case "wf_act_alertbuble":
                    return WiredBoxType.EffectShowMessage;

                case "wf_act_teleport_to":
                case "wf_act_teleport_to_furni_habbo":
                case "wf_act_tp_furni_to_habbo":
                case "wf_act_toroom":
                case "wf_act_teportoroom":
                case "wf_act_toroom_staff":
                case "wf_act_teleport_yellow":
                case "wf_act_teleport_red":
                case "wf_act_teleport_blue":
                case "wf_act_teleport_green":
                    return WiredBoxType.EffectTeleportToFurni;

                case "wf_act_furni_to_furni":
                case "wf_act_furni_to_user":
                    return WiredBoxType.EffectTeleportToFurni;

                case "wf_act_toggle_state":
                case "wf_act_toggle_state_down":
                case "wf_act_toggle_to_rnd":
                case "wf_act_toggle_nega":
                case "wf_act_toggle_negat":
                case "wf_act_close_dice":
                case "wf_act_closes_dices":
                case "wf_act_toggle_moodlight":
                    return WiredBoxType.EffectToggleFurniState;
                case "wf_act_toggle_state_random":
                case "wf_act_toggle_random":
                    return WiredBoxType.EffectToggleFurniState;

                case "wf_act_kick_user": return WiredBoxType.EffectKickUser;

                case "wf_act_match_to_sshot":
                case "wf_act_match_to_sshot_xyz":
                case "wf_act_plus_match_furni_state":
                    return WiredBoxType.EffectMatchPosition;

                case "wf_act_move_rotate":
                case "wf_act_move_rotate_instant":
                case "wf_act_effect_mpu":
                    return WiredBoxType.EffectMoveAndRotate;

                case "wf_act_move_to_dir":
                case "wf_act_move_furni_xyz_slide":
                    return WiredBoxType.EffectMoveAndRotate;

                case "wf_act_chase":
                case "wf_act_super_chase":
                case "wf_act_furni_super_chase":
                    return WiredBoxType.EffectMoveFurniToNearestUser;
                case "wf_act_flee":
                    return WiredBoxType.EffectMoveFurniFromNearestUser;

                case "wf_act_mute_triggerer":
                    return WiredBoxType.EffectMuteTriggerer;

                case "wf_act_give_reward":
                    return WiredBoxType.EffectGiveReward;
                case "wf_act_give_score":
                case "wf_act_give_score_tm":
                case "wf_act_points":
                case "wf_act_set_points":
                case "wf_act_give_score_pp":
                case "wf_act_custom_addhighscore":
                case "wf_act_counter_points":
                case "wf_act_add_tag":
                    return WiredBoxType.EffectAddScore;

                case "wf_act_call_stacks":
                case "wf_act_call_stk_wc":
                case "wf_act_exe_condicion":
                case "wf_act_exe_flawless":
                case "wf_act_exe_super":
                case "wf_act_control":
                case "wf_act_control_id":
                case "wf_act_neg_call_stacks":
                    return WiredBoxType.EffectExecuteWiredStacks;

                case "wf_act_dont_chase_top":
                case "wf_act_collisioncase":
                    return WiredBoxType.EffectCollisionCase;
                case "wf_act_join_team": 
                    return WiredBoxType.EffectAddActorToTeam;
                case "wf_act_leave_team": return WiredBoxType.EffectRemoveActorFromTeam;

                case "wf_act_roller":
                case "wf_act_roller_speed":
                case "wf_act_set_roller_spd":
                    return WiredBoxType.EffectSetRollerSpeed;

                case "wf_act_regenerate_map": return WiredBoxType.EffectRegenerateMaps;

                case "wf_act_give_userbadge":
                case "wf_act_give_badge":
                    return WiredBoxType.EffectGiveUserBadge;

                case "wf_act_execute_command": return WiredBoxType.EffectExecuteCommand;
                case "wf_act_progress_achievement": return WiredBoxType.EffectGiveExperience;

                case "wf_act_rotationhabbo":
                case "wf_act_rotate_user":
                case "wf_act_tour_ne_ava":
                case "wf_act_move_rotate_user":
                    return WiredBoxType.EffectSetRotation;

                case "wf_act_give_currency": return WiredBoxType.EffectGiveCurrency;

                case "wf_cnd_furnis_hv_avtrs": return WiredBoxType.ConditionFurniHasUsers;
                case "wf_cnd_has_furni_on": return WiredBoxType.ConditionFurniHasFurni;
                case "wf_cnd_trggrer_on_frn": return WiredBoxType.ConditionTriggererOnFurni;
                case "wf_cnd_actor_in_group": return WiredBoxType.ConditionIsGroupMember;
                case "wf_cnd_not_in_group": return WiredBoxType.ConditionIsNotGroupMember;
                case "wf_cnd_not_trggrer_on": return WiredBoxType.ConditionTriggererNotOnFurni;
                case "wf_cnd_not_hv_avtrs": return WiredBoxType.ConditionFurniHasNoUsers;
                case "wf_cnd_habbo_owns_badge":
                case "wf_cnd_wearing_badge":
                case "wf_cnd_wearing_badg":
                case "wf_cnd_has_badge_or_mission":
                    return WiredBoxType.ConditionIsWearingBadge;
                case "wf_cnd_wearing_effect": return WiredBoxType.ConditionIsWearingFX;
                case "wf_cnd_not_wearing_b":
                case "wf_cnd_not_habbo_owns_badge":
                case "wf_cnd_not_wringbdg":
                    return WiredBoxType.ConditionIsNotWearingBadge;
                case "wf_cnd_not_wearing_fx": return WiredBoxType.ConditionIsNotWearingFX;
                case "wf_cnd_match_snapshot":
                case "wf_cnd_match_snapshot_xyz":
                    return WiredBoxType.ConditionMatchStateAndPosition;
                case "wf_cnd_not_match_snap":
                case "wf_cnd_not_match_snap_xyz":
                case "wf_cnd_match_snap_no_xyz":
                    return WiredBoxType.ConditionDontMatchStateAndPosition;
                case "wf_cnd_user_count_in": return WiredBoxType.ConditionUserCountInRoom;
                case "wf_cnd_not_user_count": return WiredBoxType.ConditionUserCountDoesntInRoom;
                case "wf_cnd_stuff_is": return WiredBoxType.ConditionFurniTypeMatches;
                case "wf_cnd_not_stuff_is": return WiredBoxType.ConditionFurniTypeDoesntMatch;
                case "wf_cnd_not_furni_on": return WiredBoxType.ConditionFurniHasNoFurni;
                case "wf_cnd_has_handitem":
                case "wf_cnd_handitem":
                    return WiredBoxType.ConditionActorHasHandItemBox;
                case "wf_cnd_not_handitem":
                case "wf_cnd_not_has_handitem":
                    return WiredBoxType.ConditionNotActorHasHandItemBox;
                case "wf_cnd_actor_in_team":
                case "wf_cnd_super_wiredequipo":
                    return WiredBoxType.ConditionActorIsInTeamBox;

                case "wf_cnd_has_job": return WiredBoxType.ConditionHasJob;
                case "wf_cnd_is_night": return WiredBoxType.ConditionIsNight;
                case "wf_cnd_is_day": return WiredBoxType.ConditionIsDay;
                case "wf_cnd_is_dead": return WiredBoxType.ConditionIsDead;
                case "wf_cnd_is_jailed": return WiredBoxType.ConditionIsJailed;
                case "wf_cnd_has_rank":
                case "wf_cnd_user_hasrights":
                case "wf_cnd_habbo_has_rank":
                    return WiredBoxType.ConditionHasVip;
                case "wf_cnd_is_sitting": return WiredBoxType.ConditionIsSitting;
                case "wf_cnd_has_weapon": return WiredBoxType.ConditionHasWeapon;
                case "wf_cnd_has_rp_item": return WiredBoxType.ConditionHasRPItem;
                case "wf_cnd_is_afk": return WiredBoxType.ConditionIsIdle;
                case "wf_cnd_is_dancingbb": return WiredBoxType.ConditionIsDancing;
                case "wf_cnd_is_driving": return WiredBoxType.ConditionIsDriving;

                case "wf_act_enable_cstm": return WiredBoxType.EffectApplyEffect;
                case "wf_act_damage_user": return WiredBoxType.EffectDamageUser;
                case "wf_act_heal_user": return WiredBoxType.EffectHealUser;
                case "wf_act_sign_cstm": return WiredBoxType.EffectSetMotto;
                case "wf_act_freeze_cstm":
                case "wf_act_freeze":
                case "wf_act_freeze_delay":
                case "wf_cstm_freeze":
                    return WiredBoxType.EffectFreezeUser;
                case "wf_act_unfreeze_cstm":
                case "wf_act_unfreez":
                case "wf_act_unfreeze_delay":
                case "wf_cstm_ufreez":
                    return WiredBoxType.EffectUnfreezeUser;
                case "wf_act_give_hunt_points": return WiredBoxType.EffectGiveHuntPoints;
                case "wf_act_give_energy": return WiredBoxType.EffectGiveEnergy;
                case "wf_act_give_armor": return WiredBoxType.EffectGiveArmor;
                case "wf_act_give_rp_item": return WiredBoxType.EffectGiveRPItem;

                case "wf_xtra_random": return WiredBoxType.AddonRandom;
                case "wf_xtra_unseen": return WiredBoxType.AddonUnseen;
                case "wf_xtra_animation_time": return WiredBoxType.AddonAnimationTime;
                case "wf_xtra_execute_in_order": return WiredBoxType.AddonExecuteInOrder;
                case "wf_xtra_execution_limit": return WiredBoxType.AddonExecutionLimit;
                case "wf_xtra_filter_furni": return WiredBoxType.AddonFilterFurni;
                case "wf_xtra_filter_user": return WiredBoxType.AddonFilterUser;
                case "wf_xtra_mov_carry_users": return WiredBoxType.AddonMoveCarryUsers;
                case "wf_xtra_mov_no_animation": return WiredBoxType.AddonMoveNoAnimation;
                case "wf_xtra_mov_physics": return WiredBoxType.AddonMovePhysics;
                case "wf_xtra_or_eval": return WiredBoxType.AddonOrEval;
                case "wf_xtra_text_output_furni_name": return WiredBoxType.AddonTextOutputFurniName;
                case "wf_xtra_text_output_username": return WiredBoxType.AddonTextOutputUsername;
                case "wf_xtra_unseenrandom":
                case "wf_xtra_unseen_random":
                case "wf_xtra_pt_unseenrandom":
                case "wf_xtra_comment":
                case "wf_xtra_diagonalcollision":
                case "wf_xtra_fleche":
                case "wf_xtra_moveitemwithusers":
                case "wf_xtra_sequence":
                case "wf_xtra_usability":
                case "wf_xtra_usabilitycond":
                case "wf_xtra_useraround":
                case "wf_xtra_userbehind":
                case "wf_xtra_userinfront":
                case "wf_xtra_xixixi":
                case "wf_xtra_all_eval":
                case "wf_xtra_all_eval_not":
                case "wf_xtra_and_eval":
                case "wf_xtra_and_eval_not":
                case "wf_xtra_not_animate_furni":
                case "wf_xtra_not_animate_user":
                case "wf_xtra_not_effect_teleport":
                case "wf_xtra_noteffecttp":
                case "wf_xtra_notfreezetp":
                case "wf_xtra_notteam":
                case "wf_xtra_one_condition":
                    return WiredBoxType.AddonRandomEffect;

                case "wf_highscore": return WiredBoxType.SpecialHighscore;

                case "wf_act_set_variable": return WiredBoxType.EffectSetVariable;
                case "wf_act_variable_add": return WiredBoxType.EffectVariableAdd;
                case "wf_cnd_variable_is_equal": return WiredBoxType.ConditionVariableIsEqual;
                case "wf_act_variable_sub": return WiredBoxType.EffectVariableSubtract;
                case "wf_cnd_variable_gt": return WiredBoxType.ConditionVariableIsGreaterThan;
                case "wf_cnd_variable_lt": return WiredBoxType.ConditionVariableIsLessThan;

                case "wf_act_bot_teleport": return WiredBoxType.EffectTeleportBotToFurniBox;
                case "wf_act_bot_clothes": return WiredBoxType.EffectBotChangesClothesBox;
                case "wf_act_bot_move": return WiredBoxType.EffectBotMovesToFurniBox;
                case "wf_act_bot_talk": return WiredBoxType.EffectBotCommunicatesToAllBox;
                case "wf_act_bot_talk_to_avatar": return WiredBoxType.EffectBotCommunicatesToUserBox;
                case "wf_act_bot_follow_avatar": return WiredBoxType.EffectBotFollowsUserBox;
                case "wf_act_bot_give_handitem": return WiredBoxType.EffectBotGivesHanditemBox;
                case "wf_act_move_furni_xyz": return WiredBoxType.EffectMoveFurniXYZ;
                case "wf_act_give_handitem": return WiredBoxType.EffectGiveHanditem;
                case "wf_act_teleport_to_room": return WiredBoxType.EffectTeleportToRoom;

                default: return WiredBoxType.None;
            }
        }

        public static int GetWiredId(WiredBoxType Type)
        {
            switch (Type)
            {
                case WiredBoxType.TriggerUserSays:
                case WiredBoxType.TriggerUserSaysCommand:
                    return 0;
                case WiredBoxType.TriggerWalkOnFurni:
                    return 1;
                case WiredBoxType.TriggerWalkOffFurni:
                    return 2;
                case WiredBoxType.TriggerStateChanges:
                    return 4;
                case WiredBoxType.TriggerRepeat:
                    return 6;
                case WiredBoxType.TriggerRoomEnter:
                    return 7;
                case WiredBoxType.TriggerGameStarts:
                    return 8;
                case WiredBoxType.TriggerGameEnds:
                    return 9;
                case WiredBoxType.TriggerUserFurniCollision:
                    return 11;

                case WiredBoxType.EffectToggleFurniState:
                    return 0;
                case WiredBoxType.EffectMatchPosition:
                    return 3;
                case WiredBoxType.EffectMoveAndRotate:
                    return 4;
                case WiredBoxType.EffectAddScore:
                    return 6;
                case WiredBoxType.EffectShowMessage:
                case WiredBoxType.EffectGiveUserBadge:
                case WiredBoxType.EffectRegenerateMaps:
                case WiredBoxType.EffectSetRollerSpeed:
                    return 7;
                case WiredBoxType.EffectTeleportToFurni:
                    return 8;
                case WiredBoxType.EffectAddActorToTeam:
                    return 9;
                case WiredBoxType.EffectRemoveActorFromTeam:
                    return 10;
                case WiredBoxType.EffectMoveFurniToNearestUser:
                    return 11;
                case WiredBoxType.EffectMoveFurniFromNearestUser:
                    return 12;
                case WiredBoxType.EffectGiveReward:
                    return 17;
                case WiredBoxType.EffectExecuteWiredStacks:
                    return 18;
                case WiredBoxType.EffectKickUser:
                    return 19;
                case WiredBoxType.EffectMuteTriggerer:
                    return 20;
                case WiredBoxType.EffectTeleportBotToFurniBox:
                    return 21;
                case WiredBoxType.EffectBotMovesToFurniBox:
                    return 22;
                case WiredBoxType.EffectBotCommunicatesToAllBox:
                    return 23;
                case WiredBoxType.EffectBotGivesHanditemBox:
                    return 24;
                case WiredBoxType.EffectBotFollowsUserBox:
                    return 25;
                case WiredBoxType.EffectBotChangesClothesBox:
                    return 26;
                case WiredBoxType.EffectBotCommunicatesToUserBox:
                    return 27;
                case WiredBoxType.EffectGiveCurrency:
                    return 28;
                case WiredBoxType.EffectExecuteCommand:
                    return 31;
                case WiredBoxType.EffectGiveExperience:
                    return 32;
                case WiredBoxType.EffectApplyEffect:
                    return 34;
                case WiredBoxType.EffectDamageUser:
                case WiredBoxType.EffectHealUser:
                    return 36;
                case WiredBoxType.EffectSetMotto:
                    return 38;
                case WiredBoxType.EffectFreezeUser:
                case WiredBoxType.EffectUnfreezeUser:
                    return 39;
                case WiredBoxType.EffectGiveHuntPoints:
                    return 41;
                case WiredBoxType.EffectGiveEnergy:
                case WiredBoxType.EffectGiveArmor:
                    return 42;
                case WiredBoxType.EffectGiveRPItem:
                    return 44;
                case WiredBoxType.EffectCollisionCase:
                    return 45;
                case WiredBoxType.EffectSetRotation:
                    return 46;

                case WiredBoxType.ConditionMatchStateAndPosition:
                    return 0;
                case WiredBoxType.ConditionFurniHasUsers:
                    return 1;
                case WiredBoxType.ConditionTriggererOnFurni:
                    return 2;
                case WiredBoxType.ConditionUserCountInRoom:
                    return 5;
                case WiredBoxType.ConditionActorIsInTeamBox:
                    return 6;
                case WiredBoxType.ConditionFurniHasFurni:
                    return 7;
                case WiredBoxType.ConditionFurniTypeMatches:
                    return 8;
                case WiredBoxType.ConditionIsGroupMember:
                    return 10;
                case WiredBoxType.ConditionIsWearingBadge:
                    return 11;
                case WiredBoxType.ConditionIsWearingFX:
                    return 12;
                case WiredBoxType.ConditionDontMatchStateAndPosition:
                    return 13;
                case WiredBoxType.ConditionFurniHasNoUsers:
                    return 14;
                case WiredBoxType.ConditionTriggererNotOnFurni:
                    return 15;
                case WiredBoxType.ConditionUserCountDoesntInRoom:
                    return 16;
                case WiredBoxType.ConditionFurniHasNoFurni:
                    return 18;
                case WiredBoxType.ConditionFurniTypeDoesntMatch:
                    return 19;
                case WiredBoxType.ConditionIsNotGroupMember:
                    return 21;
                case WiredBoxType.ConditionIsNotWearingBadge:
                    return 22;
                case WiredBoxType.ConditionIsNotWearingFX:
                    return 23;
                case WiredBoxType.ConditionActorHasHandItemBox:
                    return 25;
                case WiredBoxType.ConditionNotActorHasHandItemBox:
                    return 31;

                case WiredBoxType.ConditionHasJob:
                    return 29;
                case WiredBoxType.ConditionIsNight:
                case WiredBoxType.ConditionIsDay:
                    return 30;
                case WiredBoxType.ConditionIsJailed:
                case WiredBoxType.ConditionIsDead:
                    return 33;
                case WiredBoxType.ConditionIsDriving:
                    return 35;
                case WiredBoxType.ConditionHasVip:
                    return 37;
                case WiredBoxType.ConditionIsSitting:
                    return 40;
                case WiredBoxType.ConditionHasWeapon:
                    return 43;
                case WiredBoxType.ConditionHasRPItem:
                    return 45;
                case WiredBoxType.ConditionIsIdle:
                case WiredBoxType.ConditionIsDancing:
                    return 47;

                case WiredBoxType.SpecialHighscore:
                    return 48;
                case WiredBoxType.AddonFilterFurni:
                    return 56;
                case WiredBoxType.AddonFilterUser:
                    return 57;
                case WiredBoxType.AddonMoveCarryUsers:
                    return 58;
                case WiredBoxType.AddonMoveNoAnimation:
                    return 59;
                case WiredBoxType.AddonAnimationTime:
                    return 60;
                case WiredBoxType.AddonMovePhysics:
                    return 61;
                case WiredBoxType.AddonUnseen:
                    return 62;
                case WiredBoxType.AddonRandom:
                    return 63;
                case WiredBoxType.AddonExecuteInOrder:
                    return 64;
                case WiredBoxType.AddonExecutionLimit:
                    return 65;
                case WiredBoxType.AddonOrEval:
                    return 66;
                case WiredBoxType.AddonTextOutputUsername:
                    return 67;
                case WiredBoxType.AddonTextOutputFurniName:
                    return 68;

                case WiredBoxType.EffectSetVariable:
                    return 89;
                case WiredBoxType.EffectVariableAdd:
                    return 90;
                case WiredBoxType.ConditionVariableIsEqual:
                    return 91;
                case WiredBoxType.EffectVariableSubtract:
                    return 92;
                case WiredBoxType.ConditionVariableIsGreaterThan:
                    return 93;
                case WiredBoxType.ConditionVariableIsLessThan:
                    return 94;
                case WiredBoxType.EffectMoveFurniXYZ:
                    return 95;
                case WiredBoxType.EffectGiveHanditem:
                    return 96;
                case WiredBoxType.EffectTeleportToRoom:
                    return 97;
            }
            return 0;
        }

        public static List<int> ContainsBlockedTrigger(IWiredItem box, ICollection<IWiredItem> triggers)
        {
            List<int> blockedItems = new();
            if (box.Type != WiredBoxType.EffectShowMessage && box.Type != WiredBoxType.EffectMuteTriggerer && box.Type != WiredBoxType.EffectTeleportToFurni && box.Type != WiredBoxType.EffectKickUser && box.Type != WiredBoxType.ConditionTriggererOnFurni)
                return blockedItems;

            foreach (IWiredItem item in triggers)
            {
                if (item.Type == WiredBoxType.TriggerRepeat)
                {
                    if (!blockedItems.Contains(item.Item.GetBaseItem().SpriteId))
                        blockedItems.Add(item.Item.GetBaseItem().SpriteId);
                }
            }
            return blockedItems;
        }

        public static bool IsTriggerType(WiredBoxType type) => type.ToString().StartsWith("Trigger");
        public static bool IsEffectType(WiredBoxType type) => type.ToString().StartsWith("Effect");
        public static bool IsConditionType(WiredBoxType type) => type.ToString().StartsWith("Condition");
        public static bool IsAddonType(WiredBoxType type) => type.ToString().StartsWith("Addon");

        public static List<int> ContainsBlockedEffect(IWiredItem box, ICollection<IWiredItem> effects)
        {
            List<int> blockedItems = new();
            if (box.Type != WiredBoxType.TriggerRepeat)
                return blockedItems;

            bool hasMoveRotate = effects.Any(x => x.Type == WiredBoxType.EffectMoveAndRotate);
            bool hasMoveNear = effects.Any(x => x.Type == WiredBoxType.EffectMoveFurniToNearestUser);

            foreach (IWiredItem item in effects)
            {
                if (item.Type == WiredBoxType.EffectKickUser || item.Type == WiredBoxType.EffectMuteTriggerer || item.Type == WiredBoxType.EffectShowMessage || item.Type == WiredBoxType.EffectTeleportToFurni || item.Type == WiredBoxType.EffectBotFollowsUserBox)
                {
                    if (!blockedItems.Contains(item.Item.GetBaseItem().SpriteId))
                        blockedItems.Add(item.Item.GetBaseItem().SpriteId);
                }
                else if ((item.Type == WiredBoxType.EffectMoveFurniToNearestUser && hasMoveRotate) || (item.Type == WiredBoxType.EffectMoveAndRotate && hasMoveNear))
                {
                    if (!blockedItems.Contains(item.Item.GetBaseItem().SpriteId))
                        blockedItems.Add(item.Item.GetBaseItem().SpriteId);
                }
            }
            return blockedItems;
        }
    }
}
