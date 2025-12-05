using UnityEngine;
using System.Collections;

namespace UnityEngine.UI
{
    ///
    ///
    public interface ILayoutElement
    {
        void CalculateLayoutInputHorizontal();

        void CalculateLayoutInputVertical();

        float minWidth { get; }


        float preferredWidth { get; }


        ///
        ///
        ///


        float flexibleWidth { get; }

        float minHeight { get; }


        float preferredHeight { get; }


        ///
        ///
        ///


        float flexibleHeight { get; }


        int layoutPriority { get; }
    }


    ///
    ///
    ///
    public interface ILayoutController
    {
        void SetLayoutHorizontal();

        void SetLayoutVertical();
    }


    public interface ILayoutGroup : ILayoutController
    {
    }


    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public interface ILayoutSelfController : ILayoutController
    {
    }


    public interface ILayoutIgnorer
    {
        bool ignoreLayout { get; }
    }
}