using UnityEngine;

// Each entry scene owns a serialized reference, so builds include the art without Resources loading.
[DefaultExecutionOrder(-10000)]
public sealed class DoodleSkinBindings : MonoBehaviour
{
    [SerializeField] private DoodleSkinAssets assets;

    private void Awake() => DoodleLabSkin.Register(assets);
}
