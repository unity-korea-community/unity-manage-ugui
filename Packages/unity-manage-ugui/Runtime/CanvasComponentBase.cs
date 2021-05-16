using System.Collections;
using Core;
using UnityEngine;

namespace Core
{
    public class CanvasComponentBase : MonoBehaviour, ICanvas
    {
        public CanvasManager canvasManager { get; set; }

        virtual public void Init() { }

        virtual public IEnumerator OnShowCanvasCoroutine()
        {
            yield break;
        }

        virtual public IEnumerator OnHideCanvasCoroutine()
        {
            yield break;
        }
    }
}
