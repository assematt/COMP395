using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// This ScriptableObject stores the list of flags:
///     - The country flag sprite
///     - The flag's country name
/// </summary>
[CreateAssetMenu(fileName = "FlagsDatabase", menuName = "Tools/Create Flags Database", order = 1)]
public class FlagsDatabase : ScriptableObject
{
    [Serializable]
    public class FlagData
    {
        public Sprite flagSprite;
        public string flagCountry;
    }

    [TableList]
    public List<FlagData> flags;
}
