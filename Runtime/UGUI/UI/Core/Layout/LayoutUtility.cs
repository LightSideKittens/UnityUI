using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Utility functions for querying layout elements for their minimum, preferred, and flexible sizes.
    /// </summary>
    public static class LayoutUtility
    {
        /// <summary>
        /// Returns the minimum size of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        /// <remarks>All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.</remarks>
        public static float GetMinSize(RectTransform rect, int axis)
        {
            return axis == 0 ? GetMinWidth(rect) : GetMinHeight(rect);
        }

        /// <summary>
        /// Returns the preferred size of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetPreferredSize(RectTransform rect, int axis)
        {
            return axis == 0 ? GetPreferredWidth(rect) : GetPreferredHeight(rect);
        }

        /// <summary>
        /// Returns the flexible size of the layout element.
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        public static float GetFlexibleSize(RectTransform rect, int axis)
        {
            return axis == 0 ? GetFlexibleWidth(rect) : GetFlexibleHeight(rect);
        }

        /// <summary>
        /// Returns the minimum width of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetMinWidth(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.minWidth, 0);
        }

        /// <summary>
        /// Returns the preferred width of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <returns>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </returns>
        public static float GetPreferredWidth(RectTransform rect)
        {
            return GetMaxLayoutProperty(rect, e => e.minWidth, e => e.preferredWidth);
        }

        /// <summary>
        /// Returns the flexible width of the layout element.
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        public static float GetFlexibleWidth(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.flexibleWidth, 0);
        }

        /// <summary>
        /// Returns the minimum height of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetMinHeight(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.minHeight, 0);
        }

        /// <summary>
        /// Returns the preferred height of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetPreferredHeight(RectTransform rect)
        {
            return GetMaxLayoutProperty(rect, e => e.minHeight, e => e.preferredHeight);
        }

        /// <summary>
        /// Returns the flexible height of the layout element.
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        public static float GetFlexibleHeight(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.flexibleHeight, 0);
        }

        /// <summary>
        /// Gets a calculated layout property for the layout element with the given RectTransform.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to get a property for.</param>
        /// <param name="property">The property to calculate.</param>
        /// <param name="defaultValue">The default value to use if no component on the layout element supplies the given property</param>
        /// <returns>The calculated value of the layout property.</returns>
        public static float GetLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> property, float defaultValue)
        {
            ILayoutElement dummy;
            return GetLayoutProperty(rect, property, defaultValue, out dummy);
        }

        /// <summary>
        /// Gets a calculated layout property for the layout element with the given RectTransform.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to get a property for.</param>
        /// <param name="property">The property to calculate.</param>
        /// <param name="defaultValue">The default value to use if no component on the layout element supplies the given property</param>
        /// <param name="source">Optional out parameter to get the component that supplied the calculated value.</param>
        /// <returns>The calculated value of the layout property.</returns>
        public static float GetLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> property, float defaultValue, out ILayoutElement source)
        {
            source = null;
            if (rect == null)
                return 0;
            float min = defaultValue;
            int maxPriority = System.Int32.MinValue;
            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutElement), components);

            var componentsCount = components.Count;
            for (int i = 0; i < componentsCount; i++)
            {
                var layoutComp = components[i] as ILayoutElement;
                if (layoutComp is Behaviour && !((Behaviour)layoutComp).isActiveAndEnabled)
                    continue;

                int priority = layoutComp.layoutPriority;
                // If this layout components has lower priority than a previously used, ignore it.
                if (priority < maxPriority)
                    continue;
                float prop = property(layoutComp);
                // If this layout property is set to a negative value, it means it should be ignored.
                if (prop < 0)
                    continue;

                // If this layout component has higher priority than all previous ones,
                // overwrite with this one's value.
                if (priority > maxPriority)
                {
                    min = prop;
                    maxPriority = priority;
                    source = layoutComp;
                }
                // If the layout component has the same priority as a previously used,
                // use the largest of the values with the same priority.
                else if (prop > min)
                {
                    min = prop;
                    source = layoutComp;
                }
            }

            ListPool<Component>.Release(components);
            return min;
        }

        /// <summary>
        /// Resolves two layout properties (e.g. min and preferred for one axis) over a single GetComponents query
        /// and returns the larger result. Equivalent to <c>Mathf.Max(GetLayoutProperty(a), GetLayoutProperty(b))</c>
        /// — same per-property priority resolution — but halves the component lookups for preferred-size queries,
        /// which dominate layout-group child measurement.
        /// </summary>
        static float GetMaxLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> propertyA, System.Func<ILayoutElement, float> propertyB)
        {
            if (rect == null)
                return 0;

            float a = 0f;
            float b = 0f;
            int aPriority = System.Int32.MinValue;
            int bPriority = System.Int32.MinValue;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutElement), components);

            var componentsCount = components.Count;
            for (int i = 0; i < componentsCount; i++)
            {
                var layoutComp = components[i] as ILayoutElement;
                if (layoutComp is Behaviour && !((Behaviour)layoutComp).isActiveAndEnabled)
                    continue;

                int priority = layoutComp.layoutPriority;

                if (priority >= aPriority)
                {
                    float prop = propertyA(layoutComp);
                    if (prop >= 0)
                    {
                        if (priority > aPriority) { a = prop; aPriority = priority; }
                        else if (prop > a) { a = prop; }
                    }
                }

                if (priority >= bPriority)
                {
                    float prop = propertyB(layoutComp);
                    if (prop >= 0)
                    {
                        if (priority > bPriority) { b = prop; bPriority = priority; }
                        else if (prop > b) { b = prop; }
                    }
                }
            }

            ListPool<Component>.Release(components);
            return Mathf.Max(a, b);
        }
    }
}
