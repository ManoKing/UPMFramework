using UnityEngine;
using System.Linq;
using System.Collections;
public class RefTypes : MonoBehaviour
{
    // 添加引用，防止被裁剪
    void RefUnityEngine()
    {
        IOrderedEnumerable<IEnumerable> sortedTreeRootEnumerable;

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        Handheld.Vibrate();
#endif
    }
}
