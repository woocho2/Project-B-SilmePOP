using UnityEngine;

// 씬에 하나뿐인 매니저의 공용 틀.
// 쓰는 법: public class GameManager : Singleton<GameManager>
//
// 자식이 Awake를 덮어쓸 때는 base.Awake()를 먼저 부르고, 중복으로 지워지는 쪽이면 바로 끝냄:
//     protected override void Awake()
//     {
//         base.Awake();
//         if (Instance != this) return;
//         ... 초기화 ...
//     }
// OnDestroy를 덮어쓸 때도 base.OnDestroy()를 꼭 부름 (안 부르면 Instance가 지워진 오브젝트를 가리킴).
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    // true면 씬이 바뀌어도 남음. NetworkManager처럼 접속을 유지해야 하는 매니저만 true로 덮어씀.
    protected virtual bool PersistAcrossScenes => false;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 씬을 다시 열면 같은 매니저가 또 생김 → 먼저 있던 것을 남기고 새것을 지움
            Destroy(gameObject);
            return;
        }

        Instance = this as T;

        if (PersistAcrossScenes)
        {
            // DontDestroyOnLoad는 맨 위(루트) 오브젝트에만 동작함
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
