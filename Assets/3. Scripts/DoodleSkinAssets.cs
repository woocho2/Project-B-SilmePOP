using System;
using UnityEngine;

[CreateAssetMenu(menuName = "SlimeGame/Skin Assets", fileName = "SlimeSkinAssets")]
public sealed class DoodleSkinAssets : ScriptableObject
{
    [Serializable]
    public struct Binding
    {
        public string name;
        public Sprite sprite;
    }

    public Binding[] sprites;
}
