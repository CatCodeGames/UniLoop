using CatCode.PlayerLoops.Collections;
using NUnit.Framework;
using System.Collections.Generic;

namespace CatCode.PlayerLoops.Tests
{
    public class DeferredDenseArrayStressTests
    {
        [Test]
        public void Stress_RandomRemove()
        {
            const int ElementsCount = 100;
            const int RemoveCount = 10;

            UnityEngine.Random.InitState(0);

            var arr = new DeferredDenseArray<int>(8);

            var items = new List<(int Value, ElementHandle Handle)>();
            var order = new List<int>();

            for (int i = 0; i < ElementsCount; i++)
            {
                int value = i + 1;

                items.Add((value, arr.Add(value)));
                order.Add(i);
            }

            arr.ApplyChanges();

            Shuffle();

            for (int iter = 0; iter < 5; iter++)
            {
                RemoveBatch();
                Check();
            }

            void RemoveBatch()
            {
                int start = order.Count - RemoveCount;

                for (int i = start; i < order.Count; i++)
                    arr.Remove(items[order[i]].Handle);

                arr.ApplyRemove();
                order.RemoveRange(start, RemoveCount);
            }

            void Shuffle()
            {
                for (int i = 0; i < order.Count; i++)
                {
                    int j = UnityEngine.Random.Range(i, order.Count);
                    (order[i], order[j]) = (order[j], order[i]);
                }
            }

            void Check()
            {
                Assert.AreEqual(order.Count, arr.Count);

                foreach (var index in order)
                {
                    var item = items[index];
                    Assert.IsTrue(arr.IsValid(item.Handle));
                    Assert.AreEqual(item.Value, arr[item.Handle]);
                }
            }
        }


        [Test]
        public void Stress_AddRemoveCycles()
        {
            UnityEngine.Random.InitState(0);

            var arr = new DeferredDenseArray<int>(16);

            var items = new List<(int Value, ElementHandle Handle)>();
            var order = new List<int>();

            AddBatch(10);
            Check();

            for (int i = 1; i <= 5; i += 1)
            {
                var batchSize = 10 * i;

                AddBatch(batchSize);
                Check();

                RemoveRandomBatch(batchSize);
                Check();
            }

            void AddBatch(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int value = items.Count + 1;

                    items.Add((value, arr.Add(value)));
                    order.Add(items.Count - 1);
                }

                arr.ApplyAdd();
            }

            void RemoveRandomBatch(int count)
            {
                Shuffle();

                int first = order.Count - count;

                for (int i = first; i < order.Count; i++)
                    arr.Remove(items[order[i]].Handle);

                arr.ApplyRemove();
                order.RemoveRange(first, count);
            }

            void Shuffle()
            {
                for (int i = 0; i < order.Count; i++)
                {
                    int j = UnityEngine.Random.Range(i, order.Count);
                    (order[i], order[j]) = (order[j], order[i]);
                }
            }

            void Check()
            {
                Assert.AreEqual(order.Count, arr.Count);

                foreach (int index in order)
                {
                    var item = items[index];

                    Assert.IsTrue(arr.IsValid(item.Handle));
                    Assert.AreEqual(item.Value, arr[item.Handle]);
                }
            }
        }
    }
}