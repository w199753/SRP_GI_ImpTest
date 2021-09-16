﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ReplaceMaterial : EditorWindow
{

    public GameObject rootObject;
    Dictionary<string,Material> resultMatDic = new Dictionary<string, Material>(16);
    private bool AssertResource()
    {
        if (rootObject == null) { this.ShowNotification(new GUIContent("RootObject 不能为空!")); return false; };
        return true;
    }

    [MenuItem("FTools/Prefilter")]
    static void AddWindow()
    {
        Rect wr = new Rect(0, 0, 300, 500);
        ReplaceMaterial window = (ReplaceMaterial)EditorWindow.GetWindowWithRect(typeof(ReplaceMaterial), wr, true, "ReplaceMaterial");
        window.Show();
    }
    private void OnGUI()
    {
        GUILayout.BeginVertical();
        rootObject = EditorGUILayout.ObjectField("RootObj",rootObject,typeof(GameObject),true) as GameObject;
        if(GUILayout.Button("Replace Object Material",GUILayout.Width(300)))
        {
            if(AssertResource()) Replace(rootObject);
            resultMatDic.Clear();
        }

        GUILayout.EndVertical();
    }

    private void DoReplace()
    {
        
    }

    private void Replace(GameObject root)
    {
        var meshRender = root.GetComponent<MeshRenderer>();
        if(meshRender)
        { 
            var matList = meshRender.sharedMaterials;
            for(int i = 0; i<matList.Length;i++)
            {
                if(resultMatDic.ContainsKey(matList[i].name))
                {
                    meshRender.materials[i] = resultMatDic[matList[i].name];
                }
                else
                {
                    var mainTex = matList[i].mainTexture;
                    var mainTexName = mainTex.name;
                    int idx = mainTexName.LastIndexOf("_");
                    if(idx>=0)
                    {
                        Debug.Log("fzy name:"+mainTexName.Substring(0,idx));
                    }
                    else
                    {
                        Debug.Log("fzy name:"+mainTexName);
                    }
                    //var mat = new Material(Shader.Find(""));
                    //mat.SetTexture
                    //meshRender.materials[i] = mat;
                    //resultMatDic.Add(matList[i].name,mat);
                }
                //var tt = m.mainTexture;
                //var t = m.GetTexture("_MainTex");
                //Debug.Log("fzy "+m.name+" ,"+t.name+" ,"+tt.name);
            }
        }
        var count = root.transform.childCount;
        for(int i=0;i<count;i++)
        {
            var child = root.transform.GetChild(i).gameObject;
            Replace(child);
        }

    }

}