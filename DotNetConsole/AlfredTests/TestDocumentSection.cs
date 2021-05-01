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

        [TestMethod]
        public void ParseCanHandleComments()
        {
            string text = @"IGNORE+ Junk after this is a comment
    lalala  
IGNORE-  Junk after this is still a comment
    An Item
## A comment
== Another comment
        ## Not a comment
        IGNORE+ Not a comment
IGNORE+
this is acomment
IGNORE-

-------- Alfred Settings --------------------------------------------------
--Alfred:CommentTag=##
--Alfred:CommentTag===
--Alfred:IgnoreSectionStartTag=IGNORE+
--Alfred:IgnoreSectionEndTag=IGNORE-
";

            var results = DocumentSection.Parse(text);
            //Assert.AreEqual(15, results.Length);

            Assert.AreEqual(true, results[0].IsComment);
            Assert.AreEqual(true, results[1].IsComment);
            Assert.AreEqual(true, results[2].IsComment);
            Assert.AreEqual(true, results[3].IsComment);
            Assert.AreEqual(true, results[4].IsComment);
            Assert.AreEqual("IGNORE+ Junk after this is a comment", results[0].Contents);
            Assert.AreEqual("    lalala  ", results[1].Contents);
            Assert.AreEqual("IGNORE-  Junk after this is still a comment", results[2].Contents);
            Assert.AreEqual("## A comment", results[3].Contents);
            Assert.AreEqual("== Another comment", results[4].Contents);

            var item = results[5];
            Assert.AreEqual(false, item.IsComment);
            Assert.AreEqual(4, item.IndentSize);
            Assert.AreEqual("An Item", item.Contents);
            Assert.AreEqual(2, item.Children.Count);
            Assert.AreEqual("## Not a comment", item.Children[0].Contents);
            Assert.AreEqual("IGNORE+ Not a comment", item.Children[1].Contents);
            Assert.AreEqual(false, item.Children[0].IsComment);
            Assert.AreEqual(false, item.Children[1].IsComment);

            Assert.AreEqual(true, results[6].IsComment);
            Assert.AreEqual(true, results[7].IsComment);
            Assert.AreEqual(true, results[8].IsComment);
            Assert.AreEqual("IGNORE+", results[6].Contents);
            Assert.AreEqual("this is acomment", results[7].Contents);
            Assert.AreEqual("IGNORE-", results[8].Contents);

            item = results[9];
            Assert.AreEqual(false, item.IsComment);
            Assert.AreEqual(0, item.IndentSize);
            Assert.AreEqual("", item.Contents);

            Assert.AreEqual(true, results[10].IsComment);
            Assert.AreEqual(true, results[11].IsComment);
            Assert.AreEqual(true, results[12].IsComment);
            Assert.AreEqual(true, results[13].IsComment);
            Assert.AreEqual(true, results[14].IsComment);
            Assert.AreEqual("-------- Alfred Settings --------------------------------------------------", results[10].Contents);
            Assert.AreEqual("--Alfred:CommentTag=##", results[11].Contents);
            Assert.AreEqual("--Alfred:CommentTag===", results[12].Contents);
            Assert.AreEqual("--Alfred:IgnoreSectionStartTag=IGNORE+", results[13].Contents);
            Assert.AreEqual("--Alfred:IgnoreSectionEndTag=IGNORE-", results[14].Contents);

        }
    }
}
