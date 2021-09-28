using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UNKO.Utils;

namespace UNKO.ManageUGUI
{
    public interface ICanvasManager
    {
#pragma warning disable IDE1006
        string name { get; }
        Transform transform { get; }
        GameObject gameObject { get; }
#pragma warning restore IDE1006

        bool IsShow(string canvasName);
        ICanvas Show(string canvasName);
        ICanvas Show(ICanvas canvas);
        void Hide(string canvasName);
        void Hide(ICanvas canvas);
    }

    public class CanvasManager<TDerived, TCanvasName> : SingletonComponentBase<TDerived>, ICanvasManager
        where TDerived : CanvasManager<TDerived, TCanvasName>
        where TCanvasName : struct, System.Enum
    {
        private Dictionary<TCanvasName, List<CanvasWrapper>> _canvasInstance = new Dictionary<TCanvasName, List<CanvasWrapper>>();
        private Dictionary<ICanvas, (TCanvasName name, CanvasWrapper wrapper)> _instanceMatch = new Dictionary<ICanvas, (TCanvasName, CanvasWrapper)>();

        private SimplePool<CanvasWrapper> _canvasWrapperPool = new SimplePool<CanvasWrapper>(new CanvasWrapper(), 10);
        public bool IsReady { get; private set; }

        public void Init(bool active)
        {
            foreach (var canvasList in _canvasInstance.Values)
            {
                foreach (CanvasWrapper canvasValue in canvasList)
                    canvasValue.SetActive(active);
            }
        }

        public CanvasWrapper AddCanvasInstance(TCanvasName canvasName, ICanvas canvasInstance)
        {
            if (_instanceMatch.TryGetValue(canvasInstance, out var wrapper))
            {
                return wrapper.wrapper;
            }

            if (_canvasInstance.TryGetValue(canvasName, out var list) == false)
            {
                list = new List<CanvasWrapper>();
                _canvasInstance.Add(canvasName, list);
            }

            wrapper = (canvasName, _canvasWrapperPool.Spawn().Init(canvasInstance));
            _instanceMatch.Add(canvasInstance, wrapper);
            list.Add(wrapper.wrapper);

            canvasInstance.canvasManager = this;
            wrapper.wrapper.Awake();

            return wrapper.wrapper;
        }

        public ICanvas Show(string canvasName)
        {
            if (System.Enum.TryParse(canvasName, out TCanvasName canvasNameEnum) == false)
            {
                Debug.LogError($"{name} not found canvasInstance:{canvasName}", this);
                return null;
            }

            return Show(canvasNameEnum);
        }

        public ICanvas Show(TCanvasName canvasName)
        {
            CanvasWrapper canvasWrapper = GetCanvasWrapper(canvasName, canvas => canvas.IsShow == false);
            if (canvasWrapper == null)
                canvasWrapper = OnCreateInstance(canvasName);

            if (canvasWrapper != null)
            {
                canvasWrapper.SetActive(true);
                StartCoroutine(canvasWrapper.OnShowCanvasCoroutine());
                OnShowCanvas(canvasName, canvasWrapper.CanvasInstance);
            }

            return canvasWrapper.CanvasInstance;
        }

        public ICanvas Show(ICanvas canvas)
        {
            if (_instanceMatch.TryGetValue(canvas, out var canvasWrapper) == false)
            {
                Debug.LogError($"CanvasManager({name}) not found canvasInstance:{canvas.gameObject.name}", this);
                return canvas;
            }

            string canvasName = canvas.gameObject.name;
            if (System.Enum.TryParse(canvasName, out TCanvasName canvasNameEnum) == false)
            {
                Debug.LogError($"{name} not found canvasInstance:{canvasName}", this);
                return canvas;
            }

            if (canvasWrapper.wrapper != null)
            {
                canvasWrapper.wrapper.SetActive(true);
                StartCoroutine(canvasWrapper.wrapper.OnShowCanvasCoroutine());
                OnShowCanvas(canvasNameEnum, canvasWrapper.wrapper.CanvasInstance);
            }

            return canvasWrapper.wrapper.CanvasInstance;
        }

        public void Hide(string canvasName)
        {
            if (System.Enum.TryParse(canvasName, out TCanvasName canvasNameEnum) == false)
            {
                Debug.LogError($"{name} not found canvasInstance:{canvasName}", this);
                return;
            }

            Hide(canvasNameEnum);
        }

        public void Hide(TCanvasName canvasName)
        {
            CanvasWrapper canvasWrapper = GetCanvasWrapper(canvasName);
            ProcessHide(canvasName, canvasWrapper);
        }

        public void Hide(ICanvas canvas)
        {
            if (_instanceMatch.TryGetValue(canvas, out var canvasWrapper) == false)
            {
                Debug.LogError($"CanvasManager({name}) not found canvasInstance:{canvas.gameObject.name}", this);
                return;
            }

            ProcessHide(canvasWrapper.name, canvasWrapper.wrapper);
        }

        public void HideAll()
        {
            foreach (var canvasName in _canvasInstance.Keys)
                Hide(canvasName);
        }

        public bool IsShow(string canvasName)
        {
            if (System.Enum.TryParse(canvasName, out TCanvasName canvasNameEnum) == false)
            {
                Debug.LogError($"{name} not found canvasInstance:{canvasName}", this);
                return false;
            }

            CanvasWrapper canvasWrapper = GetCanvasWrapper(canvasNameEnum);
            return canvasWrapper.IsShow;
        }


        public bool IsShow(TCanvasName canvasName)
        {
            CanvasWrapper canvasWrapper = GetCanvasWrapper(canvasName);
            if (canvasWrapper == null)
            {
                return false;
            }

            return canvasWrapper.IsShow;
        }

        public ICanvas GetCanvas(TCanvasName canvasName) => GetCanvasWrapper(canvasName).CanvasInstance;

        public void DisposeCanvas(TCanvasName canvasName)
        {
            if (_canvasInstance.TryGetValue(canvasName, out var canvasWrapperList))
            {
                _canvasInstance.Remove(canvasName);

                canvasWrapperList.Foreach(canvas => canvas.Dispose());
                canvasWrapperList.Clear();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            ICanvas[] canvasArray = GetComponentsInChildren<ICanvas>(true);
            foreach (var canvas in canvasArray)
            {
                if (System.Enum.TryParse(canvas.name, out TCanvasName canvasName))
                    AddCanvasInstance(canvasName, canvas);
            }
            Init(false);

            IsReady = true;
        }

        protected virtual ICanvas OnRequireCanvasInstance(TCanvasName canvasName)
        {
            return null;
        }

        protected virtual void OnShowCanvas(TCanvasName canvasName, ICanvas canvas)
        {
        }

        protected virtual void OnHideCanvas(TCanvasName canvasName, ICanvas canvas)
        {
        }

        protected CanvasWrapper GetCanvasWrapper(TCanvasName canvasName, System.Func<CanvasWrapper, bool> OnMatch = null)
        {
            if (_canvasInstance.TryGetValue(canvasName, out var canvasWrapperList) == false)
                return null;

            if (OnMatch == null)
                return canvasWrapperList.FirstOrDefault();

            return canvasWrapperList.FirstOrDefault(canvas => OnMatch(canvas));
        }

        private CanvasWrapper OnCreateInstance(TCanvasName canvasName)
        {
            ICanvas canvasInstance = OnRequireCanvasInstance(canvasName);
            if (canvasInstance == null)
            {
                Debug.LogError($"CanvasManager({name}) GetCanvasInstance(canvasName:{canvasName}) canvasInstance == null", this);
                return null;
            }

            return AddCanvasInstance(canvasName, canvasInstance);
        }

        private void ProcessHide(TCanvasName canvasName, CanvasWrapper canvasWrapper)
        {
            canvasWrapper.SetStatus(CanvasStatus.HideAnimationPlaying);
            OnHideCanvas(canvasName, canvasWrapper.CanvasInstance);
            StartCoroutine(canvasWrapper.OnHideCanvasCoroutine, () =>
            {
                canvasWrapper.SetActive(false);
            });
        }

        void StartCoroutine(System.Func<IEnumerator> coroutine, System.Action OnFinish)
        {
            StartCoroutine(InvokeAfterCoroutine(coroutine, OnFinish));
        }

        IEnumerator InvokeAfterCoroutine(System.Func<IEnumerator> coroutine, System.Action OnFinish)
        {
            yield return StartCoroutine(coroutine());

            OnFinish?.Invoke();
        }
    }
}
