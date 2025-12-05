using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Selectable", 35)]
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class Selectable
        :
            UIBehaviour,
            IMoveHandler,
            IPointerDownHandler, IPointerUpHandler,
            IPointerEnterHandler, IPointerExitHandler,
            ISelectHandler, IDeselectHandler
    {
        protected static Selectable[] s_Selectables = new Selectable[10];
        protected static int s_SelectableCount = 0;
        private bool m_EnableCalled = false;


        ///


        public static Selectable[] allSelectablesArray
        {
            get
            {
                Selectable[] temp = new Selectable[s_SelectableCount];
                Array.Copy(s_Selectables, temp, s_SelectableCount);
                return temp;
            }
        }

        public static int allSelectableCount
        {
            get { return s_SelectableCount; }
        }


        ///
        ///
        ///
        ///
        public static int AllSelectablesNoAlloc(Selectable[] selectables)
        {
            int copyCount = selectables.Length < s_SelectableCount ? selectables.Length : s_SelectableCount;

            Array.Copy(s_Selectables, selectables, copyCount);

            return copyCount;
        }

        // Navigation information.
        [FormerlySerializedAs("navigation")] [SerializeField]
        private Navigation m_Navigation = Navigation.defaultNavigation;

        public enum Transition
        {
            None,

            ColorTint,

            SpriteSwap,

            Animation
        }

        // Type of the transition that occurs when the button state changes.
        [FormerlySerializedAs("transition")] [SerializeField]
        private Transition m_Transition = Transition.ColorTint;

        // Colors used for a color tint-based transition.
        [FormerlySerializedAs("colors")] [SerializeField]
        private ColorBlock m_Colors = ColorBlock.defaultColorBlock;

        // Sprites used for a Image swap-based transition.
        [FormerlySerializedAs("spriteState")] [SerializeField]
        private SpriteState m_SpriteState;

        [FormerlySerializedAs("animationTriggers")] [SerializeField]
        private AnimationTriggers m_AnimationTriggers = new AnimationTriggers();

        [Tooltip("Can the Selectable be interacted with?")] [SerializeField]
        private bool m_Interactable = true;

        // Graphic that will be colored.
        [FormerlySerializedAs("highlightGraphic")] [FormerlySerializedAs("m_HighlightGraphic")] [SerializeField]
        private Graphic m_TargetGraphic;


        private bool m_GroupsAllowInteraction = true;
        protected int m_CurrentIndex = -1;


        ///
        ///


        public Navigation navigation
        {
            get { return m_Navigation; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Navigation, value)) OnSetProperty();
            }
        }


        ///
        ///


        public Transition transition
        {
            get { return m_Transition; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Transition, value)) OnSetProperty();
            }
        }


        ///
        ///


        public ColorBlock colors
        {
            get { return m_Colors; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Colors, value)) OnSetProperty();
            }
        }


        ///
        ///
        ///


        public SpriteState spriteState
        {
            get { return m_SpriteState; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_SpriteState, value)) OnSetProperty();
            }
        }


        public AnimationTriggers animationTriggers
        {
            get { return m_AnimationTriggers; }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_AnimationTriggers, value)) OnSetProperty();
            }
        }


        ///
        ///


        public Graphic targetGraphic
        {
            get { return m_TargetGraphic; }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_TargetGraphic, value)) OnSetProperty();
            }
        }


        ///
        ///
        ///


        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Interactable, value))
                {
                    if (!m_Interactable && EventSystem.current != null &&
                        EventSystem.current.currentSelectedGameObject == gameObject)
                        EventSystem.current.SetSelectedGameObject(null);
                    OnSetProperty();
                }
            }
        }

        private bool isPointerInside { get; set; }
        private bool isPointerDown { get; set; }
        private bool hasSelection { get; set; }

        protected Selectable()
        {
        }

        public Image image
        {
            get { return m_TargetGraphic as Image; }
            set { m_TargetGraphic = value; }
        }


        ///
        ///


#if PACKAGE_ANIMATION
        public Animator animator
        {
            get { return GetComponent<Animator>(); }
        }
