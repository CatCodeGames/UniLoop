using CatCode.PlayerLoops.Collections;
using NUnit.Framework;

namespace CatCode.PlayerLoops.Tests
{
    public class DeferredDenseArrayBaseTests
    {
        [Test]
        public void Add_ApplyAdd()
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
        }

        [Test]
        public void Add_ApplyAdd_Add_ApplyAdd()
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
        public void Remove_NoApplyAdd()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.Remove(h3);

            Assert.AreEqual(0, arr.Count);
            Assert.AreEqual(3, arr.PendingCount);
        }

        [Test]
        public void Remove_BeforeApplyAdd()
        {
            var arr = new DeferredDenseArray<int>(4);

            var h1 = arr.Add(10);
            var h2 = arr.Add(20);
            var h3 = arr.Add(30);

            arr.Remove(h3);

            arr.ApplyAdd();

            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(0, arr.PendingCount);

            Assert.AreEqual(10, arr[h1]);
            Assert.AreEqual(20, arr[h2]);
            Assert.AreEqual(30, arr[h3]);
        }


        [Test]
        public void Remove_CorrectCount()
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
        public void Remove_First_CorrectElements()
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
        public void Remove_Middle_CorrectElements()
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
        public void Remove_Last_CorrectElements()
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
        public void Remove_AllEments()
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
        public void Add_Remove_Add()
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

            Assert.IsTrue(arr.IsValid(h2));
            Assert.AreEqual(20, arr[h2]);

            Assert.IsTrue(arr.IsValid(h3));
            Assert.AreEqual(30, arr[h3]);
        }
    }
}