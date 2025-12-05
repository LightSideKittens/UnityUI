using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TMPro.EditorUtilities
{
    public class TMP_EditorCoroutine
    {
        readonly IEnumerator coroutine;


        TMP_EditorCoroutine(IEnumerator routine)
        {
            this.coroutine = routine;
        }


        public static TMP_EditorCoroutine StartCoroutine(IEnumerator routine)
        {
            TMP_EditorCoroutine coroutine = new TMP_EditorCoroutine(routine);
            coroutine.Start();

            return coroutine;
        }


        void Start()
        {
            EditorApplication.update += EditorUpdate;
        }


        public void Stop()
        {
            if (EditorApplication.update != null)
                EditorApplication.update -= EditorUpdate;
        }


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