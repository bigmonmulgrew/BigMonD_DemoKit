using UnityEngine;

namespace Utils
{
    public class HashPrint : MonoBehaviour
    {
        void Start()
        {
            Debug.Log(HashUtils.GetObjectHash(gameObject));
        }
    }
}