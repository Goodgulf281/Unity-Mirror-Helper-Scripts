using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Goodgulf.Utilities
{
    public class DontDestroy : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}
