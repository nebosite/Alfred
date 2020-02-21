using System;
using Alfred;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlfredTests
{
    [TestClass]
    public class TestDocumentSection
    {
        [TestMethod]
        public void ParseCanHandleSingleLineItem()
        {
            string text = @"This is an item  ";

            var results = DocumentSection.Parse(text);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(0, results[0].IndentSize);
            Assert.AreEqual("This is an item", results[0].Contents);
        }

        [TestMethod]
        public void ParseCanHandleMultipleItems()
        {
            string text = @"    hey 

            
    hey2     
       child1

       child2
        
  Last hey   ";

            var results = DocumentSection.Parse(text);
            //Assert.AreEqual(6, results.Length);

            var itemIndex = 0;
            var item = results[itemIndex++];
            Assert.AreEqual(0, item.Children.Count);
            Assert.AreEqual(4, item.IndentSize);
            Assert.AreEqual("hey", item.Contents);

            item = results[itemIndex++];
            Assert.AreEqual(0, item.Children.Count);
            Assert.AreEqual(0, item.IndentSize);
            Assert.AreEqual("", item.Contents);

            item = results[itemIndex++];
            Assert.AreEqual(0, item.Children.Count);
            Assert.AreEqual(0, item.IndentSize);
            Assert.AreEqual("", item.Contents);

            item = results[itemIndex++];
            Assert.AreEqual(3, item.Children.Count);
            Assert.AreEqual(4, item.IndentSize);
            Assert.AreEqual("hey2", item.Contents);
            foreach(var child in item.Children)
            {
                Assert.AreEqual(0, child.Children.Count);
                Assert.AreEqual(7, child.IndentSize);
            }
            Assert.AreEqual("child1", item.Children[0].Contents);
            Assert.AreEqual("", item.Children[1].Contents);
            Assert.AreEqual("child2", item.Children[2].Contents);

            item = results[itemIndex++];
            Assert.AreEqual(0, item.Children.Count);
            Assert.AreEqual(0, item.IndentSize);
            Assert.AreEqual("", item.Contents);

            item = results[itemIndex++];
            Assert.AreEqual(0, item.Children.Count);
            Assert.AreEqual(2, item.IndentSize);
            Assert.AreEqual("Last hey", item.Contents);

        }


    }
}
