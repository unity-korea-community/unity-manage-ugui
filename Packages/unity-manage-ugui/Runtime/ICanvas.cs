using System.Collections;
using UnityEngine;

namespace Core
{
    public interface ICanvas
    {
        string name { get; }
        Transform transform { get; }
        GameObject gameObject { get; }
        CanvasManager canvasManager { get; set; }

        void Init();
        IEnumerator OnShowCanvasCoroutine();
        IEnumerator OnHideCanvasCoroutine();
    }

    public static class ICanvasHelper
    {
        public static T Cast<T>(this ICanvas canvas)
            where T : class, ICanvas
        {
            return canvas as T;
        }

        public static void Hide(this ICanvas canvas)
        {
            canvas.canvasManager.Hide(canvas);
        }
    }
}
