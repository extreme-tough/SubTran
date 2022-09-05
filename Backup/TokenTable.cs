using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Collections.Generic;

#if DIALOGUEMASTER
using DialogueMaster.Infrastructure;
#endif 

namespace DialogueMaster.Babel
{
	/// <summary>
	/// A table of TokenStats. 
	/// </summary>
    [Serializable()]
    public sealed partial class TokenTable : ITokenTable, ISerializable, IDeserializationCallback
    {

		#region Fields (8) 

        private TokenTable m_CharsetTable = null;
		private bool m_Enabled = true;
        /// <summary>
		/// Maximum number of Tokens to store.
		/// </summary>
		private int m_MaxTokens;
		/// <summary>
		/// Maxiumum lengt of a word to use for word occurence count
		/// </summary>
        private int m_MaxWordLen = 6;
		private int m_Ranks = 0;
		private long m_TotalTokens =0;
        private TokenTable m_WordTable = null;
        public const int MAX_TOKENS_DEFAULT = 300;

		#endregion Fields 

		#region Properties (9) 

        /// <summary>
        /// The SubTable of containing the TokenStats of the Character occurences
        /// </summary>
        /// <remarks>Bad design, but grown over time... </remarks>
        public ITokenTable CharsetTable
        {
            get
            {
                return this.m_CharsetTable;
            }
        }

        /// <summary>
        /// The SubTable of containing the TokenStats of the Word occurences
        /// </summary>
        /// <remarks>Bad design, but grown over time... </remarks>
    	public ITokenTable WordTable
		{
			get
			{
				return this.m_WordTable;
			}
		}
        
        /// <summary>
        /// Number of TokenStats in table
        /// </summary>
        public int Count
        {
            get { return this.m_InnerDict.Count; }
        }

        /// <summary>
        /// If set to false the table will not be used for classification 
        /// </summary>
        /// <remarks>Do not use this feature. It is better to use distinct Models
        /// </remarks>
		public bool Enabled
		{
			get {return this.m_Enabled;}
			set {this.m_Enabled = value;}
		}

        /// <summary>
        /// Collection of the keys (tokens)
        /// </summary>
        public ICollection<string> Keys
        {
            get { return this.m_InnerDict.Keys; }
        }

        /// <summary>
        /// Number of distinct ranks
        /// </summary>
		public int Ranks
		{
			get
			{
				return this.m_Ranks;
			}
			set
			{
				this.m_Ranks = value;
			}
		}

        /// <summary>
        /// Access a single TokenStats by its token
        /// </summary>
        /// <param name="token">the token to look for</param>
        /// <returns>the found ITokenStats or null if not found</returns>
		public ITokenStats this[string token]
		{
			get
			{
                ITokenStats result = null;
                this.m_InnerDict.TryGetValue(token, out result);
                return result;
			}
		}
        /// <summary>
        /// Number of tokens 
        /// </summary>
		public long TotalTokens
		{
			get
			{
				return this.m_TotalTokens;
			}
		}

        /// <summary>
        /// Collection of TokenStats
        /// </summary>
        public ICollection<ITokenStats> Values
        {
            get { return this.m_InnerDict.Values; }
        }

	

		#endregion Properties 

		#region Methods (11) 


		// Public Methods (11) 

        public void Add(string key, ITokenStats stats)
        {
            this.m_InnerDict.Add(key, stats);
        }

		public void AddToken(Hashtable targetTable, string nGram)
		{
			TokenStats stats = targetTable[nGram] as TokenStats;
			if (stats != null)
			{
				stats.AddOccurence();
				return;
			}
            // max 2 mio entries... then abort
            if (targetTable.Count > 1000 * 1000)
                return;
            stats = new TokenStats(nGram);
			targetTable.Add(nGram,stats);

		}

        public void Clear()
        {
            this.m_InnerDict.Clear();
        }



