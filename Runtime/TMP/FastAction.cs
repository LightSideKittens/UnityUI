using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace TMPro
{
    public class FastAction
    {
        private LinkedList<System.Action> delegates = new();

        private Dictionary<System.Action, LinkedListNode<System.Action>> lookup = new();

        public void Add(System.Action rhs)
        {
            if (lookup.ContainsKey(rhs)) return;

            lookup[rhs] = delegates.AddLast(rhs);
        }

        public void Remove(System.Action rhs)
        {
            if (lookup.TryGetValue(rhs, out var node))
            {
                lookup.Remove(rhs);
                delegates.Remove(node);
            }
        }

        public void Call()
        {
            var node = delegates.First;
            while (node != null)
            {
                node.Value();
                node = node.Next;
            }
        }
    }


    public class FastAction<A>
    {
        private LinkedList<System.Action<A>> delegates = new();

        private Dictionary<System.Action<A>, LinkedListNode<System.Action<A>>> lookup = new();

        public void Add(System.Action<A> rhs)
        {
            if (lookup.ContainsKey(rhs)) return;

            lookup[rhs] = delegates.AddLast(rhs);
        }

        public void Remove(System.Action<A> rhs)
        {
            if (lookup.TryGetValue(rhs, out var node))
            {
                lookup.Remove(rhs);
                delegates.Remove(node);
            }
        }

        public void Call(A a)
        {
            var node = delegates.First;
            while (node != null)
            {
                node.Value(a);
                node = node.Next;
            }
        }
    }


    public class FastAction<A, B>
    {
        private LinkedList<System.Action<A, B>> delegates = new();

        private Dictionary<System.Action<A, B>, LinkedListNode<System.Action<A, B>>> lookup = new();

        public void Add(System.Action<A, B> rhs)
        {
            if (lookup.ContainsKey(rhs)) return;

            lookup[rhs] = delegates.AddLast(rhs);
        }

        public void Remove(System.Action<A, B> rhs)
        {
            if (lookup.TryGetValue(rhs, out var node))
            {
                lookup.Remove(rhs);
                delegates.Remove(node);
            }
        }

        public void Call(A a, B b)
        {
            var node = delegates.First;
            while (node != null)
            {
                node.Value(a, b);
                node = node.Next;
            }
        }
    }


    public class FastAction<A, B, C>
    {
        private LinkedList<System.Action<A, B, C>> delegates = new();

        private Dictionary<System.Action<A, B, C>, LinkedListNode<System.Action<A, B, C>>> lookup = new();

        public void Add(System.Action<A, B, C> rhs)
        {
            if (lookup.ContainsKey(rhs)) return;

            lookup[rhs] = delegates.AddLast(rhs);
        }

        public void Remove(System.Action<A, B, C> rhs)
        {
            if (lookup.TryGetValue(rhs, out var node))
            {
                lookup.Remove(rhs);
                delegates.Remove(node);
            }
        }

        public void Call(A a, B b, C c)
        {
            var node = delegates.First;
            while (node != null)
            {
                node.Value(a, b, c);
                node = node.Next;
            }
        }
    }
}