#endif

        protected override void Awake()
        {
            if (m_TargetGraphic == null)
                m_TargetGraphic = GetComponent<Graphic>();
        }

        private readonly List<CanvasGroup> m_CanvasGroupCache = new List<CanvasGroup>();

        protected override void OnCanvasGroupChanged()
        {
            var parentGroupAllowsInteraction = ParentGroupAllowsInteraction();

            if (parentGroupAllowsInteraction != m_GroupsAllowInteraction)
            {
                m_GroupsAllowInteraction = parentGroupAllowsInteraction;
                OnSetProperty();
            }
        }

        bool ParentGroupAllowsInteraction()
        {
            Transform t = transform;
            while (t != null)
            {
                t.GetComponents(m_CanvasGroupCache);
                for (var i = 0; i < m_CanvasGroupCache.Count; i++)
                {
                    if (m_CanvasGroupCache[i].enabled && !m_CanvasGroupCache[i].interactable)
                        return false;

                    if (m_CanvasGroupCache[i].ignoreParentGroups)
                        return true;
                }

                t = t.parent;
            }

            return true;
        }


        ///
        ///
        public virtual bool IsInteractable()
        {
            return m_GroupsAllowInteraction && m_Interactable;
        }

        // Call from unity if animation properties have changed
        protected override void OnDidApplyAnimationProperties()
        {
            OnSetProperty();
        }

        // Select on enable and add to the list.
        protected override void OnEnable()
        {
            //Check to avoid multiple OnEnable() calls for each selectable
            if (m_EnableCalled)
                return;

            base.OnEnable();

            if (s_SelectableCount == s_Selectables.Length)
            {
                Selectable[] temp = new Selectable[s_Selectables.Length * 2];
                Array.Copy(s_Selectables, temp, s_Selectables.Length);
                s_Selectables = temp;
            }

            if (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                hasSelection = true;
            }

            m_CurrentIndex = s_SelectableCount;
            s_Selectables[m_CurrentIndex] = this;
            s_SelectableCount++;
            isPointerDown = false;
            m_GroupsAllowInteraction = ParentGroupAllowsInteraction();
            DoStateTransition(currentSelectionState, true);

            m_EnableCalled = true;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            // If our parenting changes figure out if we are under a new CanvasGroup.
            OnCanvasGroupChanged();
        }

        private void OnSetProperty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DoStateTransition(currentSelectionState, true);
            else