        double ITokenTable.CharsetComparisonScore(ITokenTable otherTable, double cutOf)
        {
            long startTime = DateTime.Now.Ticks;

            double hits = 0;
            foreach (TokenStats test in otherTable.Values)
            {
                int otherRank = this.RankOf(test.Token);
                if (otherRank != -1)
                {
                    hits ++;
                }
            }

            double newScore = (hits / (double)this.Count) * 100;
            if (newScore < cutOf)
            {
                newScore = 0;
            }


#if DIALOGUEMASTER
			if (UseCounters)
			{
				m_Counters.Comparisons.Increment();
				m_Counters.ComparisonsPerSecond.Increment();
				m_Counters.ComparisonTime.IncrementBy( (DateTime.Now.Ticks - startTime) / 100);
				m_Counters.ComparisonTimeBase.Increment();
			}
#endif
            return newScore;
        }
        double ITokenTable.ComparisonScore(ITokenTable otherTable, double cutOf)
        {
            long startTime = DateTime.Now.Ticks;

            double maxScore = this.Ranks * otherTable.Count;
            double maxNegScore = this.Ranks * otherTable.Count;
        //     int score = (int)(maxScore / 1.75); // Assume that at least 50% hits mast be in the first half of the table to be a scorer....
            double score = this.Count * this.Count;
            double score2 = 0;



            foreach (TokenStats test in otherTable.Values)
            {
                int otherRank = this.RankOf(test.Token);
                if (otherRank == -1)
                    score -= this.Ranks;
                else
                {
                    double val = System.Math.Abs(test.Rank - otherRank);
                    
                    score -= val;
                    score2 += this.Ranks - val;
                }
                if (score < cutOf)
                {
                    score = 0;
                    break;
                }
            }

            if (score < -1)
                score = -1;

            double newScore = Math.Max(0, (double)((score + 1) * ((double)score2 / (double)maxNegScore)));


#if DIALOGUEMASTER
			if (UseCounters)
			{
				m_Counters.Comparisons.Increment();
				m_Counters.ComparisonsPerSecond.Increment();
				m_Counters.ComparisonTime.IncrementBy( (DateTime.Now.Ticks - startTime) / 100);
				m_Counters.ComparisonTimeBase.Increment();
			}
#endif
            return newScore;
        }

        public void CreateFromFile(string fileName)
        {
            this.CreateFromFile(fileName, null);
        }

        public void CreateFromFile(string fileName, List<char> charsetFilter)
		{
			using (StreamReader sr = new StreamReader(fileName, true) )
			{
                if (charsetFilter != null)
                {
                    StringBuilder sbContent = new StringBuilder();
                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        line = line.Trim();
                        if (line.Length > 0)
                        {
                            foreach (char c in line)
                            {
                                if (!charsetFilter.Contains(c))
                                    sbContent.Append(c);
                            }
                            sbContent.AppendLine();
                        }
                        line = sr.ReadLine();
                    }
                    CreateFromString(sbContent.ToString());
                }
                else
                {
                    CreateFromString(sr.ReadToEnd());
                }
			}
		}

