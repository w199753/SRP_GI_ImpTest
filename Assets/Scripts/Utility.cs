using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class Utility
{
    public static List<V> GetValueList<K, V>(this System.Collections.Generic.Dictionary<K, V>.ValueCollection collection)
    {
        List<V> valueList = new List<V>();
        foreach (var value in collection)
        {
            valueList.Add(value);
        }
        return valueList;
    }

    public static void DrawBound(Bounds bounds, Color color)
    {
        var verts = new Vector2[]
        {
                    new Vector2(-1, -1),
                    new Vector2(1, -1),
                    new Vector2(1, 1),
                    new Vector2(-1, 1),
                    new Vector2(-1, -1),
        };
        for (var i = 0; i < 4; i++)
        {
            Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, 1)), color);
            Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, -1)), color);

            Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1)), color);
        }
    }

}

public static class MaterialPool
{
    private static Dictionary<string, Material> m_Dic = new Dictionary<string, Material>(8);
    public static Material GetMaterial(string name)
    {
        if (!m_Dic.ContainsKey(name) || !m_Dic[name])
            m_Dic[name] = new Material(Shader.Find(name));
        return m_Dic[name];
    }
}

public static class ShaderTagConstant
{
    public static ShaderTagId BaseShaderTagID = new ShaderTagId("FRP_BASE");
    public static ShaderTagId ShadowCasterTagID = new ShaderTagId("FRP_Caster_Shadow");
    public static ShaderTagId RsmNormalCasterTagID = new ShaderTagId("FRP_Caster_Normal");
    public static ShaderTagId RsmFluxCasterTagID = new ShaderTagId("FRP_Caster_Flux");
    public static ShaderTagId RsmWorldPosCasterTagID = new ShaderTagId("FRP_Caster_WorldPos");
    
}