#endif
                DoStateTransition(currentSelectionState, false);
        }

        // Remove from the list.
        protected override void OnDisable()
        {
            //Check to avoid multiple OnDisable() calls for each selectable
            if (!m_EnableCalled)
                return;

            s_SelectableCount--;

            // Update the last elements index to be this index
            s_Selectables[s_SelectableCount].m_CurrentIndex = m_CurrentIndex;

            // Swap the last element and this element
            s_Selectables[m_CurrentIndex] = s_Selectables[s_SelectableCount];

            // null out last element.
            s_Selectables[s_SelectableCount] = null;

            InstantClearState();
            base.OnDisable();

            m_EnableCalled = false;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsPressed())
            {
                InstantClearState();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Colors.fadeDuration = Mathf.Max(m_Colors.fadeDuration, 0.0f);

            // OnValidate can be called before OnEnable, this makes it unsafe to access other components
            // since they might not have been initialized yet.
            // OnSetProperty potentially access Animator or Graphics. (case 618186)
            if (isActiveAndEnabled)
            {
                if (!interactable && EventSystem.current != null &&
                    EventSystem.current.currentSelectedGameObject == gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
                // Need to clear out the override image on the target...
                DoSpriteSwap(null);

                // If the transition mode got changed, we need to clear all the transitions, since we don't know what the old transition mode was.
                StartColorTween(Color.white, true);
                TriggerAnimation(m_AnimationTriggers.normalTrigger);

                // And now go to the right state.
                DoStateTransition(currentSelectionState, true);
            }
        }

        protected override void Reset()
        {
            m_TargetGraphic = GetComponent<Graphic>();
        }

#endif // if UNITY_EDITOR

        protected SelectionState currentSelectionState
        {
            get
            {
                if (!IsInteractable())
                    return SelectionState.Disabled;
                if (isPointerDown)
                    return SelectionState.Pressed;
                if (hasSelection)
                    return SelectionState.Selected;
                if (isPointerInside)
                    return SelectionState.Highlighted;
                return SelectionState.Normal;
            }
        }

        protected virtual void InstantClearState()
        {
            string triggerName = m_AnimationTriggers.normalTrigger;

            isPointerInside = false;
            isPointerDown = false;
            hasSelection = false;

            switch (m_Transition)
            {
                case Transition.ColorTint:
                    StartColorTween(Color.white, true);
                    break;
                case Transition.SpriteSwap:
                    DoSpriteSwap(null);
                    break;
                case Transition.Animation:
                    TriggerAnimation(triggerName);
                    break;
            }
        }


        protected virtual void DoStateTransition(SelectionState state, bool instant)
        {
            if (!gameObject.activeInHierarchy)
                return;

            Color tintColor;
            Sprite transitionSprite;
            string triggerName;

            switch (state)
            {
                case SelectionState.Normal:
                    tintColor = m_Colors.normalColor;
                    transitionSprite = null;
                    triggerName = m_AnimationTriggers.normalTrigger;
                    break;
                case SelectionState.Highlighted:
                    tintColor = m_Colors.highlightedColor;
                    transitionSprite = m_SpriteState.highlightedSprite;
                    triggerName = m_AnimationTriggers.highlightedTrigger;
                    break;
                case SelectionState.Pressed:
                    tintColor = m_Colors.pressedColor;
                    transitionSprite = m_SpriteState.pressedSprite;
                    triggerName = m_AnimationTriggers.pressedTrigger;
                    break;
                case SelectionState.Selected:
                    tintColor = m_Colors.selectedColor;
                    transitionSprite = m_SpriteState.selectedSprite;
                    triggerName = m_AnimationTriggers.selectedTrigger;
                    break;
                case SelectionState.Disabled:
                    tintColor = m_Colors.disabledColor;
                    transitionSprite = m_SpriteState.disabledSprite;
                    triggerName = m_AnimationTriggers.disabledTrigger;
                    break;
                default:
                    tintColor = Color.black;
                    transitionSprite = null;
                    triggerName = string.Empty;
                    break;
            }

            switch (m_Transition)
            {
                case Transition.ColorTint:
                    StartColorTween(tintColor * m_Colors.colorMultiplier, instant);
                    break;
                case Transition.SpriteSwap:
                    DoSpriteSwap(transitionSprite);
                    break;
                case Transition.Animation:
                    TriggerAnimation(triggerName);
                    break;
            }
        }

        protected enum SelectionState
        {
            Normal,

            Highlighted,

            Pressed,

            Selected,

            Disabled,
        }

        // Selection logic


        ///
        ///
        ///
        public Selectable FindSelectable(Vector3 dir)
        {
            dir = dir.normalized;
            Vector3 localDir = Quaternion.Inverse(transform.rotation) * dir;
            Vector3 pos = transform.TransformPoint(GetPointOnRectEdge(transform as RectTransform, localDir));
            float maxScore = Mathf.NegativeInfinity;
            float maxFurthestScore = Mathf.NegativeInfinity;
            float score = 0;

            bool wantsWrapAround = navigation.wrapAround && (m_Navigation.mode == Navigation.Mode.Vertical ||
                                                             m_Navigation.mode == Navigation.Mode.Horizontal);

            Selectable bestPick = null;
            Selectable bestFurthestPick = null;

            for (int i = 0; i < s_SelectableCount; ++i)
            {
                Selectable sel = s_Selectables[i];

                if (sel == this)
                    continue;

                if (!sel.IsInteractable() || sel.navigation.mode == Navigation.Mode.None)
                    continue;

#if UNITY_EDITOR
                // Apart from runtime use, FindSelectable is used by custom editors to
                // draw arrows between different selectables. For scene view cameras,
                // only selectables in the same stage should be considered.
                if (Camera.current != null &&
                    !UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(sel.gameObject,
                        Camera.current))
                    continue;
#endif

                var selRect = sel.transform as RectTransform;
                Vector3 selCenter = selRect != null ? (Vector3)selRect.rect.center : Vector3.zero;
                Vector3 myVector = sel.transform.TransformPoint(selCenter) - pos;

                // Value that is the distance out along the direction.
                float dot = Vector3.Dot(dir, myVector);

                // If element is in wrong direction and we have wrapAround enabled check and cache it if furthest away.
                if (wantsWrapAround && dot < 0)
                {
                    score = -dot * myVector.sqrMagnitude;

                    if (score > maxFurthestScore)
                    {
                        maxFurthestScore = score;
                        bestFurthestPick = sel;
                    }

                    continue;
                }

                // Skip elements that are in the wrong direction or which have zero distance.
                // This also ensures that the scoring formula below will not have a division by zero error.
                if (dot <= 0)
                    continue;

                // This scoring function has two priorities:
                // - Score higher for positions that are closer.
                // - Score higher for positions that are located in the right direction.
                // This scoring function combines both of these criteria.
                // It can be seen as this:
                //   Dot (dir, myVector.normalized) / myVector.magnitude
                // The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
                // The second part scores lower the greater the distance is by dividing by the distance.
                // The formula below is equivalent but more optimized.
                //
                // If a given score is chosen, the positions that evaluate to that score will form a circle
                // that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
                // From the position pos, blow up a circular balloon so it grows in the direction of dir.
                // The first Selectable whose center the circular balloon touches is the one that's chosen.
                score = dot / myVector.sqrMagnitude;

                if (score > maxScore)
                {
                    maxScore = score;
                    bestPick = sel;
                }
            }

            if (wantsWrapAround && null == bestPick) return bestFurthestPick;

            return bestPick;
        }

        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
        {
            if (rect == null)
                return Vector3.zero;
            if (dir != Vector2.zero)
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
            return dir;
        }

        // Convenience function -- change the selection to the specified object if it's not null and happens to be active.
        void Navigate(AxisEventData eventData, Selectable sel)
        {
            if (sel != null && sel.IsActive())
                eventData.selectedObject = sel.gameObject;
        }


        ///
        ///
        public virtual Selectable FindSelectableOnLeft()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnLeft;
            }

            if ((m_Navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.left);
            }

            return null;
        }


        ///
        ///
        public virtual Selectable FindSelectableOnRight()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnRight;
            }

            if ((m_Navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.right);
            }

            return null;
        }


        ///
        ///
        public virtual Selectable FindSelectableOnUp()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnUp;
            }

            if ((m_Navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.up);
            }

            return null;
        }


        ///
        ///
        public virtual Selectable FindSelectableOnDown()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnDown;
            }

            if ((m_Navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.down);
            }

            return null;
        }


        ///
        ///
        public virtual void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Right:
                    Navigate(eventData, FindSelectableOnRight());
                    break;

                case MoveDirection.Up:
                    Navigate(eventData, FindSelectableOnUp());
                    break;

                case MoveDirection.Left:
                    Navigate(eventData, FindSelectableOnLeft());
                    break;

                case MoveDirection.Down:
                    Navigate(eventData, FindSelectableOnDown());
                    break;
            }
        }

        void StartColorTween(Color targetColor, bool instant)
        {
            if (m_TargetGraphic == null)
                return;

            m_TargetGraphic.CrossFadeColor(targetColor, instant ? 0f : m_Colors.fadeDuration, true, true);
        }

        void DoSpriteSwap(Sprite newSprite)
        {
            if (image == null)
                return;

            image.overrideSprite = newSprite;
        }

        void TriggerAnimation(string triggername)
        {
#if PACKAGE_ANIMATION
            if (transition != Transition.Animation || animator == null || !animator.isActiveAndEnabled ||
                !animator.hasBoundPlayables || string.IsNullOrEmpty(triggername))
                return;

            animator.ResetTrigger(m_AnimationTriggers.normalTrigger);
            animator.ResetTrigger(m_AnimationTriggers.highlightedTrigger);
            animator.ResetTrigger(m_AnimationTriggers.pressedTrigger);
            animator.ResetTrigger(m_AnimationTriggers.selectedTrigger);
            animator.ResetTrigger(m_AnimationTriggers.disabledTrigger);

            animator.SetTrigger(triggername);
#endif
        }


        ///
        ///
        ///
        protected bool IsHighlighted()
        {
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerInside && !isPointerDown && !hasSelection;
        }

        protected bool IsPressed()
        {
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerDown;
        }

        // Change the button to the correct state
        private void EvaluateAndTransitionToSelectionState()
        {
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(currentSelectionState, false);
        }


        ///
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Selection tracking
            if (IsInteractable() && navigation.mode != Navigation.Mode.None && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            isPointerDown = true;
            EvaluateAndTransitionToSelectionState();
        }


        ///
        ///
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isPointerDown = false;
            EvaluateAndTransitionToSelectionState();
        }


        ///
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            EvaluateAndTransitionToSelectionState();
        }


        ///
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            EvaluateAndTransitionToSelectionState();
        }


        ///
        public virtual void OnSelect(BaseEventData eventData)
        {
            hasSelection = true;
            EvaluateAndTransitionToSelectionState();
        }


        ///
        public virtual void OnDeselect(BaseEventData eventData)
        {
            hasSelection = false;
            EvaluateAndTransitionToSelectionState();
        }


        ///
        ///
        public virtual void Select()
        {
            if (EventSystem.current == null || EventSystem.current.alreadySelecting)
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}