		public void CreateFromString(string content)
		{
			this.Clear();
			this.m_WordTable.Clear();
            this.m_CharsetTable.Clear();
			long startTime = DateTime.Now.Ticks;


			// any content at all?
			if (content == null)
				return;

			bool lastWasWhitespace = false;

			// buffer for the nGrams
			StringBuilder sb = new StringBuilder(5);
			Hashtable tempStore = new Hashtable();

			// buffer for the words
			Hashtable tempWordStore = new Hashtable();
			StringBuilder currentWord = new StringBuilder();

			// build the Tokens
			for(int i=0;i<content.Length;i++)
			{
				sb.Length=0;
				int t=0;

				while ( (sb.Length < 5) && (i+t < content.Length) )
				{
					char c = Char.ToLower(content[i+t]) ;
					if ( (c=='\r') || (c=='\n') || (c=='\t')) 
						c=' ';
					if (i+t >= content.Length)
						break;

                   
					if ( (Char.IsPunctuation(c)) || (Char.IsSeparator(c)) || (Char.IsSymbol(c)) )
					{
                        /*
                            if ((c != '\'') &&
                                (c != ' ') &&
                                (c != '.') &&
                                (c != ',') &&
                                (c != ':') &&
                                (c != ';') &&
                                (c != '!') &&
                                (c != '?') &&
                                (c != '+') &&
                                (c != '-') &&
                                (c != '(') &&
                                (c != ')') &&
                                (c != '[') &&
                                (c != ']') &&
                                (c != '{') &&
                                (c != '}') &&

                                (c != '\t') &&
                                (c != '"'))
                                System.Console.Out.WriteLine("{0} {1}", c, (int)c);
                        */


                        if (t==0)
						{
							if(currentWord.Length > 1)
								AddToken(tempWordStore,currentWord.ToString());
							currentWord.Length=0;
						}
					}
                    else if ( (Char.IsWhiteSpace(c)) && (!Char.IsLetter(c)) )
					{
						if (t==0)
						{
							if(currentWord.Length > 0)
								AddToken(tempWordStore,currentWord.ToString());
							currentWord.Length=0;
						}
						t++;
						continue;
					}  
                    else  if (Char.IsWhiteSpace(c) || (c=='\r') || (c=='\n') )
					{
						if (t==0)
						{
							if ( (currentWord.Length > 1) && (currentWord.Length < this.m_MaxWordLen) )
								AddToken(tempWordStore,currentWord.ToString());
							currentWord.Length=0;
						}
						if (lastWasWhitespace)
						{
							t++;
							continue;
						}
						

						sb.Append('_');
						lastWasWhitespace = true;
					}


					else if (Char.IsLetter(c))
					{
						
						if ( (t==0) && (!Char.IsPunctuation(c)) )
						{
							currentWord.Append(c) ;
						}
						sb.Append(c);
						lastWasWhitespace = false;
					}
					// add the NGram to the list
                    if (sb.Length > 0)
					    AddToken(tempStore, sb.ToString());
					t++;
				}
			}
			if ( (currentWord.Length > 1) && (currentWord.Length < this.m_MaxWordLen) )
				AddToken(tempWordStore,currentWord.ToString());

			// add the best Tokens to list
			ArrayList bestHitsList = new ArrayList(System.Math.Min(tempStore.Count,this.m_MaxTokens));
			int minVal = Int32.MaxValue;
			foreach(TokenStats stats in tempStore.Values)
			{
				if ( (bestHitsList.Count >= this.m_MaxTokens) &&  (stats.Occurences <= minVal) )
					continue;
				bestHitsList.Add(stats);
				minVal = System.Math.Min(minVal,stats.Occurences);
			}

			// clear temp store
			tempStore.Clear();
			tempStore = null;

			// sort by value
			bestHitsList.Sort();
			// cut to m_MaxTokens Tokens
			if (bestHitsList.Count > this.m_MaxTokens)
				bestHitsList.RemoveRange(this.m_MaxTokens,bestHitsList.Count-this.m_MaxTokens);
			bestHitsList.TrimToSize();

			// calc ranks... and add to self
			int lastRank = -1;
			int lastPosition = 0;
			int maxOccuenceseSoFar = Int32.MaxValue;
			foreach(TokenStats stats in bestHitsList)
			{
				stats.Position = lastPosition++;
				if (stats.Occurences < maxOccuenceseSoFar)
				{
					this.m_Ranks++;
					lastRank++;
					maxOccuenceseSoFar = stats.Occurences;
				}
                stats.Rank = lastRank;
				this.Add(stats.Token,stats);
			}
			bestHitsList.Clear();

			// do it for the WordList...
			minVal = Int32.MaxValue;
			foreach(TokenStats stats in tempWordStore.Values)
			{
				if ( (bestHitsList.Count >= this.m_MaxTokens) &&  (stats.Occurences <= minVal) )
					continue;
				bestHitsList.Add(stats);
				minVal = System.Math.Min(minVal,stats.Occurences);
			}

			// clear temp store
			tempWordStore.Clear();
			tempWordStore = null;

			// sort by value
			bestHitsList.Sort();
            // cut to TokenTable.MAX_TOKENS_DEFAULT Tokens
			if (bestHitsList.Count > this.m_MaxTokens)
				bestHitsList.RemoveRange(this.m_MaxTokens,bestHitsList.Count-this.m_MaxTokens);
			bestHitsList.TrimToSize();

			// calc ranks... and add to self
			lastRank = -1;
            lastPosition = 0;
			maxOccuenceseSoFar = Int32.MaxValue;
			foreach(TokenStats stats in bestHitsList)
			{

                stats.Position = lastPosition++;
                if (stats.Occurences < maxOccuenceseSoFar)
                {
                    this.m_WordTable.Ranks++;
                    lastRank++;
                    maxOccuenceseSoFar = stats.Occurences;
                }
                stats.Rank = lastRank;
                this.m_TotalTokens += stats.Occurences;
                this.m_WordTable.Add(stats.Token, stats);
			}

			bestHitsList.Clear();
	
#if DIALOGUEMASTER
			if (UseCounters)
			{
				m_Counters.TablesCreated.Increment();
				m_Counters.TablesCreatedPerSecond.Increment();
				m_Counters.TableCreationTime.IncrementBy(DateTime.Now.Ticks - startTime);
				m_Counters.TableCreationTimeBase.Increment();
			}
#endif
		}

