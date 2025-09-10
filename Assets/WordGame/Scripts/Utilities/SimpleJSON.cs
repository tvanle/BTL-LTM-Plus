//#define USE_SharpZipLib
#if !UNITY_WEBPLAYER
#define USE_FileIO
#endif

/* * * * *
 * A simple JSON Parser / builder
 * ------------------------------
 * 
 * It mainly has been written as a simple JSON parser. It can build a JSON string
 * from the node-tree, or generate a node tree from any valid JSON string.
 * 
 * If you want to use compression when saving to file / stream / B64 you have to include
 * SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ ) in your project and
 * define "USE_SharpZipLib" at the top of the file
 * 
 * Written by Bunny83 
 * 2012-06-09
 * 
 * Features / attributes:
 * - provides strongly typed node classes and lists / dictionaries
 * - provides easy access to class members / array items / data values
 * - the parser ignores data types. Each value is a string.
 * - only double quotes (") are used for quoting strings.
 * - values and names are not restricted to quoted strings. They simply add up and are trimmed.
 * - There are only 3 types: arrays(JSONArray), objects(JSONClass) and values(JSONData)
 * - provides "casting" properties to easily convert to / from those types:
 *   int / float / double / bool
 * - provides a common interface for each node so no explicit casting is required.
 * - the parser try to avoid errors, but if malformed JSON is parsed the result is undefined
 * 
 * 
 * 2012-12-17 Update:
 * - Added internal JSONLazyCreator class which simplifies the construction of a JSON tree
 *   Now you can simple reference any item that doesn't exist yet and it will return a JSONLazyCreator
 *   The class determines the required type by it's further use, creates the type and removes itself.
 * - Added binary serialization / deserialization.
 * - Added support for BZip2 zipped binary format. Requires the SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ )
 *   The usage of the SharpZipLib library can be disabled by removing or commenting out the USE_SharpZipLib define at the top
 * - The serializer uses different types when it comes to store the values. Since my data values
 *   are all of type string, the serializer will "try" which format fits best. The order is: int, float, double, bool, string.
 *   It's not the most efficient way but for a moderate amount of data it should work on all platforms.
 * 
 * * * * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
 
 
namespace SimpleJSON
{
    public enum JSONBinaryTag
    {
        Array            = 1,
        Class            = 2,
        Value            = 3,
        IntValue        = 4,
        DoubleValue        = 5,
        BoolValue        = 6,
        FloatValue        = 7,
    }
 
    public class JSONNode
    {
        #region common interface
        public virtual void Add(string aKey, JSONNode aItem){ }
        public virtual JSONNode this[int aIndex]   { get { return null; } set { } }
        public virtual JSONNode this[string aKey]  { get { return null; } set { } }
        public virtual string Value                { get { return "";   } set { } }
        public virtual int Count                   { get { return 0;    } }
 
        public virtual void Add(JSONNode aItem)
        {
            this.Add("", aItem);
        }
 
        public virtual JSONNode Remove(string aKey) { return null; }
        public virtual JSONNode Remove(int aIndex) { return null; }
        public virtual JSONNode Remove(JSONNode aNode) { return aNode; }
 
        public virtual IEnumerable<JSONNode> Childs { get { yield break;} }
        public IEnumerable<JSONNode> DeepChilds
        {
            get
            {
                foreach (var C in this.Childs)
                    foreach (var D in C.DeepChilds)
                        yield return D;
            }
        }
 
        public override string ToString()
        {
            return "JSONNode";
        }
        public virtual string ToString(string aPrefix)
        {
            return "JSONNode";
        }
 
        #endregion common interface
 
        #region typecasting properties
        public virtual int AsInt
        {
            get
            {
                int v = 0;
                if (int.TryParse(this.Value,out v))
                    return v;
                return 0;
            }
            set
            {
                this.Value = value.ToString();
            }
        }
        public virtual float AsFloat
        {
            get
            {
                float v = 0.0f;
                if (float.TryParse(this.Value,out v))
                    return v;
                return 0.0f;
            }
            set
            {
                this.Value = value.ToString();
            }
        }
        public virtual double AsDouble
        {
            get
            {
                double v = 0.0;
                if (double.TryParse(this.Value,out v))
                    return v;
                return 0.0;
            }
            set
            {
                this.Value = value.ToString();
            }
        }
        public virtual bool AsBool
        {
            get
            {
                bool v = false;
                if (bool.TryParse(this.Value,out v))
                    return v;
                return !string.IsNullOrEmpty(this.Value);
            }
            set
            {
                this.Value = (value)?"true":"false";
            }
        }
        public virtual JSONArray AsArray
        {
            get
            {
                return this as JSONArray;
            }
        }
        public virtual JSONClass AsObject
        {
            get
            {
                return this as JSONClass;
            }
        }
 
 
        #endregion typecasting properties
 
        #region operators
        public static implicit operator JSONNode(string s)
        {
            return new JSONData(s);
        }
        public static implicit operator string(JSONNode d)
        {
            return (d == null)?null:d.Value;
        }
        public static bool operator ==(JSONNode a, object b)
        {
            if (b == null && a is JSONLazyCreator)
                return true;
            return System.Object.ReferenceEquals(a,b);
        }
 
        public static bool operator !=(JSONNode a, object b)
        {
            return !(a == b);
        }
        public override bool Equals (object obj)
        {
            return System.Object.ReferenceEquals(this, obj);
        }
        public override int GetHashCode ()
        {
            return base.GetHashCode();
        }
 
 
        #endregion operators
 
        internal static string Escape(string aText)
        {
            string result = "";
            foreach(char c in aText)
            {
                switch(c)
                {
                    case '\\' : result += "\\\\"; break;
                    case '\"' : result += "\\\""; break;
                    case '\n' : result += "\\n" ; break;
                    case '\r' : result += "\\r" ; break;
                    case '\t' : result += "\\t" ; break;
                    case '\b' : result += "\\b" ; break;
                    case '\f' : result += "\\f" ; break;
                    default   : result += c     ; break;
                }
            }
            return result;
        }
 
        public static JSONNode Parse(string aJSON)
        {
            Stack<JSONNode> stack = new Stack<JSONNode>();
            JSONNode ctx = null;
            int i = 0;
            string Token = "";
            string TokenName = "";
            bool QuoteMode = false;
            while (i < aJSON.Length)
            {
                switch (aJSON[i])
                {
                    case '{':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        stack.Push(new JSONClass());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName,stack.Peek());
                        }
                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                    break;
 
                    case '[':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
 
                        stack.Push(new JSONArray());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName,stack.Peek());
                        }
                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                    break;
 
                    case '}':
                    case ']':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");
 
                        stack.Pop();
                        if (Token != "")
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(Token);
                            else if (TokenName != "")
                                ctx.Add(TokenName,Token);
                        }
                        TokenName = "";
                        Token = "";
                        if (stack.Count>0)
                            ctx = stack.Peek();
                    break;
 
                    case ':':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        TokenName = Token;
                        Token = "";
                    break;
 
                    case '"':
                        QuoteMode ^= true;
                    break;
 
                    case ',':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        if (Token != "")
                        {
                            if (ctx is JSONArray)
                                ctx.Add(Token);
                            else if (TokenName != "")
                                ctx.Add(TokenName, Token);
                        }
                        TokenName = "";
                        Token = "";
                    break;
 
                    case '\r':
                    case '\n':
                    break;
 
                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            Token += aJSON[i];
                    break;
 
                    case '\\':
                        ++i;
                        if (QuoteMode)
                        {
                            char C = aJSON[i];
                            switch (C)
                            {
                                case 't' : Token += '\t'; break;
                                case 'r' : Token += '\r'; break;
                                case 'n' : Token += '\n'; break;
                                case 'b' : Token += '\b'; break;
                                case 'f' : Token += '\f'; break;
                                case 'u':
                                {
                                    string s = aJSON.Substring(i+1,4);
                                    Token += (char)int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    i += 4;
                                    break;
                                }
                                default  : Token += C; break;
                            }
                        }
                    break;
 
                    default:
                        Token += aJSON[i];
                    break;
                }
                ++i;
            }
            if (QuoteMode)
            {
                throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
            }
            return ctx;
        }
 
        public virtual void Serialize(System.IO.BinaryWriter aWriter) {}
 
        public void SaveToStream(System.IO.Stream aData)
        {
            var W = new System.IO.BinaryWriter(aData);
            this.Serialize(W);
        }
 
        #if USE_SharpZipLib
        public void SaveToCompressedStream(System.IO.Stream aData)
        {
            using (var gzipOut = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(aData))
            {
                gzipOut.IsStreamOwner = false;
                SaveToStream(gzipOut);
                gzipOut.Close();
            }
        }
 
        public void SaveToCompressedFile(string aFileName)
        {
            #if USE_FileIO
            System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
            using(var F = System.IO.File.OpenWrite(aFileName))
            {
                SaveToCompressedStream(F);
            }
            #else
            throw new Exception("Can't use File IO stuff in webplayer");
            #endif
        }
        public string SaveToCompressedBase64()
        {
            using (var stream = new System.IO.MemoryStream())
            {
                SaveToCompressedStream(stream);
                stream.Position = 0;
                return System.Convert.ToBase64String(stream.ToArray());
            }
        }
 
        #else
        public void SaveToCompressedStream(System.IO.Stream aData)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public void SaveToCompressedFile(string aFileName)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public string SaveToCompressedBase64()
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        #endif
        
        public void SaveToFile(string aFileName)
        {
            #if USE_FileIO
            System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
            using(var F = System.IO.File.OpenWrite(aFileName))
            {
                this.SaveToStream(F);
            }
            #else
            throw new Exception("Can't use File IO stuff in webplayer");
            #endif
        }
        public string SaveToBase64()
        {
            using (var stream = new System.IO.MemoryStream())
            {
                this.SaveToStream(stream);
                stream.Position = 0;
                return System.Convert.ToBase64String(stream.ToArray());
            }
        }
        public static JSONNode Deserialize(System.IO.BinaryReader aReader)
        {
            JSONBinaryTag type = (JSONBinaryTag)aReader.ReadByte();
            switch(type)
            {
            case JSONBinaryTag.Array:
            {
                int count = aReader.ReadInt32();
                JSONArray tmp = new JSONArray();
                for(int i = 0; i < count; i++)
                    tmp.Add(Deserialize(aReader));
                return tmp;
            }
            case JSONBinaryTag.Class:
            {
                int count = aReader.ReadInt32();                
                JSONClass tmp = new JSONClass();
                for(int i = 0; i < count; i++)
                {
                    string key = aReader.ReadString();
                    var val = Deserialize(aReader);
                    tmp.Add(key, val);
                }
                return tmp;
            }
            case JSONBinaryTag.Value:
            {
                return new JSONData(aReader.ReadString());
            }
            case JSONBinaryTag.IntValue:
            {
                return new JSONData(aReader.ReadInt32());
            }
            case JSONBinaryTag.DoubleValue:
            {
                return new JSONData(aReader.ReadDouble());
            }
            case JSONBinaryTag.BoolValue:
            {
                return new JSONData(aReader.ReadBoolean());
            }
            case JSONBinaryTag.FloatValue:
            {
                return new JSONData(aReader.ReadSingle());
            }
 
            default:
            {
                throw new Exception("Error deserializing JSON. Unknown tag: " + type);
            }
            }
        }
 
        #if USE_SharpZipLib
        public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
        {
            var zin = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(aData);
            return LoadFromStream(zin);
        }
        public static JSONNode LoadFromCompressedFile(string aFileName)
        {
            #if USE_FileIO
            using(var F = System.IO.File.OpenRead(aFileName))
            {
                return LoadFromCompressedStream(F);
            }
            #else
            throw new Exception("Can't use File IO stuff in webplayer");
            #endif
        }
        public static JSONNode LoadFromCompressedBase64(string aBase64)
        {
            var tmp = System.Convert.FromBase64String(aBase64);
            var stream = new System.IO.MemoryStream(tmp);
            stream.Position = 0;
            return LoadFromCompressedStream(stream);
        }
        #else
        public static JSONNode LoadFromCompressedFile(string aFileName)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public static JSONNode LoadFromCompressedBase64(string aBase64)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        #endif
 
        public static JSONNode LoadFromStream(System.IO.Stream aData)
        {
            using(var R = new System.IO.BinaryReader(aData))
            {
                return Deserialize(R);
            }
        }
        public static JSONNode LoadFromFile(string aFileName)
        {
            #if USE_FileIO
            using(var F = System.IO.File.OpenRead(aFileName))
            {
                return LoadFromStream(F);
            }
            #else
            throw new Exception("Can't use File IO stuff in webplayer");
            #endif
        }
        public static JSONNode LoadFromBase64(string aBase64)
        {
            var tmp = System.Convert.FromBase64String(aBase64);
            var stream = new System.IO.MemoryStream(tmp);
            stream.Position = 0;
            return LoadFromStream(stream);
        }
    } // End of JSONNode
 
    public class JSONArray : JSONNode, IEnumerable
    {
        private List<JSONNode> m_List = new List<JSONNode>();
        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex<0 || aIndex >= this.m_List.Count)
                    return new JSONLazyCreator(this);
                return this.m_List[aIndex];
            }
            set
            {
                if (aIndex<0 || aIndex >= this.m_List.Count)
                    this.m_List.Add(value);
                else
                    this.m_List[aIndex] = value;
            }
        }
        public override JSONNode this[string aKey]
        {
            get{ return new JSONLazyCreator(this);}
            set{ this.m_List.Add(value); }
        }
        public override int Count
        {
            get { return this.m_List.Count; }
        }
        public override void Add(string aKey, JSONNode aItem)
        {
            this.m_List.Add(aItem);
        }
        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= this.m_List.Count)
                return null;
            JSONNode tmp = this.m_List[aIndex];
            this.m_List.RemoveAt(aIndex);
            return tmp;
        }
        public override JSONNode Remove(JSONNode aNode)
        {
            this.m_List.Remove(aNode);
            return aNode;
        }
        public override IEnumerable<JSONNode> Childs
        {
            get
            {
                foreach(JSONNode N in this.m_List)
                    yield return N;
            }
        }
        public IEnumerator GetEnumerator()
        {
            foreach(JSONNode N in this.m_List)
                yield return N;
        }
        public override string ToString()
        {
            string result = "[ ";
            foreach (JSONNode N in this.m_List)
            {
                if (result.Length > 2)
                    result += ", ";
                result += N.ToString();
            }
            result += " ]";
            return result;
        }
        public override string ToString(string aPrefix)
        {
            string result = "[ ";
            foreach (JSONNode N in this.m_List)
            {
                if (result.Length > 3)
                    result += ", ";
                result += "\n" + aPrefix + "   ";                
                result += N.ToString(aPrefix+"   ");
            }
            result += "\n" + aPrefix + "]";
            return result;
        }
        public override void Serialize (System.IO.BinaryWriter aWriter)
        {
            aWriter.Write((byte)JSONBinaryTag.Array);
            aWriter.Write(this.m_List.Count);
            for(int i = 0; i < this.m_List.Count; i++)
            {
                this.m_List[i].Serialize(aWriter);
            }
        }
    } // End of JSONArray
 
    public class JSONClass : JSONNode, IEnumerable
    {
        private Dictionary<string,JSONNode> m_Dict = new Dictionary<string,JSONNode>();
        public override JSONNode this[string aKey]
        {
            get
            {
                if (this.m_Dict.ContainsKey(aKey))
                    return this.m_Dict[aKey];
                else
                    return new JSONLazyCreator(this, aKey);
            }
            set
            {
                if (this.m_Dict.ContainsKey(aKey))
                    this.m_Dict[aKey] = value;
                else
                    this.m_Dict.Add(aKey,value);
            }
        }
        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= this.m_Dict.Count)
                    return null;
                return this.m_Dict.ElementAt(aIndex).Value;
            }
            set
            {
                if (aIndex < 0 || aIndex >= this.m_Dict.Count)
                    return;
                string key = this.m_Dict.ElementAt(aIndex).Key;
                this.m_Dict[key] = value;
            }
        }
        public override int Count
        {
            get { return this.m_Dict.Count; }
        }
 
 
        public override void Add(string aKey, JSONNode aItem)
        {
            if (!string.IsNullOrEmpty(aKey))
            {
                if (this.m_Dict.ContainsKey(aKey))
                    this.m_Dict[aKey] = aItem;
                else
                    this.m_Dict.Add(aKey, aItem);
            }
            else
                this.m_Dict.Add(Guid.NewGuid().ToString(), aItem);
        }
 
        public override JSONNode Remove(string aKey)
        {
            if (!this.m_Dict.ContainsKey(aKey))
                return null;
            JSONNode tmp = this.m_Dict[aKey];
            this.m_Dict.Remove(aKey);
            return tmp;        
        }
        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= this.m_Dict.Count)
                return null;
            var item = this.m_Dict.ElementAt(aIndex);
            this.m_Dict.Remove(item.Key);
            return item.Value;
        }
        public override JSONNode Remove(JSONNode aNode)
        {
            try
            {
                var item = this.m_Dict.Where(k => k.Value == aNode).First();
                this.m_Dict.Remove(item.Key);
                return aNode;
            }
            catch
            {
                return null;
            }
        }
 
        public override IEnumerable<JSONNode> Childs
        {
            get
            {
                foreach(KeyValuePair<string,JSONNode> N in this.m_Dict)
                    yield return N.Value;
            }
        }
 
        public IEnumerator GetEnumerator()
        {
            foreach(KeyValuePair<string, JSONNode> N in this.m_Dict)
                yield return N;
        }
        public override string ToString()
        {
            string result = "{";
            foreach (KeyValuePair<string, JSONNode> N in this.m_Dict)
            {
                if (result.Length > 2)
                    result += ", ";
                result += "\"" + Escape(N.Key) + "\":" + N.Value.ToString();
            }
            result += "}";
            return result;
        }
        public override string ToString(string aPrefix)
        {
            string result = "{ ";
            foreach (KeyValuePair<string, JSONNode> N in this.m_Dict)
            {
                if (result.Length > 3)
                    result += ", ";
                result += "\n" + aPrefix + "   ";
                result += "\"" + Escape(N.Key) + "\" : " + N.Value.ToString(aPrefix+"   ");
            }
            result += "\n" + aPrefix + "}";
            return result;
        }
        public override void Serialize (System.IO.BinaryWriter aWriter)
        {
            aWriter.Write((byte)JSONBinaryTag.Class);
            aWriter.Write(this.m_Dict.Count);
            foreach(string K in this.m_Dict.Keys)
            {
                aWriter.Write(K);
                this.m_Dict[K].Serialize(aWriter);
            }
        }
    } // End of JSONClass
 
    public class JSONData : JSONNode
    {
        private string m_Data;
        public override string Value
        {
            get { return this.m_Data; }
            set { this.m_Data = value; }
        }
        public JSONData(string aData)
        {
            this.m_Data = aData;
        }
        public JSONData(float aData)
        {
            this.AsFloat = aData;
        }
        public JSONData(double aData)
        {
            this.AsDouble = aData;
        }
        public JSONData(bool aData)
        {
            this.AsBool = aData;
        }
        public JSONData(int aData)
        {
            this.AsInt = aData;
        }
 
        public override string ToString()
        {
            return "\"" + Escape(this.m_Data) + "\"";
        }
        public override string ToString(string aPrefix)
        {
            return "\"" + Escape(this.m_Data) + "\"";
        }
        public override void Serialize (System.IO.BinaryWriter aWriter)
        {
            var tmp = new JSONData("");
 
            tmp.AsInt = this.AsInt;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JSONBinaryTag.IntValue);
                aWriter.Write(this.AsInt);
                return;
            }
            tmp.AsFloat = this.AsFloat;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JSONBinaryTag.FloatValue);
                aWriter.Write(this.AsFloat);
                return;
            }
            tmp.AsDouble = this.AsDouble;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JSONBinaryTag.DoubleValue);
                aWriter.Write(this.AsDouble);
                return;
            }
 
            tmp.AsBool = this.AsBool;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JSONBinaryTag.BoolValue);
                aWriter.Write(this.AsBool);
                return;
            }
            aWriter.Write((byte)JSONBinaryTag.Value);
            aWriter.Write(this.m_Data);
        }
    } // End of JSONData
 
    internal class JSONLazyCreator : JSONNode
    {
        private JSONNode m_Node = null;
        private string m_Key = null;
 
        public JSONLazyCreator(JSONNode aNode)
        {
            this.m_Node = aNode;
            this.m_Key     = null;
        }
        public JSONLazyCreator(JSONNode aNode, string aKey)
        {
            this.m_Node = aNode;
            this.m_Key     = aKey;
        }
 
        private void Set(JSONNode aVal)
        {
            if (this.m_Key == null)
            {
                this.m_Node.Add(aVal);
            }
            else
            {
                this.m_Node.Add(this.m_Key, aVal);
            }
            this.m_Node = null; // Be GC friendly.
        }
 
        public override JSONNode this[int aIndex]
        {
            get
            {
                return new JSONLazyCreator(this);
            }
            set
            {
                var tmp = new JSONArray();
                tmp.Add(value);
                this.Set(tmp);
            }
        }
 
        public override JSONNode this[string aKey]
        {
            get
            {
                return new JSONLazyCreator(this, aKey);
            }
            set
            {
                var tmp = new JSONClass();
                tmp.Add(aKey, value);
                this.Set(tmp);
            }
        }
        public override void Add (JSONNode aItem)
        {
            var tmp = new JSONArray();
            tmp.Add(aItem);
            this.Set(tmp);
        }
        public override void Add (string aKey, JSONNode aItem)
        {
            var tmp = new JSONClass();
            tmp.Add(aKey, aItem);
            this.Set(tmp);
        }
        public static bool operator ==(JSONLazyCreator a, object b)
        {
            if (b == null)
                return true;
            return System.Object.ReferenceEquals(a,b);
        }
 
        public static bool operator !=(JSONLazyCreator a, object b)
        {
            return !(a == b);
        }
        public override bool Equals (object obj)
        {
            if (obj == null)
                return true;
            return System.Object.ReferenceEquals(this, obj);
        }
        public override int GetHashCode ()
        {
            return base.GetHashCode();
        }
 
        public override string ToString()
        {
            return "";
        }
        public override string ToString(string aPrefix)
        {
            return "";
        }
 
        public override int AsInt
        {
            get
            {
                JSONData tmp = new JSONData(0);
                this.Set(tmp);
                return 0;
            }
            set
            {
                JSONData tmp = new JSONData(value);
                this.Set(tmp);
            }
        }
        public override float AsFloat
        {
            get
            {
                JSONData tmp = new JSONData(0.0f);
                this.Set(tmp);
                return 0.0f;
            }
            set
            {
                JSONData tmp = new JSONData(value);
                this.Set(tmp);
            }
        }
        public override double AsDouble
        {
            get
            {
                JSONData tmp = new JSONData(0.0);
                this.Set(tmp);
                return 0.0;
            }
            set
            {
                JSONData tmp = new JSONData(value);
                this.Set(tmp);
            }
        }
        public override bool AsBool
        {
            get
            {
                JSONData tmp = new JSONData(false);
                this.Set(tmp);
                return false;
            }
            set
            {
                JSONData tmp = new JSONData(value);
                this.Set(tmp);
            }
        }
        public override JSONArray AsArray
        {
            get
            {
                JSONArray tmp = new JSONArray();
                this.Set(tmp);
                return tmp;
            }
        }
        public override JSONClass AsObject
        {
            get
            {
                JSONClass tmp = new JSONClass();
                this.Set(tmp);
                return tmp;
            }
        }
    } // End of JSONLazyCreator
 
    public static class JSON
    {
        public static JSONNode Parse(string aJSON)
        {
            return JSONNode.Parse(aJSON);
        }
    }
}