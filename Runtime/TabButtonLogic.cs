using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UNKO.Utils;

namespace UNKO.ManageUGUI
{
    public class TabButtonLogic<TEnum>
        where TEnum : struct, Enum
    {
        Dictionary<TEnum, Button> _tabButton = new Dictionary<TEnum, Button>();
        List<System.Action<TEnum>> _onShowLogic = new List<Action<TEnum>>();
        List<System.Action<TEnum>> _onHideLogic = new List<Action<TEnum>>();

        List<System.Action<TEnum>> _onShowObject = new List<Action<TEnum>>();
        List<System.Action<TEnum>> _onHideObject = new List<Action<TEnum>>();

        public void ShowObject(TEnum key)
        {
            if (_tabButton.TryGetValue(key, out var gameObject) == false)
            {
                Debug.LogError($"ShowObject - _tabLogic.ContainsKey({key}) == false");
                return;
            }

            _tabButton.Keys
                .Where(item => item.Equals(key) == false)
                .Foreach(item => HideObject(item));

            _onShowLogic.Foreach(logic => logic(key));
            _onShowObject.Foreach(logic => logic(key));
        }


        public void HideObject(TEnum key)
        {
            _onHideLogic.Foreach(logic => logic(key));
        }

        public TabButtonLogic<TEnum> AddObject(TEnum key, Button button)
        {
            if (_tabButton.ContainsKey(key))
            {
                Debug.LogError($"AddObject - _tabLogic.ContainsKey({key})");
                return this;
            }
            button.onClick.AddListener(() => ShowObject(key));
            _tabButton.Add(key, button);
            return this;
        }

        public TabButtonLogic<TEnum> AddObjectRange(Dictionary<TEnum, Button> dictionary)
        {
            foreach (var item in dictionary)
                AddObject(item.Key, item.Value);
            return this;
        }

        public TabButtonLogic<TEnum> AddShowLogic(System.Action<TEnum> logic)
        {
            _onShowLogic.Add(logic);

            return this;
        }

        public TabButtonLogic<TEnum> AddHideLogic(System.Action<TEnum> logic)
        {
            _onHideLogic.Add(logic);

            return this;
        }

        public TabButtonLogic<TEnum> AddOnShow(System.Action<TEnum> onShow)
        {
            _onShowObject.Add(onShow);

            return this;
        }

        public TabButtonLogic<TEnum> AddOnHide(System.Action<TEnum> onHide)
        {
            _onHideObject.Add(onHide);

            return this;
        }
    }
}