		public int PositionOf(string Token)
		{
			ITokenStats stats = this[Token];
			if (stats != null)
				return stats.Position;
			return -1;
		}

		public int RankOf(string Token)
		{
			ITokenStats stats = this[Token];
			if (stats != null)
				return stats.Rank;
			return -1;
		}

        double ITokenTable.WordComparisonScore(ITokenTable otherTable, double cutOf, ref int hits)
		{
			long startTime = DateTime.Now.Ticks;

			double score = 0;
			hits = 0;
			long hitCount = 0;
			foreach(TokenStats test in otherTable.WordTable.Values)
			{
				double otherPos = this.WordTable.RankOf(test.Token);
				if (otherPos != -1)
				{
                    double posScore = Math.Log((((double)(this.WordTable.Ranks - otherPos)) / (double)this.WordTable.Ranks) +1 ) * Math.Sqrt(test.Token.Length);
					// System.Console.Out.WriteLine(test.Token+"->"+otherPos.ToString()+" >> "+posScore.ToString());
					score+=posScore;
					hitCount++;
					hits++;
				}
			}

#if DIALOGUEMASTER
			if (UseCounters)
			{
				m_Counters.Comparisons.Increment();
				m_Counters.ComparisonsPerSecond.Increment();
                m_Counters.ComparisonTime.IncrementBy((DateTime.Now.Ticks - startTime) / 100);
				m_Counters.ComparisonTimeBase.Increment();
			}
#endif
			if (hits == 0)
				return 0;
			score = (score*100);
            if (score < cutOf)
                score = 0;
            return score;

		}


		#endregion Methods 
#if DIALOGUEMASTER
        private RemotableValuesDictionary<string, ITokenStats> m_InnerDict;
#else
        private Dictionary<string, ITokenStats> m_InnerDict;
#endif


		#region constructors
		public TokenTable() : this(MAX_TOKENS_DEFAULT)
		{
		}



		/// <summary>
		/// Fake constructor to avoide creation of word table
		/// </summary>
		/// <param name="wordMode"></param>
        public TokenTable(int maxTokens, bool wordMode)
		{
			this.m_MaxTokens = maxTokens;
//            this.m_WordTable = new TokenTable(this.m_MaxTokens, true);
#if DIALOGUEMASTER
            this.m_InnerDict = new RemotableValuesDictionary<string, ITokenStats>(m_MaxTokens);
            if (UseCounters && (m_Counters == null))
            {
                Installer.InstallCounters();
                m_Counters = new TableCounters();
            }
#else
            this.m_InnerDict = new Dictionary<string, ITokenStats>(m_MaxTokens);
#endif

        }

