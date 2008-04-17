#region Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com>
/*
Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com> All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MSNPSharp
{
    internal abstract class XMLNodeEnumerator
    {
        private XmlNode xmlrootnode;
        private int[,] StateTable;
        private Type InputType;

        protected Dictionary<string, int> InputTable = new Dictionary<string, int>();
        protected int currentState = 0;
        protected int currentInput = 0;
        protected bool isleafNode = false;
        protected Stack<XmlNode> stack = new Stack<XmlNode>(0);

        protected XMLNodeEnumerator(XmlNode root, int[,] statetable, Type inputtype)
        {
            xmlrootnode = root;
            StateTable = statetable;
            InputType = inputtype;
        }

        /// <summary>
        /// Parse the xml document
        /// </summary>
        public void Parse()
        {
            if (xmlrootnode != null && xmlrootnode.HasChildNodes)
                printNode(xmlrootnode);
        }

        /// <summary>
        /// Method used to process the state machine
        /// </summary>
        /// <param name="state">Current state</param>
        /// <param name="input">Current input string</param>
        /// <param name="inputType">Current input type</param>
        /// <param name="depth">Depth of current node</param>
        protected abstract void ProcessHandler(int state, string input, int inputType, int depth);

        protected virtual void IniInputTable()
        {
            InputTable.Clear();
            string[] keys = Enum.GetNames(InputType);
            int[] values = (int[])Enum.GetValues(InputType);
            int length = 0;
            for (length = 0; length < keys.Length; length++)
            {
                InputTable.Add(keys[length], values[length]);
            }
        }

        /// <summary>
        /// Preorder enumerate the DOM tree and run the state machine
        /// </summary>
        /// <param name="startnode"></param>
        protected virtual void printNode(XmlNode startnode)
        {
            if (startnode.HasChildNodes)
            {
                int nextstate = 0;
                IniInputTable();
                XmlNode currentNode = startnode.FirstChild;
                stack.Push(startnode);
                while (currentNode != startnode)
                {
                    if (currentNode.HasChildNodes)
                    {
                        //Trace.Write(startnode.Name + " ");
                        stack.Push(currentNode);
                        try
                        {
                            if (InputTable.ContainsKey(currentNode.Name))
                                currentInput = (int)Enum.Parse(InputType, currentNode.Name, true);
                            else
                                currentInput = 0;
                        }
                        catch (Exception) { }

                        nextstate = (int)(StateTable[currentState, currentInput]);
                        isleafNode = false;
                        if (nextstate != 0)
                        {
                            currentState = nextstate;
                            ProcessHandler(currentState, currentNode.Name, currentInput, stack.Count - 1);

                        }
                        currentNode = currentNode.FirstChild;
                        continue;
                    }

                    //A leaf node
                    isleafNode = true;
                    if (nextstate != 0 && currentNode.InnerText != "")  //Just ignore the empty leaf.
                    {
                        ProcessHandler(currentState, currentNode.InnerText, currentInput, stack.Count - 1);
                    }

                    currentNode = MoveNext(currentNode);
                }
                isleafNode = false;
                ProcessHandler(-1, startnode.Name,
                    ((int[])Enum.GetValues(InputType))[((int[])Enum.GetValues(InputType)).Length - 1], 0);
            }
        }

        /// <summary>
        /// Move to next node or next subtree,or return to the start node
        /// </summary>
        /// <param name="curr"></param>
        /// <returns></returns>
        private XmlNode MoveNext(XmlNode curr)
        {
            if (stack.Count == 0)
                return curr;  //The start node.

            if (curr.NextSibling != null)
                return curr.NextSibling;
            return MoveNext(stack.Pop()); //The last node in the layer.
        }
    }
}
