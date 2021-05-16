using System.Collections;

namespace Core
{
    /// <summary>
    /// 캔버스의 상태
    /// </summary>
    public enum CanvasStatus
    {
        NotInit,
        Awake,
        Show,
        HideAnimationPlaying,
        Hide,
    }

    public class CanvasWrapper
    {
        public CanvasStatus status { get; private set; } = CanvasStatus.NotInit;
        public ICanvas canvasInstance { get; private set; }

        public bool isShow => status == CanvasStatus.Show;

        public CanvasWrapper Init(ICanvas canvasInstance)
        {
            this.canvasInstance = canvasInstance;

            return this;
        }

        public void Awake(CanvasManager canvasManager)
        {
            canvasInstance.Init();
            status = CanvasStatus.Awake;
        }

        public void SetActive(bool active)
        {
            canvasInstance.gameObject.SetActive(active);
            status = active ? CanvasStatus.Show : CanvasStatus.Hide;
        }

        public void SetStatus(CanvasStatus status)
        {
            this.status = status;
        }

        public IEnumerator OnShowCanvasCoroutine() => canvasInstance.OnShowCanvasCoroutine();
        public IEnumerator OnHideCanvasCoroutine() => canvasInstance.OnHideCanvasCoroutine();
    }
}
