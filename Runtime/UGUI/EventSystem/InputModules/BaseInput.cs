using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    public class BaseInput : UIBehaviour
    {
        public virtual string compositionString
        {
            get { return Input.compositionString; }
        }

        public virtual IMECompositionMode imeCompositionMode
        {
            get { return Input.imeCompositionMode; }
            set { Input.imeCompositionMode = value; }
        }

        public virtual Vector2 compositionCursorPos
        {
            get { return Input.compositionCursorPos; }
            set { Input.compositionCursorPos = value; }
        }

        public virtual bool mousePresent
        {
            get { return Input.mousePresent; }
        }

        /// <param name="button"></param>
        /// <returns></returns>
        public virtual bool GetMouseButtonDown(int button)
        {
            return Input.GetMouseButtonDown(button);
        }

        public virtual bool GetMouseButtonUp(int button)
        {
            return Input.GetMouseButtonUp(button);
        }

        public virtual bool GetMouseButton(int button)
        {
            return Input.GetMouseButton(button);
        }

        public virtual Vector2 mousePosition
        {
            get { return Input.mousePosition; }
        }

        public virtual Vector2 mouseScrollDelta
        {
            get { return Input.mouseScrollDelta; }
        }

        public virtual float mouseScrollDeltaPerTick
        {
            get { return 1.0f; }
        }

        public virtual bool touchSupported
        {
            get { return Input.touchSupported; }
        }

        public virtual int touchCount
        {
            get { return Input.touchCount; }
        }

        /// <param name="index">Touch index to get</param>
        public virtual Touch GetTouch(int index)
        {
            return Input.GetTouch(index);
        }

        /// <param name="axisName">Axis name to check</param>
        public virtual float GetAxisRaw(string axisName)
        {
            return Input.GetAxisRaw(axisName);
        }

        /// <param name="buttonName">Button name to get</param>
        public virtual bool GetButtonDown(string buttonName)
        {
            return Input.GetButtonDown(buttonName);
        }
    }
}