		public TokenTable(int maxTokens) 		{
			this.m_WordTable = new TokenTable(this.m_MaxTokens, true);
            this.m_CharsetTable = new TokenTable(50, false);
			this.m_MaxTokens = maxTokens;
#if DIALOGUEMASTER
            this.m_InnerDict = new RemotableValuesDictionary<string, ITokenStats>(m_MaxTokens);
            if (UseCounters && (m_Counters == null))
            {
                Installer.InstallCounters();
                m_Counters = new TableCounters();
            }
#else
            this.m_InnerDict = new Dictionary<string, ITokenStats>(m_MaxTokens);
#endif
        }

        public TokenTable(string content)
            : this(content, TokenTable.MAX_TOKENS_DEFAULT)
		{
		}

		public TokenTable(string content,int maxTokens) : this(maxTokens)
		{
			this.CreateFromString(content);
            this.BuildCharTable();
		}

        public TokenTable(FileInfo file)
            : this(file, TokenTable.MAX_TOKENS_DEFAULT)
		{
		}
		public TokenTable(FileInfo file,int maxTokens): this(maxTokens)
		{
			this.CreateFromFile(file.FullName);
		}
		#endregion        /*

  
		#region ISerializable Members
		SerializationInfo m_siInfo=null;
		private TokenTable(SerializationInfo info,StreamingContext context) // : base(info,context)
		{
			this.m_siInfo=info;

#if DIALOGUEMASTER
            this.m_InnerDict = new RemotableValuesDictionary<string, ITokenStats>(TokenTable.MAX_TOKENS_DEFAULT);
#else
            this.m_InnerDict = new Dictionary<string, ITokenStats>(TokenTable.MAX_TOKENS_DEFAULT);
#endif


		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("MaxTokens",this.m_MaxTokens);
			info.AddValue("MaxWordLen",this.m_MaxWordLen);
			info.AddValue("Ranks",this.m_Ranks);
			info.AddValue("WordTable",this.m_WordTable);
			String[] objArray1 = new String[this.Count];
			TokenStats[] objArray2 = new TokenStats[this.Count];
			int i=0;
			foreach(String key in this.Keys)
			{
				objArray1[i]=key;
				i++;
			}
			i=0;
			foreach(TokenStats val in this.Values)
			{
				objArray2[i]=val;
				i++;
			}
			info.AddValue("Keys", objArray1, typeof(String[]));
			info.AddValue("Values", objArray2, typeof(TokenStats[]));
		}

		#endregion

		#region IDeserializationCallback Members

		public new void OnDeserialization(object sender)
		{
			if (this.m_siInfo == null)
			{
				throw new SerializationException("Something went wrong during deserialization");
			}
			this.m_MaxTokens = this.m_siInfo.GetInt32("MaxTokens");
			this.m_MaxWordLen = this.m_siInfo.GetInt32("MaxWordLen");
			this.m_Ranks = this.m_siInfo.GetInt32("Ranks");
			this.m_WordTable = this.m_siInfo.GetValue("WordTable", typeof(TokenTable)) as TokenTable;

			String[] objArray1 = (String[])this.m_siInfo.GetValue("Keys",typeof(String[]));
			TokenStats[] objArray2 = (TokenStats[])this.m_siInfo.GetValue("Values",typeof(TokenStats[]));

			for(int i=0;i<objArray1.Length;i++)
				this.Add(objArray1[i],objArray2[i]);


            // rebuild CharTable
            this.m_CharsetTable = new TokenTable();
            this.BuildCharTable();
		}

        private void BuildCharTable()
        {
            this.m_CharsetTable.Clear();
            int rank = 0;
            int pos = 0;
            foreach (ITokenStats stats in this.Values)
            {
                if (stats.Token.Length == 1)
                {
                    TokenStats newStats = new TokenStats(stats.Token);
                    newStats.Occurences = stats.Occurences;
                    newStats.Position = ++pos;
                    newStats.Rank = pos;
                    this.m_CharsetTable.Add(stats.Token,newStats);
                    if (this.m_CharsetTable.Count == 35)
                        break;
                }
            }
            m_CharsetTable.Ranks = pos;
        }

		#endregion
	}
}
