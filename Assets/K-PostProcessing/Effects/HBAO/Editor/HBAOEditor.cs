using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace FPostProcessing
{
    [PostProcessEditor(typeof(HBAO))]
    public class HBAOEditor : PostProcessEffectEditor<HBAO>
    {
        SerializedParameterOverride SampleCount;
        SerializedParameterOverride ThicknessStrength;
        SerializedParameterOverride OnlyShowAO;
        
        public override void OnEnable()
        {
            SampleCount = FindParameterOverride(x => x.SampleCount);
            OnlyShowAO = FindParameterOverride(x=>x.OnlyShowAO);
            ThicknessStrength = FindParameterOverride(x=>x.ThicknessStrength);
            // SampleRange = FindParameterOverride(x=>x.SampleRange);
            // SampleBias = FindParameterOverride(x=>x.SampleBias);
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
            // PropertyField(SampleRange);
            // PropertyField(SampleBias);
        }
    }

}
