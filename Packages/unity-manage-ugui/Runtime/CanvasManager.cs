using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UNKO.Utils;

namespace UNKO.ManageUGUI
{
    public interface ICanvasManager
    {
        string name { get; }
        Transform transform { get; }
        GameObject gameObject { get; }

        bool IsShow(string canvasName);
        ICanvas Show(string canvasName);
        void Hide(string canvasName);
        void Hide(ICanvas canvas);
    }

    public class CanvasManager<TDerived, TCanvasName> : SingletonComponentBase<TDerived>, ICanvasManager
        where TDerived : CanvasManager<TDerived, TCanvasName>
        where TCanvasName : struct, System.Enum
    {
        private Dictionary<TCanvasName, List<CanvasWrapper>> _canvasInstance = new Dictionary<TCanvasName, List<CanvasWrapper>>();
        private Dictionary<ICanvas, CanvasWrapper> _instanceMatch = new Dictionary<ICanvas, CanvasWrapper>();

        private SimplePool<CanvasWrapper> _canvasWrapperPool = new SimplePool<CanvasWrapper>(new CanvasWrapper(), 10);
        public bool isReady { get; private set; }

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
                return wrapper;
            }

            if (_canvasInstance.TryGetValue(canvasName, out var list) == false)
            {
                list = new List<CanvasWrapper>();
                _canvasInstance.Add(canvasName, list);
            }

            wrapper = _canvasWrapperPool.Spawn().Init(canvasInstance);
            _instanceMatch.Add(canvasInstance, wrapper);
            list.Add(wrapper);

            canvasInstance.canvasManager = this;
            wrapper.Awake(this);

            return wrapper;
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
            CanvasWrapper canvasWrapper = GetCanvasWrapper(canvasName, canvas => canvas.isShow == false);
            if (canvasWrapper == null)
                canvasWrapper = OnCreateInstance(canvasName);

            if (canvasWrapper != null)
            {
                canvasWrapper.SetActive(true);
                StartCoroutine(canvasWrapper.OnShowCanvasCoroutine());
            }
            OnShowCanvas(canvasName, canvasWrapper.canvasInstance);
            return canvasWrapper.canvasInstance;
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
            ProcessHide(canvasWrapper);
        }

        public void Hide(ICanvas canvas)
        {
            if (_instanceMatch.TryGetValue(canvas, out var canvasWrapper) == false)
            {
                Debug.LogError($"CanvasManager({name}) not found canvasInstance:{canvas.gameObject.name}", this);
                return;
            }

            ProcessHide(canvasWrapper);
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
            return canvasWrapper.isShow;
        }


        public bool IsShow(TCanvasName canvasName)
        {
            CanvasWrapper canvasWrapper = GetCanvasWrapper(canvasName);
            return canvasWrapper.isShow;
        }

        public ICanvas GetCanvas(TCanvasName canvasName) => GetCanvasWrapper(canvasName).canvasInstance;

        void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            ICanvas[] canvasArray = GetComponentsInChildren<ICanvas>(true);
            foreach (var canvas in canvasArray)
            {
                if (System.Enum.TryParse(canvas.name, out TCanvasName canvasName))
                    AddCanvasInstance(canvasName, canvas);
            }
            Init(false);

            isReady = true;
        }

        protected virtual ICanvas OnRequireCanvasInstance(TCanvasName canvasName)
        {
            return null;
        }

        protected virtual void OnShowCanvas(TCanvasName canvasName, ICanvas canvas)
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

        private void ProcessHide(CanvasWrapper canvasWrapper)
        {
            canvasWrapper.SetStatus(CanvasStatus.HideAnimationPlaying);
            StartCoroutine(canvasWrapper.OnHideCanvasCoroutine, () =>
            {
                canvasWrapper.SetActive(false);
            });
        }

        void StartCoroutine(System.Func<IEnumerator> coroutine, System.Action OnFinish)
        {
            StartCoroutine(_Coroutine(coroutine, OnFinish));
        }

        IEnumerator _Coroutine(System.Func<IEnumerator> coroutine, System.Action OnFinish)
        {
            yield return StartCoroutine(coroutine());

            OnFinish?.Invoke();
        }
    }
}
