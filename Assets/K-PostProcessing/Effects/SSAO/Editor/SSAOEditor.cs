using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine;


namespace FPostProcessing
{
    [PostProcessEditor(typeof(SSAO))]
    public class SSAOEditor : PostProcessEffectEditor<SSAO>
    {
        SerializedParameterOverride SampleCount;
        SerializedParameterOverride ThicknessStrength;
        SerializedParameterOverride OnlyShowAO;
        public override void OnEnable()
        {
            SampleCount = FindParameterOverride(x => x.SampleCount);
            OnlyShowAO = FindParameterOverride(x=>x.OnlyShowAO);
            ThicknessStrength = FindParameterOverride(x=>x.ThicknessStrength);
        }

        public override string GetDisplayTitle()
        {
            return KPostProcessingEditorUtility.DISPLAY_TITLE_PREFIX + base.GetDisplayTitle();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(SampleCount);
            PropertyField(ThicknessStrength);
            PropertyField(OnlyShowAO);
        }
    }
}
