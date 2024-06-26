﻿#if PLAYMAKER
#pragma warning disable CS0618
using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace EasyMobile.PlayerMaker.Actions
{
    [ActionCategory("Easy Mobile - Utilities")]
    [Tooltip("Disables the rating request dialog show that it can't be shown anymore.")]
    public class MobileNativeRatingRequest_DisableRatingRequest : FsmStateAction
    {
        public override void OnEnter()
        {
            StoreReview.DisableRatingRequest();
            Finish();
        }
    }
}
#pragma warning restore CS0618
#endif
