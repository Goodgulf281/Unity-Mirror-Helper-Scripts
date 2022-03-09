// This file is part of the NodeListServer Example package.
using System;
using System.Collections.Generic;


/* Source:
 * 
 * https://github.com/SoftwareGuy/NodeListServer-Example
 * 
 * MIT License
 * Copyright (c) 2020 Matt Coburn
 * This software uses dependencies (Mirror and other libraries) 
 * licensed under other different licenses.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

namespace NodeListServer
{

    [Serializable]
    public class NodeListServerListResponse
    {
        // Number of known servers.
        public int count;
        // The container for the known servers.
        public List<NodeListServerListEntry> servers;
    }

    [Serializable]
    public class NodeListServerListEntry
    {
        // IP address. Beware: Might be IPv6 format, and require you to chop off the leading "::ffff:" part. YMMV.
        public string ip;
        // Name of the server.
        public string name;
        // Port of the server.
        public int port;
        // Number of players on the server.
        public int players;
        // The number of players maximum allowed on the server.
        public int capacity;
        // Extra data.
        public string extras;
    }

    [Serializable]
    public struct ServerInfo
    {
        public string Name;             // The name of the server.
        public int Port;                // The port of the server.
        public int PlayerCount;         // The count of players currently on the server.
        public int PlayerCapacity;      // The count of players allowed on the server.
        public string ExtraInformation; // Some extra information, probably best in JSON format for easy parsing.
    }
}