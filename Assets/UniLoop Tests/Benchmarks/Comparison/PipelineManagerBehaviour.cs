using System;
using UnityEngine;

namespace CatCode.PlayerLoops.Benchmarks
{
    /// <summary>
    /// Update manager with O(1) removal by id. A removed task is replaced by the last task in the array.
    /// </summary>
    public sealed class PipelineManagerBehaviour : MonoBehaviour
    {
        public const int Infinite = -1;

        private struct Task
        {
            public Action action;
            public int remaining;
            public int id;
        }

        private Task[] _tasks;
        private int _count;

        private int[] _indexById;
        private IdPool _ids;

        internal void Init(int capacity)
        {
            _tasks = new Task[capacity];
            _indexById = new int[capacity];
            _ids = new IdPool(capacity);
        }

        public int Add(Action action, int frames)
        {
            var id = _ids.Rent();

            _tasks[_count] = new Task { action = action, remaining = frames, id = id };
            _indexById[id] = _count;
            _count++;

            return id;
        }

        public void Remove(int id)
            => RemoveAt(_indexById[id]);

        private void RemoveAt(int index)
        {
            var removedId = _tasks[index].id;

            _count--;
            var last = _tasks[_count];
            _tasks[index] = last;
            _indexById[last.id] = index;
            _tasks[_count] = default;

            _ids.Return(removedId);
        }

        private void Update()
        {
            var i = 0;
            while (i < _count)
            {
                ref var task = ref _tasks[i];
                task.action();

                if (task.remaining != Infinite)
                {
                    task.remaining--;
                    if (task.remaining == 0)
                    {
                        // The last task moves to index i and has not run yet this frame, so i stays.
                        RemoveAt(i);
                        continue;
                    }
                }

                i++;
            }
        }
    }
}
