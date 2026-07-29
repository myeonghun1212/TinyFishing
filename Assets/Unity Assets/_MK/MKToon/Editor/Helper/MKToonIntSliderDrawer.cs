//////////////////////////////////////////////////////
// MK Toon Editor Int Slider Drawer        			//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2020 All rights reserved.            //
//////////////////////////////////////////////////////

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace MK.Toon.Editor
{
    public class MKToonIntSliderDrawer : MK.Toon.Editor.MaterialPropertyDrawer
    {
        public MKToonIntSliderDrawer(GUIContent ui) : base(ui) {}
        public MKToonIntSliderDrawer() : base(GUIContent.none) {}

        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            EditorGUI.showMixedValue = prop.hasMixedValue;
            int intValue = (int) prop.floatValue;
            EditorGUI.BeginChangeCheck();

            intValue = EditorGUI.IntSlider(position, new GUIContent(label, _guiContent.tooltip), intValue, (int) prop.rangeLimits.x, (int) prop.rangeLimits.y);

            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = intValue;
            }
            EditorGUI.showMixedValue = false;
        }
    }
    public class MKToonLightBandsDrawer : MKToonIntSliderDrawer
    {
        public MKToonLightBandsDrawer() : base(UI.lightBands) {}
    }
    public class MKToonStencilRefDrawer : MKToonIntSliderDrawer
    {
        public MKToonStencilRefDrawer() : base(UI.stencilRef) {}
    }
    public class MKToonStencilReadMaskDrawer : MKToonIntSliderDrawer
    {
        public MKToonStencilReadMaskDrawer() : base(UI.stencilReadMask) {}
    }
    public class MKToonStencilWriteMaskDrawer : MKToonIntSliderDrawer
    {
        public MKToonStencilWriteMaskDrawer() : base(UI.stencilWriteMask) {}
    }
    public class MKToonRenderPriorityDrawer : MKToonIntSliderDrawer
    {
        public MKToonRenderPriorityDrawer() : base(UI.renderPriority) {}
    }
}
#endif