using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class Utility 
{
    public static List<V> GetValueList<K,V>(this System.Collections.Generic.Dictionary<K, V>.ValueCollection collection)
    {
        List<V> valueList = new List<V>();
        foreach(var value in collection)
        {
            valueList.Add(value);
        }
        return valueList;
    }
}

public static class MaterialPool
{
    private static Dictionary<string,Material> m_Dic = new Dictionary<string, Material>(8); 
    public static Material GetMaterial(string name)
    {
            if (!m_Dic.ContainsKey(name) || !m_Dic[name])
                m_Dic[name] = new Material(Shader.Find(name));
            return m_Dic[name];
    }
}


