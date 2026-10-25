using CatCode.PlayerLoops.Collections;
using NUnit.Framework;

namespace CatCode.PlayerLoops.Tests
{
    public class DeferredDenseArrayBaseTests
    {
        [Test]
        public void ApplyAdd_TwoBatches()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);

            Assert.AreEqual(0, arr.Count);
            Assert.AreEqual(2, arr.PendingCount);

            arr.ApplyAdd();

            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(0, arr.PendingCount);

            Assert.AreEqual(10, arr[h1]);
            Assert.AreEqual(20, arr[h2]);

            var h3 = arr.Add(30);
            var h4 = arr.Add(40);

            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(2, arr.PendingCount);

            arr.ApplyAdd();

            Assert.AreEqual(4, arr.Count);
            Assert.AreEqual(0, arr.PendingCount);

            Assert.AreEqual(10, arr[h1]);
            Assert.AreEqual(20, arr[h2]);
            Assert.AreEqual(30, arr[h3]);
            Assert.AreEqual(40, arr[h4]);
        }

        [Test]
        public void Remove_IgnoredByApplyAdd()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.Remove(h3);

            Assert.AreEqual(0, arr.Count);
            Assert.AreEqual(3, arr.PendingCount);

            arr.ApplyAdd();

            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(0, arr.PendingCount);

            Assert.AreEqual(10, arr[h1]);
            Assert.AreEqual(20, arr[h2]);
            Assert.AreEqual(30, arr[h3]);
        }

        [Test]
        public void Remove_PendingBeforeApplyRemove()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            arr.Remove(h1);

            arr.ApplyRemove();
            arr.ApplyAdd();
            arr.ApplyRemove();

            Assert.AreEqual(0, arr.Count);
            Assert.IsFalse(arr.IsValid(h1));
        }

        [Test]
        public void Remove_StaleHandle()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            arr.ApplyAdd();
            arr.Remove(h1);
            arr.ApplyRemove();

            var h2 = arr.Add(20);
            arr.ApplyAdd();

            arr.Remove(h1);
            arr.ApplyRemove();

            Assert.AreEqual(1, arr.Count);
            Assert.AreEqual(20, arr[h2]);
        }

        [Test]
        public void IsValid_DefaultHandle()
        {
            var arr = new DeferredDenseArray<int>(4);

            arr.Add(10);
            arr.ApplyAdd();

            Assert.IsFalse(arr.IsValid(default));
        }

        [Test]
        public void Add_ZeroCapacity()
        {
            var arr = new DeferredDenseArray<int>(0);

            var handles = new ElementHandle[10];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = arr.Add(i);
            arr.ApplyAdd();

            Assert.AreEqual(handles.Length, arr.Count);
            for (int i = 0; i < handles.Length; i++)
                Assert.AreEqual(i, arr[handles[i]]);
        }

        [Test]
        public void ApplyRemove_Count()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.ApplyAdd();

            arr.Remove(h1);
            Assert.AreEqual(3, arr.Count);
            arr.ApplyRemove();
            Assert.AreEqual(2, arr.Count);

            arr.Remove(h2);
            Assert.AreEqual(2, arr.Count);
            arr.ApplyRemove();
            Assert.AreEqual(1, arr.Count);

            arr.Remove(h3);
            Assert.AreEqual(1, arr.Count);
            arr.ApplyRemove();
            Assert.AreEqual(0, arr.Count);
        }

        [Test]
        public void ApplyRemove_First()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.ApplyAdd();

            arr.Remove(h1);
            arr.ApplyRemove();

            Assert.IsFalse(arr.IsValid(h1));
            Assert.IsTrue(arr.IsValid(h2));
            Assert.IsTrue(arr.IsValid(h3));

            Assert.AreEqual(20, arr[h2]);
            Assert.AreEqual(30, arr[h3]);

            Assert.AreEqual(20, arr[0]);
            Assert.AreEqual(30, arr[1]);
        }

        [Test]
        public void ApplyRemove_Middle()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.ApplyAdd();

            arr.Remove(h2);
            arr.ApplyRemove();

            Assert.AreEqual(2, arr.Count);

            Assert.IsTrue(arr.IsValid(h1));
            Assert.IsFalse(arr.IsValid(h2));
            Assert.IsTrue(arr.IsValid(h3));

            Assert.AreEqual(10, arr[0]);
            Assert.AreEqual(30, arr[1]);
        }

        [Test]
        public void ApplyRemove_Last()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.ApplyAdd();

            arr.Remove(h3);
            arr.ApplyRemove();

            Assert.AreEqual(2, arr.Count);

            Assert.IsTrue(arr.IsValid(h1));
            Assert.IsTrue(arr.IsValid(h2));
            Assert.IsFalse(arr.IsValid(h3));

            Assert.AreEqual(10, arr[0]);
            Assert.AreEqual(20, arr[1]);
        }

        [Test]
        public void ApplyRemove_All()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.ApplyAdd();

            arr.Remove(h1);
            arr.Remove(h2);
            arr.Remove(h3);

            arr.ApplyRemove();

            Assert.AreEqual(0, arr.Count);
            Assert.AreEqual(0, arr.PendingCount);

            Assert.IsFalse(arr.IsValid(h1));
            Assert.IsFalse(arr.IsValid(h2));
            Assert.IsFalse(arr.IsValid(h3));
        }

        [Test]
        public void Add_ReusesFreedSlot()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            arr.ApplyAdd();

            arr.Remove(h1);
            arr.ApplyRemove();

            var h3 = arr.Add(30);
            arr.ApplyAdd();

            Assert.AreEqual(2, arr.Count);

            Assert.IsFalse(arr.IsValid(h1));

            Assert.IsTrue(arr.IsValid(h2));
            Assert.AreEqual(20, arr[h2]);

            Assert.IsTrue(arr.IsValid(h3));
            Assert.AreEqual(30, arr[h3]);
        }
    }
}
