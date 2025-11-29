using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TMPro.EditorUtilities
{
    /// <summary>
    /// Simple implementation of coroutine working in the Unity Editor.
    /// </summary>
    public class TMP_EditorCoroutine
    {
        readonly IEnumerator coroutine;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="routine"></param>
        TMP_EditorCoroutine(IEnumerator routine)
        {
            this.coroutine = routine;
        }


        /// <summary>
        /// Starts a new EditorCoroutine.
        /// </summary>
        /// <param name="newCoroutine">Coroutine</param>
        /// <returns>new EditorCoroutine</returns>
        public static TMP_EditorCoroutine StartCoroutine(IEnumerator routine)
        {
            TMP_EditorCoroutine coroutine = new TMP_EditorCoroutine(routine);
            coroutine.Start();

            return coroutine;
        }


        /// <summary>
        /// Clear delegate list 
        /// </summary>


        /// <summary>
        /// Register callback for editor updates
        /// </summary>
        void Start()
        {
            EditorApplication.update += EditorUpdate;
        }


        /// <summary>
        /// Unregister callback for editor updates.
        /// </summary>
        public void Stop()
        {
            if (EditorApplication.update != null)
                EditorApplication.update -= EditorUpdate;
        }
 

        /// <summary>
        /// Delegate function called on editor updates.
        /// </summary>
        void EditorUpdate()
        {
            if (coroutine.MoveNext() == false)
                Stop();

            if (coroutine.Current != null)
            {

            }
        }
    }
}